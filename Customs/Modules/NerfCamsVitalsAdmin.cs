using System.Linq;
using EnoMod.Kernel;
using EnoMod.Utils;
using UnityEngine;

namespace EnoMod.Customs.Modules;

public static class NerfCamsVitalsAdmin
{
    public static CustomOption EnableInfosNerf;
    public static CustomOption MaxPlayersToUseCameras;
    public static CustomOption MaxPlayersToUseVitals;
    public static CustomOption MaxPlayersToUseAdmin;

    private static Color _color = Colors.FromHex("#4f4545");

    [EnoHook(CustomHooks.LoadCustomOptions)]
    public static Hooks.Result LoadCustomOptions()
    {
        EnableInfosNerf = Singleton<CustomOption.Holder>.Instance.Settings.CreateBool(
            nameof(EnableInfosNerf),
            Colors.Cs(_color, "Nerf information"),
            false);
        MaxPlayersToUseCameras = Singleton<CustomOption.Holder>.Instance.Settings.CreateFloatList(
            nameof(MaxPlayersToUseCameras),
            Colors.Cs(_color, "Disable cameras above"),
            0f,
            15f,
            0f,
            1f,
            EnableInfosNerf,
            string.Empty,
            " players left");
        MaxPlayersToUseVitals = Singleton<CustomOption.Holder>.Instance.Settings.CreateFloatList(
            nameof(MaxPlayersToUseVitals),
            Colors.Cs(_color, "Disable vitals above"),
            0f,
            15f,
            0f,
            1f,
            EnableInfosNerf,
            string.Empty,
            " players left");
        MaxPlayersToUseAdmin = Singleton<CustomOption.Holder>.Instance.Settings.CreateFloatList(
            nameof(MaxPlayersToUseAdmin),
            Colors.Cs(_color, "Disable admin above"),
            0f,
            15f,
            0f,
            1f,
            EnableInfosNerf,
            string.Empty,
            " players left");

        return Hooks.Result.Continue;
    }

    public static int PlayersLeft
    {
        get
        {
            return PlayerCache.AllPlayers.ToArray().Select(pc => pc.PlayerControl)
                .Count(pc => pc is { Data: { IsDead: false, Disconnected: false } });
        }
    }

    public static bool IsCamsDisabled
    {
        get
        {
            return EnableInfosNerf && PlayersLeft > MaxPlayersToUseCameras;
        }
    }

    public static bool IsVitalsDisabled
    {
        get
        {
            return EnableInfosNerf && PlayersLeft > MaxPlayersToUseVitals;
        }
    }

    public static bool IsAdminDisabled
    {
        get
        {
            return EnableInfosNerf && PlayersLeft > MaxPlayersToUseAdmin;
        }
    }

