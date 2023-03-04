using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using EnoMod.Kernel;
using EnoMod.Utils;
using Reactor.Networking.Attributes;
using UnityEngine;

namespace EnoMod.Customs.Modules;

public class ShieldsState
{
    public List<PlayerShielded> PlayersShielded { get; set; } = new();
    public byte? FirstPlayerKilledInThisGame { get; set; }
}

[EnoSingleton]
public class Shields
{
    public ShieldsState State = new();

    public CustomOption ShieldFirstKilledPlayer;
    public CustomOption RemoveShieldInFirstKill;
    public CustomOption RemoveShieldInFirstMeeting;

    private static readonly int Outline = Shader.PropertyToID("_Outline");
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
    private static readonly Color ShieldColor = Colors.FromHex("#186cad");
    private static readonly Color NoShieldColor = Colors.FromHex("#000000");
    private static readonly float ShieldSpriteSize = 1f;
    private static readonly float SpriteSize;

    [EnoHook(CustomHooks.LoadCustomOptions)]
    public Hooks.Result LoadCustomOptions()
    {
        ShieldFirstKilledPlayer = Singleton<CustomOption.Holder>.Instance.Settings.CreateBool(
            nameof(ShieldFirstKilledPlayer),
            Colors.Cs("#0780a8", "Shield first killed player"),
            false);
        RemoveShieldInFirstKill = Singleton<CustomOption.Holder>.Instance.Settings.CreateBool(
            nameof(RemoveShieldInFirstKill),
            Colors.Cs("#15a0cf", "Remove shield on first kill"),
            false,
            ShieldFirstKilledPlayer);
        RemoveShieldInFirstMeeting = Singleton<CustomOption.Holder>.Instance.Settings.CreateBool(
            nameof(RemoveShieldInFirstMeeting),
            Colors.Cs("#15a0cf", "Remove shield on first meeting"),
            false,
            ShieldFirstKilledPlayer);
        return Hooks.Result.Continue;
    }

    public bool IsShielded(PlayerControl player)
    {
        return IsShielded(player.PlayerId);
    }

    public bool IsShielded(byte playerId)
    {
        return State.PlayersShielded.Any(ps => ps.PlayerId == playerId);
    }

    public PlayerShielded GetPlayer(byte playerId)
    {
        return State.PlayersShielded.Find(ps => ps.PlayerId == playerId) ??
               throw new EnoModException("Player not shielded");
    }

    public void AddShieldedPlayer(PlayerControl player)
    {
        AddShieldedPlayer(player.PlayerId);
    }

    public void AddShieldedPlayer(byte playerId)
    {
        if (IsShielded(playerId)) return;
        State.PlayersShielded.Add(new PlayerShielded()
        {
            AnyoneCanSeeMurderAttempt = false,
            AnyoneCanSeeShield = true,
            PlayerId = playerId,
            RemoveShieldOnFirstKill = RemoveShieldInFirstKill,
            RemoveShieldOnFirstMeeting = RemoveShieldInFirstMeeting,
        });
        Share();
    }

    public void RemoveShieldedPlayer(PlayerControl player)
    {
        RemoveShieldedPlayer(player.PlayerId);
    }

    public void RemoveShieldedPlayer(byte playerId)
    {
        if (!IsShielded(playerId)) return;
        for (var psIndex = 0; psIndex < State.PlayersShielded.Count; psIndex++)
        {
            if (State.PlayersShielded[psIndex].PlayerId != playerId) continue;
            State.PlayersShielded.RemoveAt(psIndex);
            Share();
            return;
        }
    }

    public void Share()
    {
        if (PlayerCache.LocalPlayer == null) return;
        if (!Utils.AmongUs.IsHost()) return;
        RpcUpdateGameState(PlayerCache.LocalPlayer, Serializer.Serialize(State));
    }

    public bool Murder(PlayerControl killer, PlayerControl target)
    {
        if (CheckMurderAttempt(killer, target) == Game.MurderAttemptResult.PerformKill)
        {
            return true;
        }

        return false;
    }

