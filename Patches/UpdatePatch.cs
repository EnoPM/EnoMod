using EnoMod.Modules;
using HarmonyLib;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public class HudManagerUpdatePatch
{
    public static void Postfix(HudManager __instance)
    {
        if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
        UpdateChatButton(__instance);
        CustomButton.HudUpdate();
    }

    private static void UpdateChatButton(HudManager hudManager)
    {
        if (!hudManager.Chat.isActiveAndEnabled && (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay || CustomSettings.EnableChatInGame))
        {
            hudManager.Chat.SetVisible(true);
        }
    }
}
