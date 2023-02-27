using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using UnityEngine;

namespace EnoMod.Modules;

public class CustomSetting
{
    public enum SettingType
    {
        Boolean,
        StringList,
        FloatList,
    }

    private static readonly Regex _stringToFloatRegex = new("[^0-9 -]");

    public static ConfigEntry<string>? VanillaSettings;

    private static int _preset;

    public static implicit operator string(CustomSetting setting)
    {
        switch (setting.Type)
        {
            case SettingType.Boolean:
                return setting ? "yes" : "no";
            case SettingType.StringList:
                return setting;
            case SettingType.FloatList:
                return setting.StringSelections[setting.SelectionIndex];
            default:
                throw new EnoModException("Error: CustomSetting type out of enum SettingType range");
        }
    }

    public static implicit operator float(CustomSetting setting)
    {
        switch (setting.Type)
        {
            case SettingType.Boolean:
                return setting ? 1f : 0f;
            case SettingType.StringList:
                return (float) Convert.ToDouble(
                    _stringToFloatRegex.Replace(setting, string.Empty),
                    CultureInfo.InvariantCulture.NumberFormat);
            case SettingType.FloatList:
                if (setting.FloatSelections == null)
                    throw new EnoModException($"Error: FloatSelections is null in customSetting {setting.Key}");
                return setting.FloatSelections[setting.SelectionIndex];
            default:
                throw new EnoModException("Error: CustomSetting type out of enum SettingType range");
        }
    }

    public static implicit operator int(CustomSetting setting)
    {
        return setting.SelectionIndex;
    }

    public static implicit operator bool(CustomSetting setting)
    {
        switch (setting.Type)
        {
            case SettingType.Boolean:
                return setting.SelectionIndex == 1;
            case SettingType.StringList:
                return ((string) setting).Length > 0;
            case SettingType.FloatList:
                return (float) setting > 0f;
            default:
                throw new EnoModException("Error: CustomSetting type out of enum SettingType range");
        }
    }

    public static CustomSetting CreateBool(
        string key,
        string name,
        bool defaultValue,
        CustomSetting? parent = null)
    {
        return new CustomSetting(
            SettingType.Boolean,
            key,
            name,
            new List<string> { "no", "yes" },
            null,
            defaultValue ? 1 : 0,
            parent == null,
            parent);
    }

    public static CustomSetting CreateFloatList(
        string key,
        string name,
        float minValue,
        float maxValue,
        float defaultValue,
        float step,
        CustomSetting? parent = null,
        string prefix = "",
        string suffix = "")
    {
        var selections = new List<string>();
        var floatSelections = new List<float>();
        for (var i = minValue; i <= maxValue; i += step)
        {
            floatSelections.Add(i);
            selections.Add($"{prefix}{i}{suffix}");
        }

        return new CustomSetting(
            SettingType.FloatList,
            key,
            name,
            selections,
            floatSelections,
            floatSelections.Contains(defaultValue) ? floatSelections.IndexOf(defaultValue) : 0,
            parent == null,
            parent);
    }

    public static CustomSetting CreateStringList(
        string key,
        string name,
        List<string> selections,
        string? defaultValue = null,
        CustomSetting? parent = null)
    {
        var selection = defaultValue == null ? 0 : selections.IndexOf(defaultValue);
        if (selection < 0) selection = 0;
        return new CustomSetting(
            SettingType.StringList,
            key,
            name,
            selections,
            null,
            selection,
            parent == null,
            parent
        );
    }

    public static string Cs(Color c, string s)
    {
        return $"<color=#{ToByte(c.r):X2}{ToByte(c.g):X2}{ToByte(c.b):X2}{ToByte(c.a):X2}>{s}</color>";
    }

    private static byte ToByte(float f)
    {
        f = Mathf.Clamp01(f);
        return (byte) (f * 255);
    }

    public readonly string Key;
    public readonly string Name;
    public readonly List<string> StringSelections = new();
    public readonly List<float>? FloatSelections;
    public int SelectionIndex;
    public OptionBehaviour? OptionBehaviour;
    public readonly CustomSetting? Parent;
    public readonly bool IsHeader;
    public readonly SettingType Type;

    public ConfigEntry<int>? Entry;

    private CustomSetting(
        SettingType type,
        string key,
        string name,
        List<string> stringSelections,
        List<float>? floatSelections,
        int defaultIndex = 0,
        bool isHeader = false,
        CustomSetting? parent = null)
    {
        Key = key;
        Name = parent == null ? name : $"- {name}";
        StringSelections = stringSelections;
        FloatSelections = floatSelections;
        SelectionIndex = defaultIndex;
        Parent = parent;
        IsHeader = isHeader;
        Type = type;
        if (Key == nameof(CustomSettings.Preset)) return;
        Entry = EnoModPlugin.Instance.Config.Bind($"Preset{_preset}", Key, SelectionIndex);
        SelectionIndex = Mathf.Clamp(Entry.Value, 0, StringSelections.Count - 1);
    }

