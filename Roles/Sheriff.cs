using AmongUs.GameOptions;
using EnoMod.Modules;
using UnityEngine;

namespace EnoMod.Roles;

public class Sheriff : CustomRole
{
    private CustomSetting? _couldown;
    private CustomButton? _killButton;

    public Sheriff()
    {
        Id = 1010;
        Team = Teams.Crewmate;
        Name = "Sheriff";
        Description = "Kill the impostors";
        HexColor = "#f8cd46";
        CanTarget = true;
    }

    public override void CreateCustomOptions()
    {
        base.CreateCustomOptions();
        if (_couldown != null) return;
        _couldown = CustomSettings.Roles.AddCustomSetting(CustomSetting.CreateFloatList(
            $"{Name}Couldown",
            CustomSetting.Cs(GetColor(), $"{Name} couldown"),
            10f,
            120f,
            25f,
            2.5f,
            NumberCustomOption,
            string.Empty,
            "s"));
    }

    public override void CreateCustomButtons(HudManager hudManager)
    {
        base.CreateCustomButtons(hudManager);
        _killButton = new CustomButton(
            OnKillButtonClick,
            HasKillButton,
            CouldUseKillButton,
            OnMeetingEnd,
            hudManager.KillButton.graphic.sprite,
            CustomButton.ButtonPositions.UpperRowRight,
            hudManager,
            KeyCode.F);
        if (_couldown != null)
        {
            _killButton.Timer = _couldown;
        }
    }

    private void OnMeetingEnd()
    {
        if (_couldown != null && _killButton != null)
        {
            _killButton.Timer = _couldown;
        }

        foreach (var rp in Players)
        {
            rp.TargetId = null;
        }
    }

    private bool CouldUseKillButton()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return false;
        return HasPlayer(player.PlayerId) && GetPlayer(player.PlayerId).TargetId != null;
    }

    private bool HasKillButton()
    {
        if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return false;
        var role = GetLocalPlayerRole();
        return role != null && role.Id == Id;
    }

    private void OnKillButtonClick()
    {
        var player = PlayerCache.LocalPlayer;
        if (player == null) return;
        var rp = Players.Find(rp => rp.PlayerId == player.PlayerId && rp.TargetId != null);
        if (rp?.TargetId == null) return;
        var killer = PlayerCache.GetPlayerById(rp.PlayerId);
        var target = PlayerCache.GetPlayerById((byte) rp.TargetId);
        if (killer == null || target == null) return;
        var targetId = target.PlayerId;
        var targetRole = GetByPlayer(target);
        if (target.Data.RoleType != RoleTypes.Impostor && targetRole != null && targetRole.Team != Teams.Neutral)
        {
            targetId = player.PlayerId;
        }

        System.Console.WriteLine(
            $"{PlayerControl.LocalPlayer.Data.PlayerName}: {killer.Data.PlayerName} want kill {target.Data.PlayerName}");
        if (Game.CheckMurderAttempt(killer, target) == Game.MurderAttemptResult.SuppressKill)
        {
            Rpc.ShieldedMurderAttempt(
                killer,
                Rpc.Serialize(new Rpc.MurderInfo { Murder = killer.PlayerId, Target = target.PlayerId }));
        }
        else
        {
            Rpc.MurderAttempt(
                player,
                Rpc.Serialize(new Rpc.MurderInfo { Murder = player.PlayerId, Target = targetId }));
        }

        OnMeetingEnd();
    }
}
