using System;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using EnoMod.Kernel;
using HarmonyLib;
using Reactor;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace EnoMod;

[BepInPlugin(Id, "EnoMod", VersionString)]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[EnoSingleton]
public partial class EnoModPlugin : BasePlugin
{
    private const string Id = "me.eno.enomod";
    private const string VersionString = "1.0.0";

    public static readonly Version Version = Version.Parse(VersionString);
    public static ManualLogSource Logger;
    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;

    public static EnoModPlugin Instance;

    public static TextMeshPro? Text { get; private set; }
    public static event Action<TextMeshPro>? TextUpdated;

    public static Random Rnd = new((int) DateTime.Now.Ticks);

    private Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        Instance = this;
        Logger = Log;

        Harmony.PatchAll();

        Instances.Load();
        Hooks.Load();
        Singleton<CustomOption.Holder>.Instance.Load();
        LoadVersionText();
    }

    private static void LoadVersionText()
    {
        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>) ((_, _) =>
        {
            var original = UnityEngine.Object.FindObjectOfType<VersionShower>();
            if (!original)
                return;

            var gameObject = new GameObject("EnoPluginVersion " + Guid.NewGuid());
            gameObject.transform.parent = original.transform.parent;

            var aspectPosition = gameObject.AddComponent<AspectPosition>();

            aspectPosition.Alignment = AspectPosition.EdgeAlignments.LeftTop;

            var originalAspectPosition = original.GetComponent<AspectPosition>();
            var originalPosition = originalAspectPosition.DistanceFromEdge;
            originalPosition.y = 0.15f;
            originalAspectPosition.DistanceFromEdge = originalPosition;
            originalAspectPosition.AdjustPosition();

            var position = originalPosition;
            position.x += 10.075f - 0.1f;
            position.y += 2.75f - 0.15f;
            position.z -= 1;
            aspectPosition.DistanceFromEdge = position;

            aspectPosition.AdjustPosition();

            Text = gameObject.AddComponent<TextMeshPro>();
            Text.fontSize = 2;

            Text.text = $"<size=80%>{Utils.Colors.Cs("#fcc203", "EnoMod")}</size> <size=60%>v{VersionString}</size>";
            TextUpdated?.Invoke(Text);
        }));
    }
}
