using EnoMod.Kernel;

namespace EnoMod.Customs.Modules;

public static class DebugMode
{
    [EnoHook(CustomHooks.EndGameCheck)]
    public static Hooks.Result EndGameCheck()
    {
        if (Singleton<CustomOption.Holder>.Instance.DebugMode)
        {
            return Hooks.Result.ReturnFalse;
        }

        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.GameStartManagerUpdate)]
    public static Hooks.Result GameStartManagerUpdate(GameStartManager gameStartManager)
    {
        if (Singleton<CustomOption.Holder>.Instance.DebugMode)
        {
            gameStartManager.MinPlayers = 1;
        }

        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.HudManagerUpdate)]
    public static Hooks.Result HudManagerUpdate(HudManager hudManager)
    {
        if (!hudManager.Chat.isActiveAndEnabled && (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay ||
                                                    Singleton<CustomOption.Holder>.Instance.DebugMode))
        {
            hudManager.Chat.SetVisible(true);
        }

        return Hooks.Result.Continue;
    }
}
