using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace EnoMod.Kernel;

public class PlayerCache
{
    public static readonly Dictionary<IntPtr, PlayerCache> PlayerPointers = new();
    public static readonly List<PlayerCache> AllPlayers = new();
    public static PlayerCache? LocalPlayer { get; set; }

    public Transform transform;
    public PlayerControl PlayerControl;
    public PlayerPhysics PlayerPhysics;
    public CustomNetworkTransform NetworkTransform;
    public GameData.PlayerInfo Data;
    public byte PlayerId;

    public static bool IsLocalPlayer(PlayerControl player)
    {
        return LocalPlayer != null && player.Data.PlayerId == LocalPlayer.Data.PlayerId;
    }

    public static PlayerCache? GetPlayerById(byte playerId)
    {
        return AllPlayers.Find(player => player != null && player.PlayerId == playerId);
    }

    public static PlayerCache? GetPlayerById(int? targetId)
    {
        return targetId == null ? null : GetPlayerById((byte) targetId);
    }

    public static implicit operator bool(PlayerCache? player)
    {
        return player != null && player.PlayerControl;
    }

    public static implicit operator PlayerControl(PlayerCache player) => player.PlayerControl;
    public static implicit operator PlayerPhysics(PlayerCache player) => player.PlayerPhysics;
}

[HarmonyPatch]
public static class PlayerCachePatches
{
    [HarmonyPatch]
    private class CacheLocalPlayerPatch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var type = typeof(PlayerControl).GetNestedTypes(AccessTools.all)
                .FirstOrDefault(t => t.Name.Contains("Start"));
            return AccessTools.Method(type, nameof(IEnumerator.MoveNext));
        }

        [HarmonyPostfix]
        public static void SetLocalPlayer()
        {
            var localPlayer = PlayerControl.LocalPlayer;
            if (!localPlayer)
            {
                PlayerCache.LocalPlayer = null;
                return;
            }

            var cached = PlayerCache.AllPlayers.FirstOrDefault(p => p.PlayerControl.Pointer == localPlayer.Pointer);
            if (cached != null)
            {
                PlayerCache.LocalPlayer = cached;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Awake))]
    [HarmonyPostfix]
    public static void CachePlayerPatch(PlayerControl __instance)
    {
        if (__instance.notRealPlayer) return;
        var player = new PlayerCache
        {
            transform = __instance.transform,
            PlayerControl = __instance,
            PlayerPhysics = __instance.MyPhysics,
            NetworkTransform = __instance.NetTransform,
        };
        PlayerCache.AllPlayers.Add(player);
        PlayerCache.PlayerPointers[__instance.Pointer] = player;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnDestroy))]
    [HarmonyPostfix]
    public static void RemoveCachedPlayerPatch(PlayerControl __instance)
    {
        if (__instance.notRealPlayer) return;
        PlayerCache.AllPlayers.RemoveAll(p => p.PlayerControl.Pointer == __instance.Pointer);
        PlayerCache.PlayerPointers.Remove(__instance.Pointer);
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.Deserialize))]
    [HarmonyPostfix]
    public static void AddCachedDataOnDeserialize()
    {
        foreach (var cachedPlayer in PlayerCache.AllPlayers)
        {
            cachedPlayer.Data = cachedPlayer.PlayerControl.Data;
            cachedPlayer.PlayerId = cachedPlayer.PlayerControl.PlayerId;
        }
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.AddPlayer))]
    [HarmonyPostfix]
    public static void AddCachedDataOnAddPlayer()
    {
        foreach (var cachedPlayer in PlayerCache.AllPlayers)
        {
            cachedPlayer.Data = cachedPlayer.PlayerControl.Data;
            cachedPlayer.PlayerId = cachedPlayer.PlayerControl.PlayerId;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Deserialize))]
    [HarmonyPostfix]
    public static void SetCachedPlayerId(PlayerControl __instance)
    {
        PlayerCache.PlayerPointers[__instance.Pointer].PlayerId = __instance.PlayerId;
    }
}
