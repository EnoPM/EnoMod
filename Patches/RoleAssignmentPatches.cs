using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using EnoMod.Modules;
using HarmonyLib;
using UnityEngine;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
public class RoleManagerSelectRolesPatch
{
    public static void Postfix()
    {
        if (!CustomSettings.EnableRoles) return;
        System.Console.WriteLine("RoleManagerSelectRolesPatch.Postfix patch");
        AssignRoles();
    }

    private static void AssignRoles()
    {
        CustomRole.ClearPlayers();
        var playerList = PlayerControl.AllPlayerControls;
        var roleNames = new List<string>();
        foreach (var role in CustomRole.Roles)
        {
            if (role.NumberCustomOption == null || role.PercentageCustomOption == null ||
                role.PercentageCustomOption == 0f) continue;
            var quantity = (int) role.NumberCustomOption;
            if (quantity == 0) continue;
            var percentage = (float) role.PercentageCustomOption;
            System.Console.WriteLine($"Role: {role.Name} quantity: {quantity}, percentage: {percentage}%");
            for (var i = 0; i < quantity; i++)
            {
                if (EnoModPlugin.Rnd.Next(0, 100) > percentage) continue;
                roleNames.Add(role.Name);
            }
        }

        foreach (var player in playerList)
        {
            for (var roleNameIndex = 0; roleNameIndex < roleNames.Count; roleNameIndex++)
            {
                var role = CustomRole.GetByName(roleNames[roleNameIndex]);
                if (role == null) continue;
                if (role.CanBe(player))
                {
                    SetRoleToPlayer(role, player);
                    roleNames.RemoveAt(roleNameIndex);
                    break;
                }
            }
        }
    }

    private static void SetRoleToPlayer(CustomRole role, PlayerControl player)
    {
        role.Players.Add(new RoleData.RolePlayer { PlayerId = player.PlayerId });
        System.Console.WriteLine($"*** I assigned role {role.Name} to {player.Data.PlayerName}");
        Rpc.UpdateRoleInfo(player, role.Serialize());
    }
}

[HarmonyPatch(typeof(RoleOptionsData), nameof(RoleOptionsData.GetNumPerGame))]
internal class RoleOptionsDataGetNumPerGamePatch
{
    public static void Postfix(ref int __result)
    {
        if (CustomSettings.EnableRoles) __result = 0;
    }
}

[HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
internal class GameOptionsDataGetAdjustedNumImpostorsPatch
{
    public static void Postfix(ref int __result)
    {
        __result = Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.NumImpostors, 1, 3);
    }
}

[HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Validate))]
internal class GameOptionsDataValidatePatch
{
    public static void Postfix(GameOptionsData __instance)
    {
        __instance.NumImpostors = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
    }
}
