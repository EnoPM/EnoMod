using System.Collections.Generic;
using System.Linq;
using EnoMod.Kernel;
using EnoMod.Utils;
using UnityEngine;

namespace EnoMod.Customs.Modules;

public static class HideNameNearVent
{
    private const float DistanceOfNearVentToHidePlayerName = 2.05f;

    private static List<Vent> _vents = new();

    public static CustomOption HideNamesOfPlayersNearVent;

    [EnoHook(CustomHooks.LoadCustomOptions)]
    public static void CreateCustomOptions()
    {
        HideNamesOfPlayersNearVent = Singleton<CustomOption.Holder>.Instance.Settings.CreateBool(
            nameof(HideNamesOfPlayersNearVent),
            Colors.Cs("#435570", "Hide name of players near vent"),
            false);
    }

    [EnoHook(CustomHooks.ShipStatusBegin)]
    public static Hooks.Result ShipStatusBegin(ShipStatus shipStatus)
    {
        _vents = Object.FindObjectsOfType<Vent>().ToList();
        System.Console.WriteLine($"Vents loaded : {_vents.Count}");
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.PlayerControlFixedUpdatePostfix)]
    public static Hooks.Result PlayerControlFixedUpdatePostfix(PlayerControl player)
    {
        if (HideNamesOfPlayersNearVent)
        {
            HideNameOfPlayersNearVents();
        }
        return Hooks.Result.Continue;
    }

    private static void HideNameOfPlayersNearVents()
    {
        if (_vents.Count == 0) return;
        if (!AmongUsClient.Instance.IsGameStarted) return;
        if (PlayerCache.LocalPlayer == null) return;
        foreach (var playerCache in PlayerCache.AllPlayers)
        {
            var player = playerCache.PlayerControl;
            var distance = GetLowestValue(_vents
                .Select(vent => Vector3.Distance(vent.transform.position, player.transform.position)).ToList());
            var isNearVent = distance <= DistanceOfNearVentToHidePlayerName;
            var nameText = PlayerCache.LocalPlayer.PlayerControl.cosmetics.nameText;
            nameText.color = nameText.color.SetAlpha(isNearVent ? 0f : 255f);
        }
    }

    private static float GetLowestValue(List<float> list)
    {
        list.Sort((a, b) => a > b ? 1 : -1);
        return list.First();
    }
}