    public Game.MurderAttemptResult CheckMurderAttempt(PlayerControl? killer, PlayerControl target)
    {
        // Modified vanilla checks
        return Utils.AmongUs.IsGameOver() || killer == null || killer.Data == null || killer.Data.IsDead ||
               killer.Data.Disconnected || target == null || target.Data == null || target.Data.IsDead ||
               target.Data.Disconnected || IsShielded(target)
            ? Game.MurderAttemptResult.SuppressKill
            : Game.MurderAttemptResult.PerformKill;
    }

    [EnoHook(CustomHooks.PlayerControlFixedUpdatePostfix)]
    public static Hooks.Result PlayerControlFixedUpdatePostfix(PlayerControl player)
    {
        RenderImpostorOutline();
        RenderShieldOutline(player);
        RenderRoleOutline();
        RenderPlayerColor();

        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.PlayerControlFixedUpdatePrefix)]
    public static Hooks.Result PlayerControlFixedUpdatePrefix(PlayerControl player)
    {
        RenderShieldOutline(player);

        return Hooks.Result.Continue;
    }

    private static void RenderPlayerColor()
    {
        if (PlayerCache.LocalPlayer == null) return;
        var role = CustomRole.GetLocalPlayerRole();
        if (role == null) return;
        var nameText = PlayerCache.LocalPlayer.PlayerControl.cosmetics.nameText;
        nameText.color = role.Color;
    }

    private static void RenderImpostorOutline()
    {
        if (PlayerControl.LocalPlayer == null) return;
        if (PlayerControl.LocalPlayer.Data.RoleType != RoleTypes.Impostor) return;

        var target = RenderTarget(false, false, new List<PlayerControl>());
        if (target == null) return;

        if (Singleton<Shields>.Instance.IsShielded(target))
        {
            RenderPlayerOutline(
                target.PlayerId,
                Colors.Blend(new List<Color> { Color.red, ShieldColor }));
        }
        else
        {
            RenderPlayerOutline(target.PlayerId, Color.red);
        }
    }

    private static void RenderRoleOutline()
    {
        if (PlayerCache.LocalPlayer == null) return;
        var role = CustomRole.GetByPlayer(PlayerCache.LocalPlayer);
        if (role == null) return;
        if (!role.CanTarget) return;
        var targetId = RenderTarget(false, false, new List<PlayerControl>())?.PlayerId;
        role.GetPlayer(PlayerCache.LocalPlayer.PlayerId).TargetId = targetId;
        if (targetId != null && Singleton<Shields>.Instance.IsShielded((byte) targetId))
        {
            RenderPlayerOutline(targetId, Colors.Blend(new List<Color> { role.Color, ShieldColor }));
        }
    }

    private static void RenderPlayerOutline(byte? targetId, Color color)
    {
        if (targetId == null) return;
        var target = PlayerControl.AllPlayerControls.ToArray().ToList().Find(pc => pc.PlayerId == targetId);
        if (target == null || target.cosmetics.currentBodySprite.BodySprite == null) return;
        target.cosmetics.currentBodySprite.BodySprite.material.SetFloat(Outline, 1f);
        target.cosmetics.currentBodySprite.BodySprite.material.SetColor(OutlineColor, color);
    }

    private static PlayerControl? RenderTarget(
        bool onlyCrewmates,
        bool playerInVent,
        List<PlayerControl> untargetablePlayers,
        PlayerControl? targetingPlayer = null)
    {
        PlayerControl? result = null;
        var num = GameOptionsData.KillDistances[
            Mathf.Clamp(GameOptionsManager.Instance.currentNormalGameOptions.KillDistance, 0, 2)];
        if (PlayerControl.LocalPlayer == null) return result;
        if (!MapUtilities.CachedShipStatus) return result;
        if (targetingPlayer == null) targetingPlayer = PlayerControl.LocalPlayer;
        if (targetingPlayer.Data.IsDead) return result;

        var truePosition = targetingPlayer.GetTruePosition();
        foreach (var playerInfo in GameData.Instance.AllPlayers.GetFastEnumerator())
        {
            if (playerInfo.Disconnected || playerInfo.PlayerId == targetingPlayer.PlayerId || playerInfo.IsDead ||
                (onlyCrewmates && playerInfo.Role.IsImpostor)) continue;

            var @object = playerInfo.Object;
            if (untargetablePlayers != null && untargetablePlayers.Any(x => x == @object)) continue;

            if (!@object || (@object.inVent && !playerInVent)) continue;

            var vector = @object.GetTruePosition() - truePosition;
            var magnitude = vector.magnitude;
            if (!(magnitude <= num) || PhysicsHelpers.AnyNonTriggersBetween(
                    truePosition,
                    vector.normalized,
                    magnitude,
                    Constants.ShipAndObjectsMask)) continue;

            result = @object;
            num = magnitude;
        }

        return result;
    }

    private static void RenderShieldOutline(PlayerControl player)
    {
        if (PlayerCache.LocalPlayer == null) return;
        foreach (var playerCache in PlayerCache.AllPlayers)
        {
            if (playerCache == null) continue;
            var target = playerCache.PlayerControl;
            if (target.cosmetics == null || target.cosmetics.currentBodySprite?.BodySprite == null) return;
            if (Singleton<Shields>.Instance.IsShielded(target))
            {
                target.cosmetics.currentBodySprite.BodySprite.material.SetFloat(Outline, ShieldSpriteSize);
                target.cosmetics.currentBodySprite.BodySprite.material.SetColor(OutlineColor, ShieldColor);
            }
            else
            {
                target.cosmetics.currentBodySprite.BodySprite.material.SetFloat(Outline, SpriteSize);
                target.cosmetics.currentBodySprite.BodySprite.material.SetColor(OutlineColor, NoShieldColor);
            }
        }
    }

    [EnoHook(CustomHooks.MeetingEnded)]
    public Hooks.Result OnMeetingEnd(ExileController exileController, GameData.PlayerInfo? exiled)
    {
        if (exiled != null && IsShielded(exiled.PlayerId))
        {
            RemoveShieldedPlayer(exiled.PlayerId);
        }

        foreach (var ps in State.PlayersShielded.Where(ps =>
                     ps.RemoveShieldOnFirstMeeting || (ps.RemoveShieldOnFirstKill && exiled != null)))
        {
            RemoveShieldedPlayer(ps.PlayerId);
        }

        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.IntroCutsceneDestroying)]
    public static Hooks.Result IntroCutsceneDestroying(IntroCutscene cutscene)
    {
        if (!Utils.AmongUs.IsHost()) return Hooks.Result.Continue;
        if (Singleton<Shields>.Instance.State.FirstPlayerKilledInThisGame == null) return Hooks.Result.Continue;
        Singleton<Shields>.Instance.State.PlayersShielded.Add(
            new PlayerShielded
            {
                PlayerId = (byte) Singleton<Shields>.Instance.State.FirstPlayerKilledInThisGame,
                RemoveShieldOnFirstKill = Singleton<Shields>.Instance.RemoveShieldInFirstKill,
                RemoveShieldOnFirstMeeting = Singleton<Shields>.Instance.RemoveShieldInFirstMeeting,
            });
        Singleton<Shields>.Instance.State.FirstPlayerKilledInThisGame = null;
        Singleton<Shields>.Instance.Share();
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.PlayerControlCheckMurder)]
    public static Hooks.Result PlayerControlCheckMurder(PlayerControl killer, PlayerControl target)
    {
        return Singleton<Shields>.Instance.CheckMurderAttempt(killer, target) == Game.MurderAttemptResult.PerformKill
            ? Hooks.Result.ReturnTrue
            : Hooks.Result.ReturnFalse;
    }


    [MethodRpc((uint) CustomRpc.ShareGameState)]
    public static void RpcUpdateGameState(PlayerControl sender, string data)
    {
        if (PlayerCache.LocalPlayer == null) return;
        if (sender.PlayerId == PlayerCache.LocalPlayer.PlayerId) return;
        Singleton<Shields>.Instance.State = Serializer.Deserialize<ShieldsState>(data);
    }
}

public class PlayerShielded
{
    public byte PlayerId { get; set; }
    public bool RemoveShieldOnFirstKill { get; set; }
    public bool RemoveShieldOnFirstMeeting { get; set; }
    public bool AnyoneCanSeeShield { get; set; } = true;
    public bool AnyoneCanSeeMurderAttempt { get; set; }
}
