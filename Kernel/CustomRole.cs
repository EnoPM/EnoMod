using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using EnoMod.Customs.Modules;
using EnoMod.Utils;
using UnityEngine;

namespace EnoMod.Kernel;

public abstract class CustomRole : RoleData
{
    public enum Teams
    {
        Crewmate,
        Impostor,
        Neutral,
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
            RolesAssignment.UpdateRoleInfo(PlayerControl.LocalPlayer, role.Serialize());
        }
    }

    public Color Color
    {
        get
        {
            return Colors.FromHex(HexColor);
        }
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

    public CustomRole()
    {
        Players = new List<RolePlayer>();
        Roles.Add(this);
    }

    public CustomOption? NumberCustomOption;
    public CustomOption? PercentageCustomOption;

    protected void CreateCustomOptions()
    {
        if (PercentageCustomOption != null) return;
        NumberCustomOption = Singleton<CustomOption.Holder>.Instance.Roles.CreateFloatList(
            $"MaxAmount{Name}",
            CustomOption.Cs(this.Color, $"{Name}"),
            0f,
            15f,
            0f,
            1f);
        PercentageCustomOption = Singleton<CustomOption.Holder>.Instance.Roles.CreateFloatList(
            $"{Name}SpawnRate",
            CustomOption.Cs(this.Color, $"Spawn rate"),
            0f,
            100f,
            50f,
            10f,
            NumberCustomOption,
            string.Empty,
            "%");
    }

    public void AddPlayer(byte playerId)
    {
        if (HasPlayer(playerId)) return;
        var player = PlayerCache.GetPlayerById(playerId);
        if (player == null) return;
        var role = GetByPlayer(player);
        if (role != null)
        {
            role.RemovePlayer(playerId);
        }
        Players.Add(new RolePlayer { PlayerId = playerId });
        if (PlayerCache.LocalPlayer == null) return;
        RolesAssignment.UpdateRoleInfo(PlayerCache.LocalPlayer, Serialize());
    }

    public void RemovePlayer(byte playerId)
    {
        if (PlayerCache.LocalPlayer == null) return;
        for (var i = 0; i < Players.Count; i++)
        {
            var rp = Players[i];
            if (rp.PlayerId == playerId)
            {
                Players.RemoveAt(i);
                RolesAssignment.UpdateRoleInfo(PlayerCache.LocalPlayer, Serialize());
                return;
            }
        }
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
        return Serializer.Serialize(this);
    }

    public static RoleData Deserialize(string data)
    {
        return Serializer.Deserialize<RoleData>(data);
    }

    public class RolePlayer
    {
        public byte PlayerId { get; set; }
        public byte? TargetId { get; set; }
    }
}
