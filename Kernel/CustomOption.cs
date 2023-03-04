using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using EnoMod.Customs;
using UnityEngine;

namespace EnoMod.Kernel;

public class CustomOption
{
    public enum OptionType
    {
        Boolean,
        StringList,
        FloatList,
    }

    private static readonly Regex _stringToFloatRegex = new("[^0-9 -]");

    public static ConfigEntry<string>? VanillaSettings;

    private static int _preset;

    public static implicit operator string(CustomOption option)
    {
        switch (option.Type)
        {
            case OptionType.Boolean:
                return option ? "yes" : "no";
            case OptionType.StringList:
                return option;
            case OptionType.FloatList:
                return option.StringSelections[option.SelectionIndex];
            default:
                throw new EnoModException("Error: CustomSetting type out of enum SettingType range");
        }
    }

    public static implicit operator float(CustomOption option)
    {
        switch (option.Type)
        {
            case OptionType.Boolean:
                return option ? 1f : 0f;
            case OptionType.StringList:
                return (float) Convert.ToDouble(
                    _stringToFloatRegex.Replace(option, string.Empty),
                    CultureInfo.InvariantCulture.NumberFormat);
            case OptionType.FloatList:
                if (option.FloatSelections == null)
                    throw new EnoModException($"Error: FloatSelections is null in customSetting {option.Key}");
                return option.FloatSelections[option.SelectionIndex];
            default:
                throw new EnoModException("Error: CustomSetting type out of enum SettingType range");
        }
    }

    public static implicit operator int(CustomOption option)
    {
        return option.SelectionIndex;
    }

    public static implicit operator bool(CustomOption option)
    {
        switch (option.Type)
        {
            case OptionType.Boolean:
                return option.SelectionIndex == 1;
            case OptionType.StringList:
                return ((string) option).Length > 0;
            case OptionType.FloatList:
                return (float) option > 0f;
            default:
                throw new EnoModException("Error: CustomSetting type out of enum SettingType range");
        }
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
    public readonly CustomOption? Parent;
    public readonly bool IsHeader;
    public readonly OptionType Type;

    public ConfigEntry<int>? Entry;

    public CustomOption(
        OptionType type,
        string key,
        string name,
        List<string> stringSelections,
        List<float>? floatSelections,
        int defaultIndex = 0,
        bool isHeader = false,
        CustomOption? parent = null)
    {
        Key = key;
        Name = parent == null ? name : $"- {name}";
        StringSelections = stringSelections;
        FloatSelections = floatSelections;
        SelectionIndex = defaultIndex;
        Parent = parent;
        IsHeader = isHeader;
        Type = type;
        if (Key == nameof(Singleton<CustomOption.Holder>.Instance.Preset)) return;
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
            if (Key == nameof(Singleton<CustomOption.Holder>.Instance.Preset) && SelectionIndex != _preset)
            {
                Tab.SwitchPreset(SelectionIndex);
                ShareOptionChange();
            }
            else if (Entry != null)
            {
                Entry.Value = SelectionIndex;
                ShareOptionChange();
            }
        }
        else if (Key == nameof(Singleton<CustomOption.Holder>.Instance.Preset) && AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer)
        {
            // Share the preset switch for random maps, even if the menu isnt open!
            Tab.SwitchPreset(SelectionIndex);
            Tab.ShareCustomOptions(); // Share all selections
        }
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

    public class Tab
    {
        public static List<Tab> Tabs = new();
        public static ConfigEntry<string>? VanillaSettings;

        private static int _preset;

        public CustomOption CreateBool(
            string key,
            string name,
            bool defaultValue,
            CustomOption? parent = null)
        {
            var customOption = new CustomOption(
                CustomOption.OptionType.Boolean,
                key,
                name,
                new List<string> { "off", "on" },
                null,
                defaultValue ? 1 : 0,
                parent == null,
                parent);
            Add(customOption);
            return customOption;
        }

        public CustomOption CreateFloatList(
            string key,
            string name,
            float minValue,
            float maxValue,
            float defaultValue,
            float step,
            CustomOption? parent = null,
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

            var customOption = new CustomOption(
                CustomOption.OptionType.FloatList,
                key,
                name,
                selections,
                floatSelections,
                floatSelections.Contains(defaultValue) ? floatSelections.IndexOf(defaultValue) : 0,
                parent == null,
                parent);
            Add(customOption);
            return customOption;
        }

        public CustomOption CreateStringList(
            string key,
            string name,
            List<string> selections,
            string? defaultValue = null,
            CustomOption? parent = null)
        {
            var selection = defaultValue == null ? 0 : selections.IndexOf(defaultValue);
            if (selection < 0) selection = 0;
            var customOption = new CustomOption(
                CustomOption.OptionType.StringList,
                key,
                name,
                selections,
                null,
                selection,
                parent == null,
                parent
            );
            Add(customOption);
            return customOption;
        }

        public static List<CustomOption> Options()
        {
            return Tabs.SelectMany(tab => tab.Settings).ToList();
        }

        public static void ShareCustomOptions()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var options = new List<Rpc.CustomOptionInfo>();
            foreach (var setting in Tabs.SelectMany(settingsTab => settingsTab.Settings))
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
            foreach (var setting in Tabs.SelectMany(settingsTab => settingsTab.Settings))
            {
                if (setting.Key == nameof(Singleton<CustomOption.Holder>.Instance.Preset)) continue;
                setting.Entry =
                    EnoModPlugin.Instance.Config.Bind($"Preset{_preset}", $"{setting.Key}", setting.SelectionIndex);
                setting.SelectionIndex = Mathf.Clamp(setting.Entry.Value, 0, setting.StringSelections.Count - 1);
                if (setting.OptionBehaviour == null ||
                    setting.OptionBehaviour is not StringOption stringOption) continue;
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
        public readonly List<CustomOption> Settings = new();

        public Tab(string key, string title, string iconPath = "EnoMod.Resources.Icons.ModStamp.png")
        {
            Key = key;
            Title = title;
            IconPath = iconPath;
        }

        public CustomOption Add(CustomOption option)
        {
            Settings.Add(option);
            return option;
        }
    }

    [EnoSingleton]
    public class Holder
    {
        public Tab Settings;
        public Tab Roles;

        public CustomOption Preset;
        public CustomOption EnableRoles;
        public CustomOption DebugMode;

        public void Load()
        {
            Settings = new Tab("EnoSettings", "Mod settings");
            Tab.Tabs.Add(Settings);
            Roles = new Tab("EnoRolesSettings", "Roles settings");
            Tab.Tabs.Add(Roles);

            Preset = Settings.CreateStringList(
                nameof(Preset),
                Utils.Colors.Cs("#504885", "Current preset"),
                new List<string> { "Preset 0", "Preset 1", "Preset 2", "Preset 3", "Preset 4", "Preset 5" });
            EnableRoles = Settings.CreateBool(
                nameof(EnableRoles),
                Utils.Colors.Cs("#ff1010", "Enable roles"),
                true);
            DebugMode = Settings.CreateBool(
                nameof(DebugMode),
                Utils.Colors.Cs("#09730b", "Debug mode"),
                false);

            LoadHooks();
        }

        private static void LoadHooks()
        {
            Hooks.Trigger(CustomHooks.LoadCustomOptions);
        }
    }
}
