namespace EnoMod.Modules;

public static class Game
{
    public enum MurderAttemptResult
    {
        PerformKill,
        SuppressKill,
    }

    public static MurderAttemptResult CheckMurderAttempt(
        PlayerControl? killer,
        PlayerControl target)
    {
        // Modified vanilla checks
        if (AmongUsClient.Instance.IsGameOver) return MurderAttemptResult.SuppressKill;
        if (killer == null || killer.Data == null || killer.Data.IsDead || killer.Data.Disconnected)
            return MurderAttemptResult.SuppressKill; // Allow non Impostor kills compared to vanilla code
        if (target == null || target.Data == null || target.Data.IsDead || target.Data.Disconnected)
            return MurderAttemptResult.SuppressKill; // Allow killing players in vents compared to vanilla code
        if (!CustomSettings.ShieldFirstKilledPlayer)
            return MurderAttemptResult.PerformKill;
        if (target != killer && GameState.Instance.PlayerShielded == target.Data.PlayerName)
            return MurderAttemptResult.SuppressKill;

        return MurderAttemptResult.PerformKill;
    }
}
