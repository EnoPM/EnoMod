using System.Text.Json;

namespace EnoMod.Modules;

internal class GameState
{
    public string? PlayerShielded { get; set; }
    public string? FirstPlayerKilledInThisGame { get; set; }

    public static GameState Instance = new();

    public static void ShareChanges()
    {
        if (!AmongUsClient.Instance.AmHost || !PlayerCache.LocalPlayer?.PlayerControl) return;
        Rpc.ShareGameState(PlayerControl.LocalPlayer, JsonSerializer.Serialize(Instance));
    }
}
