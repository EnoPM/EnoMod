using System;
using System.Linq;
using AmongUs.GameOptions;
using EnoMod.Modules;
using HarmonyLib;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
public class ChatPatch
{
    public static bool Prefix(ChatController __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        var command = __instance.TextArea.text.Split(" ");
        switch (command[0])
        {
            case "/shield":
                if (command.Length > 1)
                {
                    var playerName = _joinName(command.Where((_, i) => i > 0).ToArray());
                    var player = GetPlayerByName(playerName);
                    if (player == null) return true;
                    GameState.Instance.PlayerShielded = player.Data.PlayerName;
                    GameState.ShareChanges();
                    __instance.TextArea.Clear();
                    return false;
                }

                break;
            case "/fshield":
                if (command.Length > 1)
                {
                    var playerName2 = _joinName(command.Where((_, i) => i > 0).ToArray());
                    var player2 = GetPlayerByName(playerName2);
                    if (player2 == null) return true;
                    GameState.Instance.FirstPlayerKilledInThisGame = player2.Data.PlayerName;
                    GameState.ShareChanges();
                    __instance.TextArea.Clear();
                    return false;
                }

                break;

            case "/sheriff":
                var playerName3 = _joinName(command.Where((_, i) => i > 0).ToArray());
                var player3 = GetPlayerByName(playerName3);
                if (player3 == null) return true;
                __instance.TextArea.Clear();
                return false;
            case "/test":
                __instance.TextArea.Clear();
                return false;
            case "/crewmate":
                if (TutorialManager.InstanceExists && PlayerControl.LocalPlayer)
                {
                    PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                }
                __instance.TextArea.Clear();
                return false;
            case "/impostor":
                if (TutorialManager.InstanceExists && PlayerControl.LocalPlayer)
                {
                    PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Impostor);
                }
                __instance.TextArea.Clear();
                return false;
        }

        return true;
    }

    private static PlayerCache? GetPlayerByName(string name)
    {
        return PlayerCache.AllPlayers.Find(player =>
            player != null && string.Equals(player.Data.PlayerName, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string _joinName(string[] values)
    {
        return string.Join(" ", values);
    }
}
