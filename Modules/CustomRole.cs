using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;

namespace EnoMod.Modules;

public abstract class CustomRole : RoleData
{
    public enum Teams
    {
        Crewmate,
        Impostor,
        Neutral,
    }

    public enum HookResult
    {
        Continue,
        Stop,
        ReturnTrue,
        ReturnFalse,
    }

    public static readonly List<CustomRole> Roles = new();

    public static CustomRole? GetById(int id)
    {
        return Roles.Find(role => role.Id == id);
    }

    public static CustomRole? GetByName(string name)
    {
        return Roles.Find(role => role.Name == name);
    }

    public static CustomRole? GetLocalPlayerRole()
    {
        return GetByPlayer(PlayerControl.LocalPlayer);
    }

    public static CustomRole? GetByPlayer(PlayerControl? player)
    {
        return player == null ? null : Roles.Find(r => r.HasPlayer(player.PlayerId));
    }

    public static void ClearPlayers()
    {
        foreach (var role in Roles)
        {
            role.Players.Clear();
            Rpc.UpdateRoleInfo(PlayerControl.LocalPlayer, role.Serialize());
        }
    }

    public virtual HookResult HookOnAdminTableOpened(MapCountOverlay adminTable)
    {
        return HookResult.Continue;
    }

    public virtual HookResult HookOnCameraUpdated(SurveillanceMinigame cameras)
    {
        return HookResult.Continue;
    }

    public virtual HookResult HookOnPlanetCameraUpdated(PlanetSurveillanceMinigame cameras)
    {
        return HookResult.Continue;
    }

    public virtual HookResult HookOnVitalsUpdated(VitalsMinigame vitals)
    {
        return HookResult.Continue;
    }

    public virtual HookResult HookOnMeetingEnd(ExileController exileController, GameData.PlayerInfo? exiled)
    {
        return HookResult.Continue;
    }

    public virtual HookResult HookOnPlanetCameraNextUpdated(PlanetSurveillanceMinigame minigame, int direction)
    {
        return HookResult.Continue;
    }

    public virtual bool TriggerEndGame()
    {
        return false;
    }

    public bool CanBe(PlayerControl player)
    {
        if (Team == Teams.Impostor)
        {
            return player.Data.RoleType == RoleTypes.Impostor;
        }

        return player.Data.RoleType == RoleTypes.Crewmate;
    }

    public void UpdateFromData(RoleData data)
    {
        if (data.Id != Id) return;
        Team = data.Team;
        Name = data.Name;
        Description = data.Description;
        Players = data.Players;
        HexColor = data.HexColor;
    }

    public Color GetColor()
    {
        return Helpers.HexColor(HexColor);
    }

    public CustomRole()
    {
        Players = new List<RolePlayer>();
        Roles.Add(this);
    }

    public CustomSetting? NumberCustomOption;
    public CustomSetting? PercentageCustomOption;

    public virtual void CreateCustomOptions()
    {
        if (PercentageCustomOption != null) return;
        NumberCustomOption = CustomSettings.Roles.AddCustomSetting(CustomSetting.CreateFloatList(
            $"MaxAmount{Name}",
            CustomSetting.Cs(GetColor(), $"{Name}"),
            0f,
            15f,
            0f,
            1f));
        PercentageCustomOption = CustomSettings.Roles.AddCustomSetting(CustomSetting.CreateFloatList(
            $"{Name}SpawnRate",
            CustomSetting.Cs(GetColor(), $"Spawn rate"),
            0f,
            100f,
            50f,
            10f,
            NumberCustomOption,
            string.Empty,
            "%"));
    }

    public virtual void CreateCustomButtons(HudManager hudManager)
    {
    }

    public void AddPlayer(byte playerId)
    {
        if (HasPlayer(playerId)) return;
        Players.Add(new RolePlayer { PlayerId = playerId });
        if (PlayerCache.LocalPlayer == null) return;
        Rpc.UpdateRoleInfo(PlayerCache.LocalPlayer, Serialize());
    }

    public bool HasPlayer(byte playerId)
    {
        return Players.Any(p => p.PlayerId == playerId);
    }

    public RoleData.RolePlayer GetPlayer(byte playerId)
    {
        if (!HasPlayer(playerId)) throw new EnoModException($"Role {Name} dont contains player {playerId}");
        return Players.Find(p => p.PlayerId == playerId)!;
    }
}

public class RoleData
{
    public int Id { get; set; }
    public CustomRole.Teams Team { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<RolePlayer> Players { get; set; }
    public string HexColor { get; set; }

    public bool CanTarget { get; set; }

    public string Serialize()
    {
        return Rpc.Serialize(this);
    }

    public static RoleData Deserialize(string data)
    {
        return Rpc.Deserialize<RoleData>(data);
    }

    public class RolePlayer
    {
        public byte PlayerId { get; set; }
        public byte? TargetId { get; set; }
    }
}
