using System.Collections.Generic;
using System.Linq;
using EnoMod.Customs;
using EnoMod.Utils;
using HarmonyLib;
using Reactor.Networking.Attributes;
using UnityEngine;

namespace EnoMod.Kernel;

public static class EndGameState
{
    public static SerializableEndGameState State { get; private set; } = new();

    private static readonly List<byte> _updatedPlayers = new();

    public static bool IsEndGame
    {
        get
        {
            if (State.IsEndGame)
            {
                System.Console.WriteLine(
                    $"IsEndGame {PlayerCache.AllPlayers.Where(p => !p.Data.Disconnected).ToList().Count} {_updatedPlayers.Count}");
                return PlayerCache.AllPlayers.Where(p => !p.Data.Disconnected).ToList().Count == _updatedPlayers.Count;
            }

            return State.IsEndGame;
        }
    }

    public static List<WinningPlayerData> Winners { get; private set; } = new();

    public static void UpdateFromData(SerializableEndGameState state)
    {
        State = state;
        var winners = new List<WinningPlayerData>();
        foreach (var playerId in State.Winners)
        {
            var player = PlayerCache.GetPlayerById(playerId);
            if (player == null) continue;
            winners.Add(new WinningPlayerData(player.Data));
        }

        Winners = winners.OrderBy(winner => winner.IsYou ? 0 : -1).ToList();
    }

    public static void Share()
    {
        if (PlayerCache.LocalPlayer == null) return;
        ShareEndGameState(PlayerCache.LocalPlayer, Serializer.Serialize(State));
    }

    public static void Reset()
    {
        UpdateFromData(new SerializableEndGameState());
        _updatedPlayers.Clear();
    }

    [MethodRpc((uint) CustomRpc.ShareEndGameState)]
    public static void ShareEndGameState(PlayerControl sender, string text)
    {
        if (PlayerCache.LocalPlayer == null) return;
        System.Console.WriteLine($"[{PlayerCache.LocalPlayer.Data.PlayerName}] I receive ShareEndGameState");
        var state = Serializer.Deserialize<SerializableEndGameState>(text);
        UpdateFromData(state);
        EndGameStateReceived(PlayerCache.LocalPlayer, string.Empty);
        if (State.IsEndGame)
        {
            System.Console.WriteLine($"[{PlayerCache.LocalPlayer?.Data.PlayerName}] I start Instance EndGame");
            // GameManager.Instance.RpcEndGame(GameOverReason.HumansByTask, false);
        }
    }

    [MethodRpc((uint) CustomRpc.EndGameStateReceived)]
    public static void EndGameStateReceived(PlayerControl player, string text)
    {
        if (!_updatedPlayers.Contains(player.PlayerId))
        {
            _updatedPlayers.Add(player.PlayerId);
        }
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class AmongUsClientOnGameEndPatch
{
    private static GameOverReason _gameOverReason;

    public static void Prefix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        _gameOverReason = endGameResult.GameOverReason;
        if ((int) endGameResult.GameOverReason >= 10) endGameResult.GameOverReason = GameOverReason.ImpostorByKill;
    }

    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        CustomRole.ClearPlayers();
    }
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
public static class EndGameManagerSetEverythingUp
{
    private static readonly int _color = Shader.PropertyToID("_Color");

