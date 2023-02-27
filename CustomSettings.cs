using System.Collections.Generic;
using EnoMod.Modules;

namespace EnoMod;

public class CustomSettings
{
    public static CustomSettingsTab Settings;
    public static CustomSettingsTab Roles;

    public static CustomSetting Preset;

    public static CustomSetting EnableRoles;

    public static CustomSetting DebugMode;

    public static CustomSetting ReactorCountdown;

    public static CustomSetting EnableChatInGame;

    public static CustomSetting ShieldFirstKilledPlayer;
    public static CustomSetting RemoveShieldInFirstKill;
    public static CustomSetting RemoveShieldInFirstMeeting;

    public static CustomSetting EnableBetterPolus;
    public static CustomSetting BetterPolusVitals;
    public static CustomSetting BetterPolusWifi;
    public static CustomSetting BetterPolusVents;

    public static CustomSetting EnableInfosNerf;
    public static CustomSetting MaxPlayersToUseCameras;
    public static CustomSetting MaxPlayersToUseVitals;
    public static CustomSetting MaxPlayersToUseAdmin;

    private static void LoadCustomSettings()
    {
        Settings = new CustomSettingsTab("EnoSettings", "Mod settings");
        Roles = new CustomSettingsTab("EnoRolesSettings", "Roles settings");
        CustomSettingsTab.SettingsTabs.Add(Settings);
        CustomSettingsTab.SettingsTabs.Add(Roles);

        Preset = Settings.AddCustomSetting(
            CustomSetting.CreateStringList(
                nameof(Preset),
                Cs("#504885", "Current preset"),
                new List<string> { "Preset 0", "Preset 1", "Preset 2", "Preset 3", "Preset 4", "Preset 5" }));
        EnableRoles = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(EnableRoles),
                Cs("#ff1010", "Enable roles"),
                true));
        DebugMode = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(DebugMode),
                Cs("#09730b", "Debug mode"),
                false));

        ReactorCountdown = Settings.AddCustomSetting(
            CustomSetting.CreateFloatList(
                nameof(ReactorCountdown),
                Cs("#75161e", "Reactor countdown"),
                10f,
                120f,
                60f,
                5,
                null,
                string.Empty,
                "s"));

        EnableChatInGame = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(EnableChatInGame),
                Cs("#18ad1b", "Chat in game"),
                false));

        ShieldFirstKilledPlayer = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(ShieldFirstKilledPlayer),
                Cs("#0780a8", "Shield first killed player"),
                false));
        RemoveShieldInFirstKill = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(RemoveShieldInFirstKill),
                Cs("#15a0cf", "Remove shield on first kill"),
                false,
                ShieldFirstKilledPlayer));
        RemoveShieldInFirstMeeting = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(RemoveShieldInFirstMeeting),
                Cs("#15a0cf", "Remove shield on first meeting"),
                false,
                ShieldFirstKilledPlayer));

        EnableBetterPolus = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(EnableBetterPolus),
                Cs("#09730b", "Enable BetterPolus"),
                false));
        BetterPolusVitals = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(BetterPolusVitals),
                Cs("#18ad1b", "Vitals in laboratory"),
                false,
                EnableBetterPolus));
        BetterPolusWifi = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(BetterPolusWifi),
                Cs("#18ad1b", "Wifi in dropship"),
                false,
                EnableBetterPolus));
        BetterPolusVents = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(BetterPolusVents),
                Cs("#18ad1b", "Change reactor vents"),
                false,
                EnableBetterPolus));

        EnableInfosNerf = Settings.AddCustomSetting(
            CustomSetting.CreateBool(
                nameof(EnableInfosNerf),
                Cs("#4f4545", "Nerf information"),
                false));
        MaxPlayersToUseCameras = Settings.AddCustomSetting(
            CustomSetting.CreateFloatList(
                nameof(MaxPlayersToUseCameras),
                Cs("#4f4545", "Disable cameras above"),
                0f,
                15f,
                0f,
                1f,
                EnableInfosNerf,
                string.Empty,
                " players left"));
        MaxPlayersToUseVitals = Settings.AddCustomSetting(
            CustomSetting.CreateFloatList(
                nameof(MaxPlayersToUseVitals),
                Cs("#4f4545", "Disable vitals above"),
                0f,
                15f,
                0f,
                1f,
                EnableInfosNerf,
                string.Empty,
                " players left"));
        MaxPlayersToUseAdmin = Settings.AddCustomSetting(
            CustomSetting.CreateFloatList(
                nameof(MaxPlayersToUseAdmin),
                Cs("#4f4545", "Disable admin above"),
                0f,
                15f,
                0f,
                1f,
                EnableInfosNerf,
                string.Empty,
                " players left"));
    }

    public static void Load()
    {
        CustomSetting.VanillaSettings = EnoModPlugin.Instance.Config.Bind("Preset0", "VanillaOptions", string.Empty);
        LoadCustomSettings();
        foreach (var customRole in CustomRole.Roles)
        {
            customRole.CreateCustomOptions();
        }
    }

    private static string Cs(string hexColor, string s)
    {
        return CustomSetting.Cs(Helpers.HexColor(hexColor), s);
    }
}
