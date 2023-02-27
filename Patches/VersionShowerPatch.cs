using HarmonyLib;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public static class VersionShowerPatch
{
    public static void Postfix(VersionShower __instance)
    {
        __instance.text.text += $" + <size=70%><color=#5B48B0FF>EnoMod v{EnoModPlugin.Version}</color> by Eno</size>";
    }
}
