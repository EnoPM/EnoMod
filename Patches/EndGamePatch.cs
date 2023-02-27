using EnoMod.Modules;
using HarmonyLib;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
public static class LogicGameFlowNormalCheckEndCriteriaPatch
{
    public static bool Prefix(ShipStatus __instance)
    {
        if (CustomSettings.DebugMode) return false;
        if (!GameData.Instance) return false;
        if (DestroyableSingleton<TutorialManager>.InstanceExists) return true;

        foreach (var role in CustomRole.Roles)
        {
            if (role.TriggerEndGame()) return true;
        }

        var statistics = new PlayerStatistics();
        if (CheckAndEndGameForSabotageWin(__instance)) return false;
        if (CheckAndEndGameForTaskWin(__instance)) return false;
        if (CheckAndEndGameForImpostorWin(statistics)) return false;
        if (CheckAndEndGameForCrewmateWin(statistics)) return false;
        return false;
    }

    private static bool CheckAndEndGameForTaskWin(ShipStatus __instance)
    {
        if (GameData.Instance.TotalTasks <= 0 ||
            GameData.Instance.TotalTasks > GameData.Instance.CompletedTasks) return false;
        // __instance.enabled = false;
        GameManager.Instance.RpcEndGame(GameOverReason.HumansByTask, false);
        return true;
    }

    private static bool CheckAndEndGameForSabotageWin(ShipStatus __instance)
    {
        var systemType = MapUtilities.Systems.TryGetValue(SystemTypes.LifeSupp, out var value) ? value : null;
        var lifeSuppSystemType = systemType?.TryCast<LifeSuppSystemType>();
        if (lifeSuppSystemType is { Countdown: < 0f })
        {
            EndGameForSabotage(__instance);
            lifeSuppSystemType.Countdown = 10000f;
            return true;
        }

        var systemType2 = (MapUtilities.Systems.TryGetValue(SystemTypes.Reactor, out var value2) ? value2 : null) ??
                          (MapUtilities.Systems.TryGetValue(SystemTypes.Laboratory, out var value3) ? value3 : null);

        var criticalSystem = systemType2?.TryCast<ICriticalSabotage>();
        if (criticalSystem is not { Countdown: < 0f }) return false;
        EndGameForSabotage(__instance);
        criticalSystem.ClearSabotage();
        return true;
    }

    private static void EndGameForSabotage(ShipStatus __instance)
    {
        // __instance.enabled = false;
        GameManager.Instance.RpcEndGame(GameOverReason.ImpostorBySabotage, false);
        return;
    }

    private static bool CheckAndEndGameForImpostorWin(PlayerStatistics statistics)
    {
        if (statistics.TeamImpostorsAlive < statistics.TotalAlive - statistics.TeamImpostorsAlive) return false;
        // __instance.enabled = false;
        var endReason = TempData.LastDeathReason switch
        {
            DeathReason.Exile => GameOverReason.ImpostorByVote,
            DeathReason.Kill => GameOverReason.ImpostorByKill,
            _ => GameOverReason.ImpostorByVote,
        };
        GameManager.Instance.RpcEndGame(endReason, false);
        return true;
    }

    private static bool CheckAndEndGameForCrewmateWin(PlayerStatistics statistics)
    {
        if (statistics.TeamImpostorsAlive != 0) return false;
        // __instance.enabled = false;
        GameManager.Instance.RpcEndGame(GameOverReason.HumansByVote, false);
        return true;
    }

    private class PlayerStatistics
    {
        public int TeamImpostorsAlive { get; private set; }
        public int TotalAlive { get; private set; }

        public PlayerStatistics()
        {
            GetPlayerCounts();
        }

        private void GetPlayerCounts()
        {
            var numImpostorsAlive = 0;
            var numTotalAlive = 0;

            foreach (var playerInfo in GameData.Instance.AllPlayers.GetFastEnumerator())
            {
                if (playerInfo.Disconnected) continue;
                if (playerInfo.IsDead) continue;
                numTotalAlive++;

                if (playerInfo.Role.IsImpostor)
                {
                    numImpostorsAlive++;
                }
            }

            TeamImpostorsAlive = numImpostorsAlive;
            TotalAlive = numTotalAlive;
        }
    }
}
