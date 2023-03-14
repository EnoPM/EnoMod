using AmongUs.GameOptions;
using EnoMod.Customs;
using HarmonyLib;
using Hazel;

namespace EnoMod.Kernel;

public static class CustomHookPatches
{
    private static class H
    {
        public static bool Hook(bool defaultReturn, CustomHooks hookId, params object[] args)
        {
            var result = Hooks.Trigger(hookId, args);
            if (result == Hooks.Result.ReturnTrue)
            {
                return true;
            }

            return result != Hooks.Result.ReturnFalse && defaultReturn;
        }
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    public static class ExileControllerBeginPatch
    {
        public static void Postfix(ExileController __instance, GameData.PlayerInfo? exiled)
        {
            H.Hook(
                false,
                CustomHooks.MeetingEnded,
                __instance,
                exiled);
        }
    }

    [HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.RepairDamage))]
    public static class ReactorSystemTypeRepairDamagePatch
    {
        public static bool Prefix(ReactorSystemType __instance, PlayerControl player, byte opCode)
        {
            return H.Hook(
                false,
                CustomHooks.ReactorSabotageStarting,
                __instance, player, opCode);
        }
    }

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
    public static class VitalsMinigameUpdatePatch
    {
        public static bool Prefix(VitalsMinigame __instance)
        {
            return H.Hook(
                true,
                CustomHooks.VitalsUpdated,
                __instance);
        }
    }

    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Update))]
    public static class PlanetSurveillanceMinigameUpdatePatch
    {
        public static bool Prefix(PlanetSurveillanceMinigame __instance)
        {
            return H.Hook(
                true,
                CustomHooks.PlanetCameraUpdated,
                __instance);
        }
    }

    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.NextCamera))]
    public static class PlanetSurveillanceMinigameNextCameraPatch
    {
        public static bool Prefix(PlanetSurveillanceMinigame __instance, int direction)
        {
            return H.Hook(
                true,
                CustomHooks.PlanetCameraNextUpdated,
                __instance,
                direction);
        }
    }

    [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Update))]
    public static class SurveillanceMinigameUpdatePatch
    {
        public static bool Prefix(SurveillanceMinigame __instance)
        {
            return H.Hook(
                true,
                CustomHooks.CamerasUpdated,
                __instance);
        }
    }

    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
    public static class MapCountOverlayUpdatePatch
    {
        public static bool Prefix(MapCountOverlay __instance)
        {
            return H.Hook(
                false,
                CustomHooks.AdminTableOpened,
                __instance);
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    public static class ChatControllerSendChatPatch
    {
        public static bool Prefix(ChatController __instance)
        {
            return H.Hook(
                true,
                CustomHooks.LocalPlayerChatMessageSending,
                __instance);
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    public static class StringOptionIncreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            return H.Hook(
                true,
                CustomHooks.StringOptionIncrease,
                __instance);
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    public static class StringOptionDecreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            return H.Hook(
                true,
                CustomHooks.StringOptionDecrease,
                __instance);
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
    public static class StringOptionOnEnablePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            return H.Hook(
                true,
                CustomHooks.StringOptionEnable,
                __instance);
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
    public static class GameOptionsMenuStartPatch
    {
        public static void Postfix(GameOptionsMenu __instance)
        {
            H.Hook(
                true,
                CustomHooks.GameOptionsMenuStart,
                __instance);
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    public class GameOptionsMenuUpdatePatch
    {
        public static void Postfix(GameOptionsMenu __instance)
        {
            H.Hook(
                true,
                CustomHooks.GameOptionsMenuUpdate,
                __instance);
        }
    }

    [HarmonyPatch]
    public static class LogicGameFlowCheckEndCriteriaPatch
    {
        [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlow.CheckEndCriteria))]
        public static bool Prefix()
        {
            return H.Hook(
                false,
                CustomHooks.EndGameCheck);
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public static class GameStartManagerUpdatePatch
    {
        public static void Prefix(GameStartManager __instance)
        {
            H.Hook(
                true,
                CustomHooks.GameStartManagerUpdate,
                __instance);
        }
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    public static class BaseExileControllerPatch
    {
        public static void Postfix(ExileController __instance)
        {
            H.Hook(
                true,
                CustomHooks.MeetingEnding,
                __instance);
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
    public static class IntroCutsceneShowRolePatch
    {
        public static bool Prefix(IntroCutscene __instance)
        {
            return H.Hook(
                true,
                CustomHooks.IntroCutsceneShowRole,
                __instance);
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
    public static class IntroCutsceneOnDestroyPatch
    {
        public static void Prefix(IntroCutscene __instance)
        {
            H.Hook(
                true,
                CustomHooks.IntroCutsceneDestroying,
                __instance);
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    public static class IntroCutsceneBeginCrewmatePatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            H.Hook(
                true,
                CustomHooks.IntroCutsceneBeginCrewmate,
                __instance);
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    public static class IntroCutsceneBeginImpostorPatch
    {
        public static void Postfix(IntroCutscene __instance,
            ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            H.Hook(
                true,
                CustomHooks.IntroCutsceneBeginImpostor,
                __instance, yourTeam);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class PlayerControlFixedUpdatePatch
    {
        public static void Prefix(PlayerControl __instance)
        {
            H.Hook(
                true,
                CustomHooks.PlayerControlFixedUpdatePrefix,
                __instance);
        }

        public static void Postfix(PlayerControl __instance)
        {
            H.Hook(
                true,
                CustomHooks.PlayerControlFixedUpdatePostfix,
                __instance);
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    public static class RoleManagerSelectRolesPatch
    {
        public static void Postfix()
        {
            H.Hook(
                true,
                CustomHooks.RoleManagerSelectRoles);
        }
    }

    [HarmonyPatch(typeof(RoleOptionsData), nameof(RoleOptionsData.GetNumPerGame))]
    public static class RoleOptionsDataGetNumPerGamePatch
    {
        public static void Postfix(ref int __result)
        {
            H.Hook(
                true,
                CustomHooks.RoleOptionsDataGetNumPerGame,
                __result);
        }
    }

    [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
    public static class GameOptionsDataGetAdjustedNumImpostorsPatch
    {
        public static void Postfix(ref int __result)
        {
            H.Hook(
                true,
                CustomHooks.GameOptionsDataGetAdjustedNumImpostorsPatch,
                __result);
        }
    }

    [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Validate))]
    public static class GameOptionsDataValidatePatch
    {
        public static void Postfix(GameOptionsData __instance)
        {
            H.Hook(
                true,
                CustomHooks.GameOptionsDataValidate,
                __instance);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public static class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            H.Hook(
                true,
                CustomHooks.PlayerControlRpcSyncSettings);
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoSpawnPlayer))]
    public static class AmongUsClientOnPlayerJoinedPatch
    {
        public static void Postfix()
        {
            H.Hook(
                true,
                CustomHooks.PlayerJoined);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    public static class PlayerControlCheckMurder
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            return H.Hook(
                true,
                CustomHooks.PlayerControlCheckMurder,
                __instance,
                target);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    public static class MeetingHudUpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            H.Hook(
                true,
                CustomHooks.MeetingHudUpdate,
                __instance);
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    public static class ShipStatusBeginPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch]
        public static void Prefix(ShipStatus __instance)
        {
            H.Hook(
                true,
                CustomHooks.ShipStatusBegin,
                __instance);
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
    public static class ShipStatusAwakePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch]
        public static void Prefix(ShipStatus __instance)
        {
            H.Hook(
                true,
                CustomHooks.ShipStatusAwake,
                __instance);
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    public static class ShipStatusFixedUpdatePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch]
        public static void Prefix(ShipStatus __instance)
        {
            H.Hook(
                true,
                CustomHooks.ShipStatusFixedUpdate,
                __instance);
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdatePatch
    {
        public static void Postfix(HudManager __instance)
        {
            H.Hook(
                true,
                CustomHooks.HudManagerUpdate,
                __instance);
        }
    }

    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    public static class VersionShowerPatch
    {
        public static void Postfix(VersionShower __instance)
        {
            System.Console.Write("VersionShower.Start patch");
            H.Hook(
                true,
                CustomHooks.VersionShower,
                __instance);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    public static class PlayerControlHandleRpcPatch
    {
        public static void Postfix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            H.Hook(
                true,
                CustomHooks.PlayerControlHandleRpc,
                callId, reader);
        }
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    public static class ExileControllerWrapUpPatch
    {
        public static void Postfix(ExileController __instance)
        {
            H.Hook(
                true,
                CustomHooks.ExileControllerWrapUp,
                __instance);
        }
    }

    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Select))]
    public static class PlayerVoteAreaSelectPatch
    {
        public static bool Prefix(MeetingHud __instance)
        {
            return H.Hook(
                true,
                CustomHooks.PlayerVoteAreaSelect,
                __instance);
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public static class GameStartManagerStartPatch
    {
        public static void Postfix(GameStartManager __instance)
        {
            System.Console.WriteLine($"[{PlayerCache.LocalPlayer?.Data.PlayerName}] GameStartManagerStartPatch");
            EndGameState.Reset();
            CustomRole.ClearPlayers(false);
        }
    }

    [HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
    public static class ConsoleCanUsePatch
    {
        public static bool Prefix(
            ref float __result,
            Console __instance,
            [HarmonyArgument(0)] GameData.PlayerInfo pc,
            [HarmonyArgument(1)] out bool canUse,
            [HarmonyArgument(2)] out bool couldUse)
        {
            canUse = couldUse = false;
            if (__instance.AllowImpostor) return true;
            if (CustomRole.GetLocalPlayerRole() is { HasTasks: false })
            {
                return __instance.AllowImpostor;
            }

            __result = float.MaxValue;
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerPurchasesData), nameof(PlayerPurchasesData.GetPurchase))]
    public static class PlayerPurchasesDataGetPurchasePatch
    {
        [HarmonyReversePatch]
        public static void Postfix(out bool __result)
        {
            __result = true;
        }
    }
}
