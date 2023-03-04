using System;
using EnoMod.Kernel;
using EnoMod.Utils;
using UnityEngine;

namespace EnoMod.Customs.Modules;

public static class RolesScenes
{
    [EnoHook(CustomHooks.IntroCutsceneShowRole)]
    public static Hooks.Result IntroCutsceneShowRole(IntroCutscene cutscene)
    {
        if (!Singleton<CustomOption.Holder>.Instance.EnableRoles) return Hooks.Result.Continue;
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(1f, new Action<float>((_) =>
        {
            if (PlayerCache.LocalPlayer == null) return;
            var role = CustomRole.GetLocalPlayerRole();
            if (role == null) return;
            cutscene.YouAreText.color = role.Color;
            cutscene.RoleText.text = role.Name;
            cutscene.RoleText.color = role.Color;
            cutscene.RoleBlurbText.text = role.Description;
            cutscene.RoleBlurbText.color = role.Color;
        })));
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.IntroCutsceneDestroying)]
    public static Hooks.Result IntroCutsceneDestroying(IntroCutscene cutscene)
    {
        SoundEffectManager.Load();
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.IntroCutsceneBeginCrewmate)]
    public static Hooks.Result IntroCutsceneBeginCrewmate(
        IntroCutscene cutscene,
        ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        SetupIntroTeam(cutscene);
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.IntroCutsceneBeginImpostor)]
    public static Hooks.Result IntroCutsceneBeginImpostor(
        IntroCutscene cutscene,
        ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        SetupIntroTeam(cutscene);
        return Hooks.Result.Continue;
    }

    private static void SetupIntroTeam(IntroCutscene cutscene)
    {
        if (PlayerCache.LocalPlayer == null) return;
        var role = CustomRole.GetLocalPlayerRole();
        if (role == null) return;
        Color color;
        string title;
        switch (role.Team)
        {
            case CustomRole.Teams.Crewmate:
                color = Colors.FromHex("#5cebed");
                title = "Crewmate";
                break;
            case CustomRole.Teams.Impostor:
                color = Colors.FromHex("#cf0000");
                title = "Impostor";
                break;
            default:
                color = Colors.FromHex("#384747");
                title = "Neutral";
                break;
        }

        cutscene.BackgroundBar.material.color = color;
        cutscene.TeamTitle.text = title;
        cutscene.TeamTitle.color = color;
    }
}
