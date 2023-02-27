using EnoMod.Modules;
using HarmonyLib;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
internal class BaseExileControllerPatch
{
    public static void Postfix(ExileController __instance)
    {
        CustomButton.MeetingEndedUpdate();
    }
}
