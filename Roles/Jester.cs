using EnoMod.Modules;
using EnoMod.Patches;
using EnoMod.Modules;
using UnityEngine;

namespace EnoMod.Roles;

public class Jester : CustomRole
{
    private CustomSetting? _couldown;
    private CustomSetting? _duration;
    private CustomButton? _jesterButton;

    private const string JesterSabotageText = "[ J E S T E R  S A B O T A G E ]";

    private byte? _winner;

    public bool JesterSabotageActive;

    public Jester()
    {
        Id = 1011;
        Team = Teams.Neutral;
        Name = "Jester";
        Description = "Get voted out";
        HexColor = "#c744c5";
        CanTarget = false;
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
        _duration = CustomSettings.Roles.AddCustomSetting(CustomSetting.CreateFloatList(
            $"{Name}Duration",
            CustomSetting.Cs(GetColor(), $"{Name} sabotage duration"),
            0f,
            120f,
            30f,
            5f,
            NumberCustomOption,
            string.Empty,
            "s"));
    }

    public override void CreateCustomButtons(HudManager hudManager)
    {
        base.CreateCustomButtons(hudManager);
        var jesterButtonSprite = Helpers.LoadSpriteFromResources("EnoMod.Resources.Buttons.Jester.png", 115f);
        _jesterButton = new CustomButton(
            OnJesterButtonClick,
            HasJesterButton,
            CouldUseJesterButton,
            OnMeetingEnd,
            jesterButtonSprite != null ? jesterButtonSprite : hudManager.KillButton.graphic.sprite,
            CustomButton.ButtonPositions.UpperRowRight,
            hudManager,
            KeyCode.F,
            true,
            _duration ?? 10f,
            OnJesterButtonEffectEnd
        );
        if (_couldown == null || _duration == null) return;

        _jesterButton.Timer = _couldown;
        _jesterButton.EffectDuration(_duration);
    }

    [RoleHook(Hooks.AmongUs.CamerasUpdated)]
    public Hooks.Result OnCamerasUpdated(SurveillanceMinigame cameras)
    {
        if (!Reference.Jester.JesterSabotageActive || BlockUtilitiesPatch.IsCommsActive()) return Hooks.Result.ReturnTrue;
        for (var j = 0; j < cameras.ViewPorts.Length; j++)
        {
            cameras.SabText[j].color = Color.white;
            cameras.SabText[j].text = JesterSabotageText;
            cameras.SabText[j].SetFaceColor(GetColor());
        }

        if (cameras.isStatic) return Hooks.Result.ReturnFalse;
        cameras.isStatic = true;
        for (var j = 0; j < cameras.ViewPorts.Length; j++)
        {
            cameras.ViewPorts[j].sharedMaterial = cameras.StaticMaterial;
            cameras.SabText[j].gameObject.SetActive(true);
        }

        return Hooks.Result.ReturnFalse;
    }

    [RoleHook(Hooks.AmongUs.PlanetCameraUpdated)]
    public Hooks.Result OnPlanetCameraUpdated(PlanetSurveillanceMinigame cameras)
    {
        if (!Reference.Jester.JesterSabotageActive || BlockUtilitiesPatch.IsCommsActive()) return Hooks.Result.Continue;
        cameras.SabText.text = JesterSabotageText;
        cameras.SabText.SetFaceColor(GetColor());
        if (!cameras.isStatic)
        {
            cameras.isStatic = false;
            cameras.ViewPort.sharedMaterial = cameras.StaticMaterial;
            cameras.SabText.gameObject.SetActive(true);
        }

        return Hooks.Result.ReturnFalse;
    }

    [RoleHook(Hooks.AmongUs.VitalsUpdated)]
    public Hooks.Result OnVitalsUpdated(VitalsMinigame vitals)
    {
        if (!Reference.Jester.JesterSabotageActive || BlockUtilitiesPatch.IsCommsActive()) return Hooks.Result.Continue;
        vitals.SabText.color = Color.white;
        vitals.SabText.text = JesterSabotageText;
        vitals.SabText.SetFaceColor(GetColor());
        if (vitals.SabText.isActiveAndEnabled) return Hooks.Result.ReturnFalse;

        vitals.SabText.gameObject.SetActive(true);
        foreach (var vital in vitals.vitals)
        {
            vital.gameObject.SetActive(false);
        }

        return Hooks.Result.ReturnFalse;
    }

    [RoleHook(Hooks.AmongUs.AdminTableOpened)]
    public Hooks.Result OnAdminTableOpened(MapCountOverlay adminTable)
    {
        if (!Reference.Jester.JesterSabotageActive || BlockUtilitiesPatch.IsCommsActive()) return Hooks.Result.Continue;
        adminTable.SabotageText.color = Color.white;
        adminTable.isSab = true;
        adminTable.SabotageText.gameObject.SetActive(true);
        adminTable.SabotageText.text = JesterSabotageText;
        adminTable.SabotageText.SetFaceColor(GetColor());
        adminTable.BackgroundColor.SetColor(Palette.DisabledGrey);

        return Hooks.Result.ReturnFalse;
    }

    [RoleHook(Hooks.AmongUs.MeetingEnded)]
    public Hooks.Result OnMeetingEnd(ExileController exileController, GameData.PlayerInfo? exiled)
    {
        OnMeetingEnd();
        if (exiled == null) return Hooks.Result.Continue;
        if (HasPlayer(exiled.PlayerId))
        {
            _winner = exiled.PlayerId;
        }

        return Hooks.Result.Continue;
    }

    [RoleHook(Hooks.AmongUs.PlanetCameraNextUpdated)]
    public static Hooks.Result OnPlanetCameraNextUpdated(PlanetSurveillanceMinigame minigame, int direction)
    {
        if (!Reference.Jester.JesterSabotageActive) return Hooks.Result.Continue;

        if (direction != 0 && Constants.ShouldPlaySfx())
        {
            SoundManager.Instance.PlaySound(minigame.ChangeSound, false, 1f);
        }

        minigame.Dots[minigame.currentCamera].sprite = minigame.DotDisabled;
        minigame.currentCamera = (minigame.currentCamera + direction).Wrap(minigame.survCameras.Length);
        minigame.Dots[minigame.currentCamera].sprite = minigame.DotEnabled;
        var survCamera = minigame.survCameras[minigame.currentCamera];
        minigame.Camera.transform.position =
            survCamera.transform.position + minigame.survCameras[minigame.currentCamera].Offset;
        minigame.LocationName.text = survCamera.CamName;
        return Hooks.Result.ReturnFalse;
    }

    public override bool TriggerEndGame()
    {
        return _winner != null;
    }

    private void OnMeetingEnd()
    {
        if (_couldown != null && _jesterButton != null)
        {
            _jesterButton.Timer = _couldown;
        }

        foreach (var rp in Players)
        {
            rp.TargetId = null;
        }
    }

    private bool CouldUseJesterButton()
    {
        if (_jesterButton == null || _couldown == null) return false;
        if (_jesterButton.Timer > 0f) return false;
        return true;
    }

    private bool HasJesterButton()
    {
        if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return false;
        var role = GetLocalPlayerRole();
        return role != null && role.Id == Id;
    }

    private void OnJesterButtonClick()
    {
        var player = PlayerCache.LocalPlayer;
        if (player == null) return;
        Rpc.JesterSabotageStart(player);
        OnMeetingEnd();
    }

    private void OnJesterButtonEffectEnd()
    {
        var player = PlayerCache.LocalPlayer;
        if (player == null) return;
        Rpc.JesterSabotageEnd(player);
        OnMeetingEnd();
    }
}
