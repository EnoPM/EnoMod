using EnoMod.Kernel;

namespace EnoMod.Customs.Modules;

[EnoSingleton]
public class ReactorSabotageCountdown
{
    public CustomOption ReactorCountdown;

    [EnoHook(CustomHooks.LoadCustomOptions)]
    public Hooks.Result LoadCustomOptions()
    {
        ReactorCountdown = Singleton<CustomOption.Holder>.Instance.Settings.CreateFloatList(
            nameof(ReactorCountdown),
            Utils.Colors.Cs("#75161e", "Reactor countdown"),
            10f,
            120f,
            60f,
            5,
            null,
            string.Empty,
            "s");
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.ReactorSabotageStarting)]
    public Hooks.Result ReactorSabotageStarting(ReactorSystemType reactorSystemType, PlayerControl player, byte opCode)
    {
        if (ShipStatus.Instance.Type != ShipStatus.MapType.Pb || opCode != (byte) 128 ||
            reactorSystemType.IsActive) return Hooks.Result.ReturnTrue;
        reactorSystemType.Countdown = ReactorCountdown;
        reactorSystemType.UserConsolePairs.Clear();
        reactorSystemType.IsDirty = true;
        return Hooks.Result.ReturnFalse;
    }
}