    public void UpdateSelection(int selection)
    {
        SelectionIndex = Mathf.Clamp(
            (selection + StringSelections.Count) % StringSelections.Count,
            0,
            StringSelections.Count - 1);
        if (StringSelections.Count > 0 && OptionBehaviour != null && OptionBehaviour is StringOption stringOption)
        {
            stringOption.oldValue = stringOption.Value = SelectionIndex;
            stringOption.ValueText.text = StringSelections[SelectionIndex].ToString();

            if (!AmongUsClient.Instance.AmHost || !PlayerCache.LocalPlayer?.PlayerControl) return;
            if (Key == nameof(CustomSettings.Preset) && SelectionIndex != _preset)
            {
                CustomSettingsTab.SwitchPreset(SelectionIndex);
                ShareOptionChange();
            }
            else if (Entry != null)
            {
                Entry.Value = SelectionIndex;
                ShareOptionChange();
            }
        }
        else if (Key == nameof(CustomSettings.Preset) && AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer)
        {
            // Share the preset switch for random maps, even if the menu isnt open!
            CustomSettingsTab.SwitchPreset(SelectionIndex);
            CustomSettingsTab.ShareCustomOptions(); // Share all selections
        }
    }

    public CustomSettingsTab GetTab()
    {
        foreach (var customSettingsTab in from customSettingsTab in CustomSettingsTab.SettingsTabs let setting = customSettingsTab.Settings.Find(cs => cs.Key == Key) where setting != null select customSettingsTab)
        {
            return customSettingsTab;
        }

        throw new EnoModException($"Setting {Key} is not inside a tab");
    }

    private void ShareOptionChange()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Rpc.ShareCustomOptions(PlayerControl.LocalPlayer, Rpc.Serialize(new List<Rpc.CustomOptionInfo>
        {
            new()
            {
                Key = Key,
                Selection = SelectionIndex,
            },
        }));
    }
}

public class CustomSettingsTab
{
    public static List<CustomSettingsTab> SettingsTabs = new();
    public static ConfigEntry<string>? VanillaSettings;

    private static int _preset;

    public static List<CustomSetting> Options()
    {
        return SettingsTabs.SelectMany(tab => tab.Settings).ToList();
    }

    public static void ShareCustomOptions()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var options = new List<Rpc.CustomOptionInfo>();
        foreach (var setting in SettingsTabs.SelectMany(settingsTab => settingsTab.Settings))
        {
            options.Add(new Rpc.CustomOptionInfo { Key = setting.Key, Selection = setting.SelectionIndex });
        }

        Rpc.ShareCustomOptions(
            PlayerControl.LocalPlayer,
            Rpc.Serialize(options)
        );
    }

    public static void SwitchPreset(int newPreset)
    {
        SaveVanillaOptions();
        _preset = newPreset;
        VanillaSettings = EnoModPlugin.Instance.Config.Bind($"Preset{_preset}", "GameOptions", string.Empty);
        LoadVanillaOptions();
        foreach (var setting in SettingsTabs.SelectMany(settingsTab => settingsTab.Settings))
        {
            if (setting.Key == nameof(CustomSettings.Preset)) continue;
            setting.Entry =
                EnoModPlugin.Instance.Config.Bind($"Preset{_preset}", $"{setting.Key}", setting.SelectionIndex);
            setting.SelectionIndex = Mathf.Clamp(setting.Entry.Value, 0, setting.StringSelections.Count - 1);
            if (setting.OptionBehaviour == null || setting.OptionBehaviour is not StringOption stringOption) continue;
            stringOption.oldValue = stringOption.Value = setting.SelectionIndex;
            stringOption.ValueText.text = setting.StringSelections[setting.SelectionIndex];
        }
    }

    private static void LoadVanillaOptions()
    {
        if (VanillaSettings != null)
        {
            var optionsString = VanillaSettings.Value;
            if (optionsString == string.Empty) return;
            GameOptionsManager.Instance.GameHostOptions =
                GameOptionsManager.Instance.gameOptionsFactory.FromBytes(Convert.FromBase64String(optionsString));
        }

        GameOptionsManager.Instance.CurrentGameOptions = GameOptionsManager.Instance.GameHostOptions;
        GameManager.Instance.LogicOptions.SetGameOptions(GameOptionsManager.Instance.CurrentGameOptions);
        GameManager.Instance.LogicOptions.SyncOptions();
    }

    private static void SaveVanillaOptions()
    {
        if (VanillaSettings != null)
        {
            VanillaSettings.Value =
                Convert.ToBase64String(
                    GameOptionsManager.Instance.gameOptionsFactory.ToBytes(GameManager.Instance.LogicOptions
                        .currentGameOptions));
        }
    }
    
    public readonly string Key;
    public readonly string Title;
    public readonly string IconPath;
    public readonly List<CustomSetting> Settings = new();

    public CustomSettingsTab(string key, string title, string iconPath = "EnoMod.Resources.Icons.ModStamp.png")
    {
        Key = key;
        Title = title;
        IconPath = iconPath;
    }

    public CustomSetting AddCustomSetting(CustomSetting setting)
    {
        Settings.Add(setting);
        return setting;
    }
}
