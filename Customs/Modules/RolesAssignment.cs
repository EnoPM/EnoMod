using System.Collections.Generic;
using System.Linq;
using EnoMod.Kernel;
using EnoMod.Utils;
using Reactor.Networking.Attributes;

namespace EnoMod.Customs.Modules;

public static class RolesAssignment
{
    [EnoHook(CustomHooks.RoleManagerSelectRoles)]
    public static Hooks.Result RoleManagerSelectRoles()
    {
        if (!Singleton<CustomOption.Holder>.Instance.EnableRoles) return Hooks.Result.Continue;
        CustomRole.ClearPlayers();
        var playerList = PlayerCache.AllPlayers.Select(player => player.PlayerControl).ToList();
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

        playerList = playerList.Randomize();
        roleNames = roleNames.Randomize();

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

        return Hooks.Result.Continue;
    }

    public static void SetRoleToPlayer(CustomRole role, PlayerControl player)
    {
        role.AddPlayer(player.PlayerId);
        System.Console.WriteLine($"*** I assigned role {role.Name} to {player.Data.PlayerName}");
    }

    [MethodRpc((uint) CustomRpc.UpdateRoleInfo)]
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
}
