using System;
using System.Linq;
using System.Text.Json;
using EnoMod.Roles;
using Reactor.Networking.Attributes;

namespace EnoMod.Modules;

public static class Rpc
{
    private enum Calls
    {
        ShareGameState,
        ShareCustomOptions,
        ShieldedMurderAttempt,
        MurderAttempt,
        UpdateRoleInfo,
        JesterSabotageStart,
        JesterSabotageEnd,
    }

    [MethodRpc((uint) Calls.ShareGameState)]
    public static void ShareGameState(PlayerControl player, string text)
    {
        try
        {
            var data = JsonSerializer.Deserialize<GameState>(text);
            if (data == null) return;
            GameState.Instance = data;
        }
        catch (Exception e)
        {
            EnoModPlugin.Logger!.LogError("Error while deserializing options: " + e.Message);
            throw;
        }
    }

    [MethodRpc((uint) Calls.ShieldedMurderAttempt)]
    public static void ShieldedMurderAttempt(PlayerControl _, string text)
    {
        var murderInfo = Deserialize<MurderInfo>(text);
        if (murderInfo.Murder != PlayerControl.LocalPlayer.PlayerId) return;
        var target = PlayerCache.AllPlayers.Find(p => p != null && p.PlayerId == murderInfo.Target);
        target?.PlayerControl.ShowFailedMurder();
    }

    [MethodRpc((uint) Calls.MurderAttempt)]
    public static void MurderAttempt(PlayerControl player, string text)
    {
        var murderInfo = Deserialize<MurderInfo>(text);
        var murder = PlayerCache.AllPlayers.Find(p => p.PlayerId == murderInfo.Murder);
        var target = PlayerCache.AllPlayers.Find(p => p.PlayerId == murderInfo.Target);
        if (murder == null || target == null) return;
        murder.PlayerControl.MurderPlayer(target.PlayerControl);
    }

    public class MurderInfo
    {
        public byte? Murder { get; set; }
        public byte Target { get; set; }
    }

    [MethodRpc((uint) Calls.UpdateRoleInfo)]
    public static void UpdateRoleInfo(PlayerControl player, string text)
    {
        var role = RoleData.Deserialize(text);
        var localPlayer = PlayerCache.LocalPlayer;
        if (localPlayer == null) return;
        CustomRole.Roles.Find(cr => cr.Id == role.Id)?.UpdateFromData(role);
        if (role.Players.Any(rp => rp.PlayerId == localPlayer.PlayerId))
        {
            System.Console.WriteLine($"### I am {role.Name} in this game");
        }
    }

    [MethodRpc((uint) Calls.ShareCustomOptions)]
    public static void ShareCustomOptions(PlayerControl _, string text)
    {
        if (AmongUsClient.Instance.AmHost) return;
        var customOptionInfos = Deserialize<CustomOptionInfo[]>(text);
        foreach (var customOptionInfo in customOptionInfos)
        {
            var option = CustomSettingsTab.Options().Find(co => co.Key == customOptionInfo.Key);
            if (option == null) return;
            option.SelectionIndex = customOptionInfo.Selection;
        }
    }

    [MethodRpc((uint) Calls.JesterSabotageStart)]
    public static void JesterSabotageStart(PlayerControl _)
    {
        Reference.Jester.JesterSabotageActive = true;
    }

    [MethodRpc((uint) Calls.JesterSabotageEnd)]
    public static void JesterSabotageEnd(PlayerControl _)
    {
        Reference.Jester.JesterSabotageActive = false;
    }

    public class CustomOptionInfo
    {
        public string Key { get; set; }
        public int Selection { get; set; }
    }

    public static string Serialize<T>(T data)
    {
        return JsonSerializer.Serialize(data);
    }

    public static T Deserialize<T>(string data)
    {
        return JsonSerializer.Deserialize<T>(data) ?? throw new EnoModException("Deserialization error");
    }
}
