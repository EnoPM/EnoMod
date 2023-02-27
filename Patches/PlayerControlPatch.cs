using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using EnoMod.Modules;
using HarmonyLib;
using UnityEngine;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public class PlayerControlFixedUpdatePatch
{
    private static readonly int Outline = Shader.PropertyToID("_Outline");
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
    private static readonly Color ShieldColor = Helpers.HexColor("#186cad");
    private static readonly Color NoShieldColor = Helpers.HexColor("#000000");
    private static readonly float ShieldSpriteSize = 1f;
    private static readonly float SpriteSize;

    public static void Postfix(PlayerControl __instance)
    {
        RenderImpostorOutline();
        RenderShieldOutline(__instance);
        CustomRole.Roles.ForEach(role =>
        {
            if (role.Players.Count == 0 || role.Name != CustomRole.GetLocalPlayerRole()?.Name) return;
            if (!role.CanTarget) return;
            foreach (var rp in CustomRole.Roles
                         .Select(cr => cr.Players.Find(rp => rp.PlayerId == PlayerControl.LocalPlayer.PlayerId))
                         .Where(rp => rp != null))
            {
                if (rp == null) continue;
                rp.TargetId = RenderTarget(false, false, new List<PlayerControl>())?.PlayerId;
                if (CustomSettings.ShieldFirstKilledPlayer && GameState.Instance.PlayerShielded != null)
                {
                    var target = RenderTarget(false, false, new List<PlayerControl>());
                    if (target != null && target.name == GameState.Instance.PlayerShielded)
                    {
                        RenderPlayerOutline(
                            rp.TargetId,
                            Helpers.BlendColor(new List<Color> { role.GetColor(), ShieldColor }));
                        return;
                    }
                }

                RenderPlayerOutline(rp.TargetId, role.GetColor());
            }
        });
    }

    public static void Prefix(PlayerControl __instance)
    {
        RenderShieldOutline(__instance);
    }

    public static void RenderImpostorOutline()
    {
        if (PlayerControl.LocalPlayer.Data.RoleType != RoleTypes.Impostor) return;

        var target = RenderTarget(false, false, new List<PlayerControl>());
        if (target == null) return;

        if (CustomSettings.ShieldFirstKilledPlayer && GameState.Instance.PlayerShielded != null &&
            target.name == GameState.Instance.PlayerShielded)
        {
            RenderPlayerOutline(
                target.PlayerId,
                Helpers.BlendColor(new List<Color> { Color.red, ShieldColor }));
        }
        else
        {
            RenderPlayerOutline(target.PlayerId, Color.red);
        }
    }

    public static void RenderPlayerOutline(byte? targetId, Color color)
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

    private static void RenderShieldOutline(PlayerControl playerControl)
    {
        if (!CustomSettings.ShieldFirstKilledPlayer) return;
        if (playerControl == null || PlayerControl.LocalPlayer == null) return;
        PlayerControl.AllPlayerControls.ToArray().ToList().ForEach(target =>
        {
            if (target.cosmetics == null || target.cosmetics.currentBodySprite?.BodySprite == null) return;
            if (GameState.Instance.PlayerShielded == target.Data.PlayerName)
            {
                target.cosmetics.currentBodySprite.BodySprite.material.SetFloat(Outline, ShieldSpriteSize);
                target.cosmetics.currentBodySprite.BodySprite.material.SetColor(OutlineColor, ShieldColor);
            }
            else
            {
                target.cosmetics.currentBodySprite.BodySprite.material.SetFloat(Outline, SpriteSize);
                target.cosmetics.currentBodySprite.BodySprite.material.SetColor(OutlineColor, NoShieldColor);
            }
        });
    }
}
