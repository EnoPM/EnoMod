using AmongUs.GameOptions;
using EnoMod.Kernel;
using UnityEngine;

namespace EnoMod.Customs.Roles;

[EnoSingleton]
public class Sheriff : CustomRole
{
    private CustomOption? _couldown;
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

    [EnoHook(CustomHooks.LoadCustomOptions)]
    public Hooks.Result LoadCustomOptions()
    {
        CreateCustomOptions();
        System.Console.WriteLine("Sheriff LoadCustomOptions");
        if (_couldown != null) return Hooks.Result.Continue;
        _couldown = Singleton<CustomOption.Holder>.Instance.Roles.CreateFloatList(
            $"{Name}Couldown",
            CustomOption.Cs(Color, $"{Name} couldown"),
            10f,
            120f,
            25f,
            2.5f,
            NumberCustomOption,
            string.Empty,
            "s");
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.LoadCustomButtons)]
    public Hooks.Result CreateCustomButtons(HudManager hudManager)
    {
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
        return Hooks.Result.Continue;
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
        var targetRole = GetByPlayer(target);

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
            if (target.Data.RoleType != RoleTypes.Impostor && targetRole is not { Team: Teams.Neutral })
            {
                target = player;
            }
            Rpc.MurderAttempt(
                player,
                Rpc.Serialize(new Rpc.MurderInfo { Murder = player.PlayerId, Target = target.PlayerId }));
        }

        OnMeetingEnd();
    }
}
