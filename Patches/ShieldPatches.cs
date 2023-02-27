using EnoMod.Modules;
using HarmonyLib;

namespace EnoMod.Patches;

// On player kill action
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
public class CheckMurderPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (Game.CheckMurderAttempt(__instance, target) == Game.MurderAttemptResult.SuppressKill)
        {
            Rpc.ShieldedMurderAttempt(
                __instance,
                Rpc.Serialize(new Rpc.MurderInfo { Murder = __instance.PlayerId, Target = target.PlayerId }));
            return false;
        }

        if (GameState.Instance.FirstPlayerKilledInThisGame == null)
        {
            GameState.Instance.FirstPlayerKilledInThisGame = target.Data.PlayerName;
            if (CustomSettings.RemoveShieldInFirstKill)
            {
                GameState.Instance.PlayerShielded = null;
            }

            GameState.ShareChanges();
        }

        return true;
    }
}

// On Meeting call
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public class MeetingHudUpdatePatch
{
    static void Postfix(MeetingHud __instance)
    {
        if (!CustomSettings.RemoveShieldInFirstMeeting) return;
        GameState.Instance.PlayerShielded = null;
        GameState.ShareChanges();
    }
}
