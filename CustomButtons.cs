using EnoMod.Modules;
using HarmonyLib;

namespace EnoMod;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
public static class CustomButtons
{
    private static bool _initialized;

    public static void Postfix(HudManager __instance)
    {
        _initialized = false;
        CreateButtons(__instance);
    }

    private static void CreateButtons(HudManager hudManager)
    {
        foreach (var role in CustomRole.Roles)
        {
            role.CreateCustomButtons(hudManager);
        }

        _initialized = true;
    }
}
