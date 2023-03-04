﻿namespace EnoMod.Customs;

public enum CustomHooks
{
    AdminTableOpened,
    CamerasUpdated,
    PlanetCameraUpdated,
    PlanetCameraNextUpdated,
    VitalsUpdated,
    MeetingEnded,
    ReactorSabotageStarting,
    LocalPlayerChatMessageSending,
    StringOptionIncrease,
    StringOptionDecrease,
    StringOptionEnable,
    GameOptionsMenuStart,
    GameOptionsMenuUpdate,
    EndGameCheck,
    GameStartManagerUpdate,
    MeetingEnding,
    IntroCutsceneShowRole,
    IntroCutsceneDestroying,
    IntroCutsceneBeginCrewmate,
    IntroCutsceneBeginImpostor,
    PlayerControlFixedUpdatePostfix,
    PlayerControlFixedUpdatePrefix,
    RoleManagerSelectRoles,
    RoleOptionsDataGetNumPerGame,
    GameOptionsDataGetAdjustedNumImpostorsPatch,
    GameOptionsDataValidate,
    PlayerControlRpcSyncSettings,
    PlayerJoined,
    PlayerControlCheckMurder,
    MeetingHudUpdate,
    ShipStatusBegin,
    ShipStatusAwake,
    ShipStatusFixedUpdate,
    HudManagerUpdate,
    VersionShower,
    PlayerControlHandleRpc,
    LoadCustomOptions,
    LoadCustomButtons,
}

public enum CustomRpc : uint
{
    ShareGameState = 1,
    ShareCustomOptions = 2,
    ShieldedMurderAttempt = 3,
    MurderAttempt = 4,
    UpdateRoleInfo = 5,
    JesterSabotageStart = 6,
    JesterSabotageEnd = 7,
    ShareCustomRoles = 8,
    RevivePlayer = 9,
}