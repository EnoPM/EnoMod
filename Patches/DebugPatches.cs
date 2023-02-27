using HarmonyLib;

namespace EnoMod.Patches;

[HarmonyPatch]
[HarmonyPriority(Priority.First)]
public static class CheckEndCriteriaPatch
{
    [HarmonyPatch(typeof(LogicGameFlow), nameof(LogicGameFlow.CheckEndCriteria))]
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlow.CheckEndCriteria))]
    [HarmonyPatch(typeof(LogicGameFlowHnS), nameof(LogicGameFlow.CheckEndCriteria))]
    public static bool Prefix()
    {
        return !CustomSettings.EnableRoles;
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
public static class GameStartManagerUpdatePatch
{
    public static void Prefix(GameStartManager __instance)
    {
        __instance.MinPlayers = 1;
    }
}