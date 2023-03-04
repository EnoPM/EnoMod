using System;
using System.Linq;
using EnoMod.Kernel;
using EnoMod.Utils;
using Reactor.Networking.Attributes;

namespace EnoMod.Customs.Modules;

public static class ChatCommands
{
    [EnoHook(CustomHooks.LocalPlayerChatMessageSending)]
    public static Hooks.Result LocalPlayerChatMessageSending(ChatController chatController)
    {
        if (!Utils.AmongUs.IsHost()) return Hooks.Result.Continue;
        var command = chatController.TextArea.text.Split(" ");
        switch (command[0])
        {
            case "/shield":
                if (command.Length <= 1) break;
                chatController.TextArea.Clear();
                return ShieldCommand(JoinName(command.Where((_, i) => i > 0).ToArray()));
            case "/rshield":
                if (command.Length <= 1) break;
                chatController.TextArea.Clear();
                return RemoveShieldCommand(JoinName(command.Where((_, i) => i > 0).ToArray()));
            case "/revive":
                if (command.Length <= 1) break;
                chatController.TextArea.Clear();
                return ReviveCommand(JoinName(command.Where((_, i) => i > 0).ToArray()));
            case "/role":
                if (command.Length <= 2) break;
                chatController.TextArea.Clear();
                return RoleCommand(command[1], JoinName(command.Where((_, i) => i > 1).ToArray()));
        }

        return Hooks.Result.Continue;
    }

    private static Hooks.Result RoleCommand(string roleName, string playerName)
    {
        if (PlayerCache.LocalPlayer == null) return Hooks.Result.ReturnFalse;
        var player = GetPlayerByName(playerName);
        if (player == null) return Hooks.Result.ReturnFalse;
        var role = CustomRole.Roles.Find(role =>
            string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase));
        if (role == null) return Hooks.Result.ReturnFalse;
        role.AddPlayer(player.PlayerId);
        return Hooks.Result.ReturnFalse;
    }

    private static Hooks.Result ReviveCommand(string playerName)
    {
        if (PlayerCache.LocalPlayer == null) return Hooks.Result.ReturnFalse;
        var player = GetPlayerByName(playerName);
        if (player == null) return Hooks.Result.ReturnFalse;
        RevivePlayer(PlayerCache.LocalPlayer.PlayerControl,
            Serializer.Serialize(new PlayerInfo(player.PlayerControl.PlayerId)));
        return Hooks.Result.ReturnFalse;
    }

    [MethodRpc((uint) CustomRpc.RevivePlayer)]
    public static void RevivePlayer(PlayerControl sender, string text)
    {
        var playerInfo = Serializer.Deserialize<PlayerInfo>(text);
        var player = PlayerCache.GetPlayerById(playerInfo.Id);
        if (player == null) return;
        if (player.PlayerControl.Data.IsDead)
        {
            player.PlayerControl.Revive();
        }
    }

    public class PlayerInfo
    {
        public byte Id { get; }

        public PlayerInfo(byte id)
        {
            Id = id;
        }
    }

    private static Hooks.Result RemoveShieldCommand(string playerName)
    {
        var player = GetPlayerByName(playerName);
        if (player == null) return Hooks.Result.ReturnFalse;
        if (!Singleton<Shields>.Instance.IsShielded(player)) return Hooks.Result.ReturnFalse;
        Singleton<Shields>.Instance.RemoveShieldedPlayer(player);

        return Hooks.Result.ReturnFalse;
    }

    private static Hooks.Result ShieldCommand(string playerName)
    {
        var player = GetPlayerByName(playerName);
        if (player == null) return Hooks.Result.ReturnFalse;
        if (Singleton<Shields>.Instance.IsShielded(player)) return Hooks.Result.ReturnFalse;
        Singleton<Shields>.Instance.AddShieldedPlayer(player);
        Singleton<Shields>.Instance.Share();

        return Hooks.Result.ReturnFalse;
    }

    private static PlayerCache? GetPlayerByName(string name)
    {
        return PlayerCache.AllPlayers.Find(player =>
            string.Equals(player.Data.PlayerName, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string JoinName(string[] values)
    {
        return string.Join(" ", values);
    }
}
