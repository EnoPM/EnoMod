using System;
using EnoMod.Modules;
using HarmonyLib;
using UnityEngine;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
public class IntroCutsceneShowRolePatch
{
    public static bool Prefix(IntroCutscene __instance)
    {
        if (!CustomSettings.EnableRoles) return true;
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(1f, new Action<float>((p) =>
        {
            SetRoleTexts(__instance);
        })));
        return true;
    }

    private static void SetRoleTexts(IntroCutscene cutscene)
    {
        if (PlayerCache.LocalPlayer == null) return;
        var role = CustomRole.GetLocalPlayerRole();
        if (role == null) return;
        cutscene.YouAreText.color = role.GetColor();
        cutscene.RoleText.text = role.Name;
        cutscene.RoleText.color = role.GetColor();
        cutscene.RoleBlurbText.text = role.Description;
        cutscene.RoleBlurbText.color = role.GetColor();
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
public class IntroCutsceneOnDestroyPatch
{
    public static void Prefix(IntroCutscene __instance)
    {
        SoundEffectManager.Load();

        if (!AmongUsClient.Instance.AmHost) return;
        GameState.Instance.PlayerShielded = GameState.Instance.FirstPlayerKilledInThisGame;
        GameState.Instance.FirstPlayerKilledInThisGame = null;
        GameState.ShareChanges();
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
public class IntroCutsceneBeginCrewmatePatch
{
    public static void Postfix(
        IntroCutscene __instance,
        ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        SetupIntroTeam(__instance, ref teamToDisplay);
    }

    public static void SetupIntroTeam(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        var role = CustomRole.GetLocalPlayerRole();
        if (role == null) return;
        Color color;
        string title;
        switch (role.Team)
        {
            case CustomRole.Teams.Crewmate:
                color = Helpers.HexColor("#5cebed");
                title = "Modded crewmate";
                break;
            case CustomRole.Teams.Impostor:
                color = Helpers.HexColor("#cf0000");
                title = "Modded impostor";
                break;
            case CustomRole.Teams.Neutral:
            default:
                color = Helpers.HexColor("#384747");
                title = "Modded neutral";
                break;
        }

        __instance.BackgroundBar.material.color = color;
        __instance.TeamTitle.text = title;
        __instance.TeamTitle.color = color;
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
public class IntroCutsceneBeginImpostorPatch
{
    public static void Postfix(IntroCutscene __instance,
        ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        IntroCutsceneBeginCrewmatePatch.SetupIntroTeam(__instance, ref yourTeam);
    }
}
