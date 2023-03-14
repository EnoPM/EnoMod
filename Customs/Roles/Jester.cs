using EnoMod.Kernel;
using Reactor.Networking.Attributes;
using UnityEngine;

namespace EnoMod.Customs.Roles;

[EnoSingleton]
public class Jester : CustomRole
{
    private CustomOption? _couldown;
    private CustomOption? _duration;
    private CustomButton? _jesterButton;

    private const string JesterSabotageText = "[ J E S T E R  S A B O T A G E ]";

    public bool JesterSabotageActive;

    public Jester()
    {
        Id = 1011;
        Team = Teams.Neutral;
        Name = "Jester";
        Description = "Get voted out";
        HexColor = "#c744c5";
        CanTarget = false;
        JesterSabotageActive = false;
        HasTasks = false;
    }

    [EnoHook(CustomHooks.LoadCustomOptions)]
    public Hooks.Result LoadCustomOptions()
    {
        CreateCustomOptions();
        System.Console.WriteLine("Jester LoadCustomOptions");
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
        _duration = Singleton<CustomOption.Holder>.Instance.Roles.CreateFloatList(
            $"{Name}Duration",
            CustomOption.Cs(Color, $"{Name} sabotage duration"),
            0f,
            120f,
            30f,
            5f,
            NumberCustomOption,
            string.Empty,
            "s");
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.LoadCustomButtons)]
    public Hooks.Result CreateCustomButtons(HudManager hudManager)
    {
        var buttonSprite = Utils.Resources.LoadSpriteFromResources("EnoMod.Resources.Buttons.Jester.png", 115f);
        if (buttonSprite == null) return Hooks.Result.Continue;
        _jesterButton = new CustomButton(
            OnJesterButtonClick,
            HasJesterButton,
            CouldUseJesterButton,
            ResetJesterButtonCouldown,
            buttonSprite,
            CustomButton.ButtonPositions.UpperRowRight,
            hudManager,
            KeyCode.F,
            true,
            _duration ?? 10f,
            OnJesterButtonEffectEnd
        );
        if (_couldown == null || _duration == null) return Hooks.Result.Continue;

        _jesterButton.Timer = _couldown;
        _jesterButton.EffectDuration(_duration);
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.CamerasUpdated)]
    public Hooks.Result OnCamerasUpdated(SurveillanceMinigame cameras)
    {
        if (!JesterSabotageActive || Utils.AmongUs.IsCommunicationsDisabled()) return Hooks.Result.ReturnTrue;
        for (var j = 0; j < cameras.ViewPorts.Length; j++)
        {
            cameras.SabText[j].color = Color;
            cameras.SabText[j].text = JesterSabotageText;
            cameras.SabText[j].SetFaceColor(Color);
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

    [EnoHook(CustomHooks.PlanetCameraUpdated)]
    public Hooks.Result OnPlanetCameraUpdated(PlanetSurveillanceMinigame cameras)
    {
        if (!JesterSabotageActive || Utils.AmongUs.IsCommunicationsDisabled()) return Hooks.Result.Continue;
        cameras.SabText.text = JesterSabotageText;
        cameras.SabText.SetFaceColor(Color);
        if (!cameras.isStatic)
        {
            cameras.isStatic = false;
            cameras.ViewPort.sharedMaterial = cameras.StaticMaterial;
            cameras.SabText.gameObject.SetActive(true);
        }

        return Hooks.Result.ReturnFalse;
    }

    [EnoHook(CustomHooks.VitalsUpdated)]
    public Hooks.Result OnVitalsUpdated(VitalsMinigame vitals)
    {
        if (!JesterSabotageActive || Utils.AmongUs.IsCommunicationsDisabled()) return Hooks.Result.Continue;
        vitals.SabText.color = Color.white;
        vitals.SabText.text = JesterSabotageText;
        vitals.SabText.SetFaceColor(Color);
        if (vitals.SabText.isActiveAndEnabled) return Hooks.Result.ReturnFalse;

        vitals.SabText.gameObject.SetActive(true);
        foreach (var vital in vitals.vitals)
        {
            vital.gameObject.SetActive(false);
        }

        return Hooks.Result.ReturnFalse;
    }

    [EnoHook(CustomHooks.AdminTableOpened)]
    public Hooks.Result OnAdminTableOpened(MapCountOverlay adminTable)
    {
        if (!JesterSabotageActive || Utils.AmongUs.IsCommunicationsDisabled()) return Hooks.Result.Continue;
        adminTable.SabotageText.color = Color.white;
        adminTable.isSab = true;
        adminTable.SabotageText.gameObject.SetActive(true);
        adminTable.SabotageText.text = JesterSabotageText;
        adminTable.SabotageText.SetFaceColor(Color);
        adminTable.BackgroundColor.SetColor(Palette.DisabledGrey);

        return Hooks.Result.ReturnFalse;
    }

    [EnoHook(CustomHooks.MeetingEnded)]
    public Hooks.Result OnMeetingEnd(ExileController exileController, GameData.PlayerInfo? exiled)
    {
        System.Console.WriteLine($"Exiled : {exiled?.PlayerName}");
        ResetJesterButtonCouldown();
        if (exiled == null || PlayerCache.LocalPlayer == null) return Hooks.Result.Continue;
        if (HasPlayer(exiled.PlayerId) && exiled.PlayerId == PlayerCache.LocalPlayer.PlayerId)
        {
            System.Console.WriteLine($"Jester set winner : {exiled.PlayerName}");
            if (!EndGameState.IsEndGame)
            {
                EndGameState.State.IsEndGame = true;
                EndGameState.State.Winners.Add(exiled.PlayerId);
                EndGameState.State.Color = HexColor;
                EndGameState.State.Title = $"{Name} win!";
                EndGameState.Share();
            }
        }

        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.PlanetCameraNextUpdated)]
    public Hooks.Result OnPlanetCameraNextUpdated(PlanetSurveillanceMinigame minigame, int direction = 0)
    {
        if (!JesterSabotageActive) return Hooks.Result.Continue;

        if (direction != 0 && Constants.ShouldPlaySfx())
        {
            SoundManager.Instance.PlaySound(minigame.ChangeSound, false);
        }

        minigame.Dots[minigame.currentCamera].sprite = minigame.DotDisabled;
        minigame.currentCamera = (minigame.currentCamera + direction).Wrap(minigame.survCameras.Length);
        minigame.Dots[minigame.currentCamera].sprite = minigame.DotEnabled;
        var surveillanceCamera = minigame.survCameras[minigame.currentCamera];
        minigame.Camera.transform.position =
            surveillanceCamera.transform.position + minigame.survCameras[minigame.currentCamera].Offset;
        minigame.LocationName.text = surveillanceCamera.CamName;
        return Hooks.Result.ReturnFalse;
    }

    private void ResetJesterButtonCouldown()
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
        JesterSabotageStart(player);
        ResetJesterButtonCouldown();
    }

    private void OnJesterButtonEffectEnd()
    {
        var player = PlayerCache.LocalPlayer;
        if (player == null) return;
        JesterSabotageEnd(player);
        ResetJesterButtonCouldown();
    }

    [MethodRpc((uint) CustomRpc.JesterSabotageStart)]
    public static void JesterSabotageStart(PlayerControl _)
    {
        Singleton<Jester>.Instance.JesterSabotageActive = true;
    }

    [MethodRpc((uint) CustomRpc.JesterSabotageEnd)]
    public static void JesterSabotageEnd(PlayerControl _)
    {
        Singleton<Jester>.Instance.JesterSabotageActive = false;
    }
}