    public static void Postfix(EndGameManager __instance)
    {
        System.Console.WriteLine(
            $"EndGameManagerSetEverythingUp Postfix triggered for {PlayerCache.LocalPlayer?.Data.PlayerName} {EndGameState.State.Title}");
        // Delete and readd PoolablePlayers always showing the name and role of the player
        foreach (var pb in __instance.transform.GetComponentsInChildren<PoolablePlayer>())
        {
            Object.Destroy(pb.gameObject);
        }

        System.Console.WriteLine($"EndGameState.State.Winners: {EndGameState.State.Winners.Count}");
        System.Console.WriteLine($"PlayerCache.AllPlayers: {PlayerCache.AllPlayers.Count}");

        if (PlayerCache.LocalPlayer != null)
        {
            var win = EndGameState.State.Winners.Contains(PlayerCache.LocalPlayer.PlayerId);
            __instance.WinText.text = win ? "Victory" : "Defeat";
            __instance.WinText.color = win ? Color.green : Color.red;
        }

        var num = Mathf.CeilToInt(7.5f);
        System.Console.WriteLine(
            $"winners list: {string.Join(", ", EndGameState.Winners.Select(item => item.PlayerName).ToList())}");
        for (var i = 0; i < EndGameState.Winners.Count; i++)
        {
            var winningPlayerData = EndGameState.Winners[i];
            var num2 = (i % 2 == 0) ? -1 : 1;
            var num3 = (i + 1) / 2;
            var num4 = (float) num3 / num;
            var num5 = Mathf.Lerp(1f, 0.75f, num4);
            var num6 = (float) (i == 0 ? -8 : -1);
            var poolablePlayer =
                Object.Instantiate(__instance.PlayerPrefab, __instance.transform);
            poolablePlayer.transform.localPosition = new Vector3(
                1f * num2 * num3 * num5,
                FloatRange.SpreadToEdges(-1.125f, 0f, num3, num),
                num6 + num3 * 0.01f) * 0.9f;
            var num7 = Mathf.Lerp(1f, 0.65f, num4) * 0.9f;
            var vector = new Vector3(num7, num7, 1f);
            poolablePlayer.transform.localScale = vector;
            poolablePlayer.UpdateFromPlayerOutfit(
                winningPlayerData,
                PlayerMaterial.MaskType.ComplexUI,
                winningPlayerData.IsDead,
                true);
            if (winningPlayerData.IsDead)
            {
                poolablePlayer.cosmetics.currentBodySprite.BodySprite.sprite =
                    poolablePlayer.cosmetics.currentBodySprite.GhostSprite;
                poolablePlayer.SetDeadFlipX(i % 2 == 0);
            }
            else
            {
                poolablePlayer.SetFlipX(i % 2 == 0);
            }

            poolablePlayer.cosmetics.nameText.color = Color.white;
            poolablePlayer.cosmetics.nameText.transform.localScale =
                new Vector3(1f / vector.x, 1f / vector.y, 1f / vector.z);
            var localPosition = poolablePlayer.cosmetics.nameText.transform.localPosition;
            localPosition = new Vector3(
                localPosition.x,
                localPosition.y,
                -15f);
            poolablePlayer.cosmetics.nameText.transform.localPosition = localPosition;
            poolablePlayer.cosmetics.nameText.text = winningPlayerData.PlayerName;

            var currentPlayer = PlayerCache.GetPlayerByName(winningPlayerData.PlayerName);
            if (currentPlayer != null)
            {
                var role = CustomRole.GetByPlayer(currentPlayer);
                if (role != null)
                {
                    poolablePlayer.cosmetics.nameText.text += $"\n{Colors.Cs(role.Color, role.Name)}";
                }
            }
        }

        // Additional code
        var bonusText = Object.Instantiate(__instance.WinText.gameObject);
        var position1 = __instance.WinText.transform.position;
        bonusText.transform.position = new Vector3(position1.x, position1.y - 0.5f, position1.z);
        bonusText.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        var textRenderer = bonusText.GetComponent<TMPro.TMP_Text>();
        textRenderer.text = EndGameState.State.Title;
        textRenderer.color = Colors.FromHex(EndGameState.State.Color);
        __instance.BackgroundBar.material.SetColor(_color, Colors.FromHex(EndGameState.State.Color));

        EndGameState.Reset();
    }
}

public class SerializableEndGameState
{
    public bool IsEndGame { get; set; }
    public string Color { get; set; } = "#00FF00";
    public string Title { get; set; } = "No winer";
    public List<byte> Winners { get; set; } = new();
}
