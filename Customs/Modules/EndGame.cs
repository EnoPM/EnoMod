using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using EnoMod.Kernel;
using EnoMod.Utils;

namespace EnoMod.Customs.Modules;

public static class EndGame
{
    [EnoHook(CustomHooks.EndGameCheck)]
    public static Hooks.Result EndGameCheck()
    {
        if (!Utils.AmongUs.IsHost()) return Hooks.Result.Continue;
        CheckEndGame();
        return Hooks.Result.Continue;
    }

    private static void CheckEndGame()
    {
        if (Singleton<CustomOption.Holder>.Instance.DebugMode) return;
        var statistics = new PlayerStatistics();
        if (CheckAndEndGameForSabotageWin() || CheckAndEndGameForImpostorWin(statistics))
        {
            EndGameState.State.IsEndGame = true;
            EndGameState.State.Color = "#FF0000";
            EndGameState.State.Title = "Impostor win";
            EndGameState.State.Winners = PlayerCache.AllPlayers.Where(IsImpostor).Select(p => p.PlayerId).ToList();
        }
        else if (CheckAndEndGameForTaskWin() || CheckAndEndGameForCrewmateWin(statistics))
        {
            EndGameState.State.IsEndGame = true;
            EndGameState.State.Color = "#63e5ff";
            EndGameState.State.Title = "Crewmate win";
            EndGameState.State.Winners = PlayerCache.AllPlayers.Where(IsCrewmate).Select(p => p.PlayerId).ToList();
        }

        if (!EndGameState.IsEndGame && EndGameState.State.IsEndGame)
        {
            EndGameState.Share();
        }
    }

    private static bool IsImpostor(PlayerCache player)
    {
        var impostorsRoles = new List<RoleTypes>
            { RoleTypes.Impostor, RoleTypes.Shapeshifter, RoleTypes.ImpostorGhost };
        return impostorsRoles.Contains(player.Data.RoleType);
    }

    private static bool IsCrewmate(PlayerCache player)
    {
        var crewmateRoles = new List<RoleTypes>
            { RoleTypes.Crewmate, RoleTypes.Engineer, RoleTypes.Scientist, RoleTypes.CrewmateGhost };
        var role = CustomRole.GetByPlayer(player);
        if (crewmateRoles.Contains(player.Data.RoleType))
        {
            return role == null || role.Team == CustomRole.Teams.Crewmate;
        }

        return false;
    }

    [EnoHook(CustomHooks.ExileControllerWrapUp)]
    public static Hooks.Result ExileControllerWrapUp(ExileController exileController)
    {
        CustomButton.MeetingEndedUpdate();
        return Hooks.Result.Continue;
    }

    private static bool CheckAndEndGameForTaskWin()
    {
        var totalTasks = PlayerCache.AllPlayers.Where(IsCrewmate).Select(p => p.PlayerControl.myTasks.Count).ToList().Sum();
        var completedTasks = PlayerCache.AllPlayers.Where(IsCrewmate).Select(p => p.PlayerControl.myTasks.ToArray().Where(t => t.IsComplete).ToList().Count).ToList().Sum();
        if (totalTasks <= 0 || totalTasks > completedTasks) return false;
        // __instance.enabled = false;
        GameManager.Instance.RpcEndGame(GameOverReason.HumansByTask, false);
        return true;
    }

    private static bool CheckAndEndGameForSabotageWin()
    {
        var systemType = MapUtilities.Systems.TryGetValue(SystemTypes.LifeSupp, out var value) ? value : null;
        var lifeSuppSystemType = systemType?.TryCast<LifeSuppSystemType>();
        if (lifeSuppSystemType is { Countdown: < 0f })
        {
            EndGameForSabotage();
            lifeSuppSystemType.Countdown = 10000f;
            return true;
        }

        var systemType2 = (MapUtilities.Systems.TryGetValue(SystemTypes.Reactor, out var value2) ? value2 : null) ??
                          (MapUtilities.Systems.TryGetValue(SystemTypes.Laboratory, out var value3) ? value3 : null);

        var criticalSystem = systemType2?.TryCast<ICriticalSabotage>();
        if (criticalSystem is not { Countdown: < 0f }) return false;
        EndGameForSabotage();
        criticalSystem.ClearSabotage();
        return true;
    }

    private static void EndGameForSabotage()
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
