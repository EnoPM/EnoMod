using System;
using System.Linq;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using EnoMod.Modules;
using EnoMod.Roles;
using HarmonyLib;
using Reactor;
using Reactor.Networking;
using Reactor.Networking.Attributes;

namespace EnoMod;

[BepInPlugin(Id, "EnoMod", VersionString)]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[ReactorModFlags(ModFlags.RequireOnAllClients)]
public partial class EnoModPlugin : BasePlugin
{
    private const string Id = "me.eno.enomod";
    private const string VersionString = "0.0.31";

    public static readonly Version Version = Version.Parse(VersionString);
    public static ManualLogSource Logger;
    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;

    public static EnoModPlugin Instance;

    public static Random Rnd = new((int) DateTime.Now.Ticks);

    private Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        Component = this.AddComponent<DebuggerComponent>();
        Instance = this;
        Logger = Log;

        Harmony.PatchAll();

        Reference.Load();
        CustomSettings.Load();
        Hooks.Load();
    }
}
