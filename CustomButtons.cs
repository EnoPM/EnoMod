using EnoMod.Customs;
using EnoMod.Kernel;
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
        Hooks.Trigger(CustomHooks.LoadCustomButtons, hudManager);

        _initialized = true;
    }
}