    [EnoHook(CustomHooks.VitalsUpdated)]
    public static Hooks.Result UpdateVitals(VitalsMinigame vitalsInstance)
    {
        vitalsInstance.SabText.color = Color.white;

        if (Utils.AmongUs.IsCommunicationsDisabled())
        {
            vitalsInstance.SabText.text = "[ C O M M S  D I S A B L E D ]";
            vitalsInstance.SabText.SetFaceColor(Palette.ImpostorRed);
        }
        else
        {
            vitalsInstance.SabText.text = "[ V I T A L S  D I S A B L E D ]\n\nabove " +
                                          (int) MaxPlayersToUseVitals + " players";
            vitalsInstance.SabText.SetFaceColor(new Color32(255, 200, 0, byte.MaxValue));
        }

        if (!vitalsInstance.SabText.isActiveAndEnabled && IsVitalsDisabled)
        {
            vitalsInstance.SabText.gameObject.SetActive(true);
            foreach (var vitals in vitalsInstance.vitals)
            {
                vitals.gameObject.SetActive(false);
            }
        }

        return IsVitalsDisabled ? Hooks.Result.ReturnFalse : Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.PlanetCameraUpdated)]
    public static Hooks.Result UpdateCameras(PlanetSurveillanceMinigame cameras)
    {
        cameras.SabText.color = Color.white;
        if (Utils.AmongUs.IsCommunicationsDisabled())
        {
            cameras.SabText.text = "[ C O M M S  D I S A B L E D ]";
            cameras.SabText.SetFaceColor(Palette.ImpostorRed);
        }
        else
        {
            cameras.SabText.text = "[ C A M S  D I S A B L E D ]\n\nabove " +
                                   (int) MaxPlayersToUseCameras + " players";
            cameras.SabText.SetFaceColor(new Color32(255, 200, 0, byte.MaxValue));
        }

        if (!cameras.isStatic && IsCamsDisabled)
        {
            cameras.isStatic = true;
            cameras.ViewPort.sharedMaterial = cameras.StaticMaterial;
            cameras.SabText.gameObject.SetActive(true);
        }

        return IsCamsDisabled ? Hooks.Result.ReturnFalse : Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.PlanetCameraNextUpdated)]
    public static Hooks.Result UpdateNextCamera(PlanetSurveillanceMinigame minigame, int direction)
    {
        if (!IsCamsDisabled) return Hooks.Result.Continue;

        if (direction != 0 && Constants.ShouldPlaySfx())
        {
            SoundManager.Instance.PlaySound(minigame.ChangeSound, false, 1f);
        }

        minigame.Dots[minigame.currentCamera].sprite = minigame.DotDisabled;
        minigame.currentCamera = (minigame.currentCamera + direction).Wrap(minigame.survCameras.Length);
        minigame.Dots[minigame.currentCamera].sprite = minigame.DotEnabled;
        var camera = minigame.survCameras[minigame.currentCamera];
        minigame.Camera.transform.position =
            camera.transform.position + minigame.survCameras[minigame.currentCamera].Offset;
        minigame.LocationName.text = camera.CamName;
        return Hooks.Result.ReturnFalse;
    }

    [EnoHook(CustomHooks.CamerasUpdated)]
    public static Hooks.Result UpdateCamerasView(SurveillanceMinigame cameras)
    {
        for (var j = 0; j < cameras.ViewPorts.Length; j++)
        {
            cameras.SabText[j].color = Color.white;
            if (Utils.AmongUs.IsCommunicationsDisabled())
            {
                cameras.SabText[j].text = "[ C O M M S  D I S A B L E D ]";
                cameras.SabText[j].SetFaceColor(Palette.ImpostorRed);
            }
            else
            {
                cameras.SabText[j].text = "[ C A M S  D I S A B L E D ]\n\nabove " +
                                          (int) MaxPlayersToUseCameras + " players";
                cameras.SabText[j].SetFaceColor(new Color32(255, 200, 0, byte.MaxValue));
            }
        }

        if (cameras.isStatic || !IsCamsDisabled)
            return IsCamsDisabled ? Hooks.Result.ReturnFalse : Hooks.Result.Continue;
        cameras.isStatic = true;
        for (var j = 0; j < cameras.ViewPorts.Length; j++)
        {
            cameras.ViewPorts[j].sharedMaterial = cameras.StaticMaterial;
            cameras.SabText[j].gameObject.SetActive(true);
        }

        return IsCamsDisabled ? Hooks.Result.ReturnFalse : Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.AdminTableOpened)]
    private static Hooks.Result UpdateAdminOverlay(MapCountOverlay adminTable)
    {
        adminTable.SabotageText.color = Color.white;
        var commsActive = Utils.AmongUs.IsCommunicationsDisabled();
        if ((!adminTable.isSab && commsActive) || IsAdminDisabled)
        {
            adminTable.isSab = true;
            adminTable.SabotageText.gameObject.SetActive(true);
            if (commsActive)
            {
                adminTable.SabotageText.text = "[ C O M M S  D I S A B L E D ]";
                adminTable.SabotageText.SetFaceColor(Palette.ImpostorRed);
                adminTable.BackgroundColor.SetColor(Palette.DisabledGrey);
            }
            else
            {
                adminTable.SabotageText.text = "[ A D M I N  D I S A B L E D ]\n\nabove " +
                                               (int) MaxPlayersToUseAdmin + " players";
                adminTable.SabotageText.SetFaceColor(new Color32(255, 200, 0, byte.MaxValue));
                adminTable.BackgroundColor.SetColor(Palette.Black);
            }

            return Hooks.Result.ReturnFalse;
        }

        if (!adminTable.isSab || commsActive) return Hooks.Result.Continue;

        adminTable.isSab = false;
        adminTable.BackgroundColor.SetColor(Color.green);
        adminTable.SabotageText.gameObject.SetActive(false);

        foreach (var counterArea in adminTable.CountAreas)
        {
            if (!Utils.AmongUs.IsCommunicationsDisabled() && MapUtilities.CachedShipStatus != null)
            {
                var plainShipRoom = MapUtilities.CachedShipStatus.FastRooms[counterArea.RoomType];

                if (plainShipRoom != null && plainShipRoom.roomArea)
                {
                    var num = plainShipRoom.roomArea.OverlapCollider(adminTable.filter, adminTable.buffer);
                    var num2 = num;
                    for (var j = 0; j < num; j++)
                    {
                        var collider2D = adminTable.buffer[j];
                        if (collider2D.tag != "DeadBody")
                        {
                            var component = collider2D.GetComponent<PlayerControl>();
                            if (!component || component.Data == null || component.Data.Disconnected ||
                                component.Data.IsDead)
                            {
                                num2--;
                            }
                        }
                    }

                    counterArea.UpdateCount(num2);
                }
                else
                {
                    Debug.LogWarning("Couldn't find counter for:" + counterArea.RoomType);
                }
            }
            else
            {
                counterArea.UpdateCount(0);
            }
        }

        return Hooks.Result.Continue;
    }
}
