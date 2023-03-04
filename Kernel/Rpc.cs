using System;
using System.Linq;
using System.Text.Json;
using EnoMod.Customs;
using Reactor.Networking.Attributes;

namespace EnoMod.Kernel;

public static class Rpc
{

    [MethodRpc((uint) CustomRpc.ShieldedMurderAttempt)]
    public static void ShieldedMurderAttempt(PlayerControl _, string text)
    {
        var murderInfo = Deserialize<MurderInfo>(text);
        if (murderInfo.Murder != PlayerControl.LocalPlayer.PlayerId) return;
        var target = PlayerCache.AllPlayers.Find(p => p != null && p.PlayerId == murderInfo.Target);
        target?.PlayerControl.ShowFailedMurder();
    }

    [MethodRpc((uint) CustomRpc.MurderAttempt)]
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

    [MethodRpc((uint) CustomRpc.ShareCustomOptions)]
    public static void ShareCustomOptions(PlayerControl _, string text)
    {
        if (AmongUsClient.Instance.AmHost) return;
        var customOptionInfos = Deserialize<CustomOptionInfo[]>(text);
        foreach (var customOptionInfo in customOptionInfos)
        {
            var option = CustomOption.Tab.Options().Find(co => co.Key == customOptionInfo.Key);
            if (option == null) return;
            option.SelectionIndex = customOptionInfo.Selection;
        }
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
