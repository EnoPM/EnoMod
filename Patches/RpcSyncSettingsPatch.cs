using EnoMod.Modules;
using HarmonyLib;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
public class RpcSyncSettingsPatch
{
    public static void Postfix()
    {
        CustomSettingsTab.ShareCustomOptions();
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoSpawnPlayer))]
public class AmongUsClientOnPlayerJoinedPatch {
    public static void Postfix()
    {
        if (PlayerControl.LocalPlayer != null)
        {
            CustomSettingsTab.ShareCustomOptions();
        }
    }
}
