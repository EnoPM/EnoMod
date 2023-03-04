using System.Collections.Generic;
using System.Linq;

namespace EnoMod.Utils;

public static class AmongUs
{
    public static bool IsHost()
    {
        return AmongUsClient.Instance.AmHost;
    }

    public static bool IsGameOver()
    {
        return AmongUsClient.Instance.IsGameOver;
    }

    public static bool IsCommunicationsDisabled()
    {
        int mapId = EnoModPlugin.NormalOptions.MapId;
        if (mapId == 1)
        {
            var hqHudSystemType = ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HqHudSystemType>();
            return hqHudSystemType is { IsActive: true };
        }

        var hudOverrideSystemType = ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>();
        return hudOverrideSystemType is { IsActive: true };
    }
}

public static class Players
{
    public static PlayerControl LocalPlayer => PlayerControl.LocalPlayer;
    public static List<PlayerControl> AllPlayers => PlayerControl.AllPlayerControls.ToArray().ToList();

    public static PlayerControl? PlayerById(byte id)
    {
        return AllPlayers.Find(p => p.PlayerId == id);
    }
}