using System;
using System.Collections.Generic;
using System.Linq;
using EnoMod.Modules;
using HarmonyLib;
using UnityEngine;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
public class StringOptionIncreasePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = CustomSettingsTab.Options().FirstOrDefault(option => option.OptionBehaviour == __instance);
        if (option == null) return true;
        option.UpdateSelection(option.SelectionIndex + 1);
        return false;
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
public class StringOptionEnablePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = CustomSettingsTab.Options().FirstOrDefault(option => option?.OptionBehaviour == __instance);
        if (option == null) return true;
        __instance.OnValueChanged = new Action<OptionBehaviour>(_ => { });
        __instance.TitleText.text = option.Name;
        __instance.Value = __instance.oldValue = option.SelectionIndex;
        __instance.ValueText.text = option.StringSelections[option.SelectionIndex];

        return false;
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
public class StringOptionDecreasePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = CustomSettingsTab.Options().FirstOrDefault(option => option?.OptionBehaviour == __instance);
        if (option == null) return true;
        option.UpdateSelection(option.SelectionIndex - 1);
        return false;
    }
}

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
public class GameOptionsMenuStartPatch
{
    public static void Postfix(GameOptionsMenu __instance)
    {
        CreateCustomTabs(__instance);
    }

    private static void CreateCustomTabs(GameOptionsMenu gameOptionsMenu)
    {
        var tabKeys = CustomSettingsTab.SettingsTabs.ToDictionary(
            customSetting => customSetting.Key,
            customSetting => customSetting.Title);
        var isReturn = SetNames(tabKeys);
        if (isReturn) return;

        var template = UnityEngine.Object.FindObjectsOfType<StringOption>().FirstOrDefault();
        if (template == null) return;

        var gameSettings = GameObject.Find("Game Settings");
        var gameSettingMenu = UnityEngine.Object.FindObjectsOfType<GameSettingMenu>().FirstOrDefault();
        if (gameSettingMenu == null) return;

        var customSettings = new Dictionary<string, GameObject>();
        var customMenus = new Dictionary<string, GameOptionsMenu>();
        for (var index = 0; index < CustomSettingsTab.SettingsTabs.Count; index++)
        {
            var settingsTabInfo = CustomSettingsTab.SettingsTabs[index];
            GameObject setting;
            if (index == 0)
            {
                setting = UnityEngine.Object.Instantiate(gameSettings, gameSettings.transform.parent);
            }
            else
            {
                var previousInfo = CustomSettingsTab.SettingsTabs[index - 1];
                var previousSetting = customSettings[previousInfo.Key];
                setting = UnityEngine.Object.Instantiate(gameSettings, previousSetting.transform.parent);
            }

            customMenus[settingsTabInfo.Key] = GetMenu(setting, settingsTabInfo.Key);
            customSettings[settingsTabInfo.Key] = setting;
        }

        var roleTab = GameObject.Find("RoleTab");
        var gameTab = GameObject.Find("GameTab");

        var customTabs = new Dictionary<string, GameObject>();
        var customTabHighlights = new Dictionary<string, SpriteRenderer>();
        for (var index = 0; index < CustomSettingsTab.SettingsTabs.Count; index++)
        {
            var tabInfo = CustomSettingsTab.SettingsTabs[index];
            GameObject tab;
            SpriteRenderer tabHighlight;
            if (index == 0)
            {
                tab = UnityEngine.Object.Instantiate(roleTab, roleTab.transform.parent);
            }
            else
            {
                var previousInfo = CustomSettingsTab.SettingsTabs[index - 1];
                var previousTab = customTabs[previousInfo.Key];
                tab = UnityEngine.Object.Instantiate(roleTab, previousTab.transform);
            }

            customTabs[tabInfo.Key] = tab;
            tabHighlight = GetTabHighlight(tab, $"{tabInfo.Key}Tab", tabInfo.IconPath);
            customTabHighlights[tabInfo.Key] = tabHighlight;
        }

        gameTab.transform.position += Vector3.left * 3f;
        roleTab.transform.position += Vector3.left * 3f;
        for (var index = 0; index < CustomSettingsTab.SettingsTabs.Count; index++)
        {
            var tabInfo = CustomSettingsTab.SettingsTabs[index];
            var tab = customTabs[tabInfo.Key];
            if (index == 0)
            {
                tab.transform.position += Vector3.left * 2f;
            }
            else
            {
                tab.transform.localPosition += Vector3.right * 1f;
            }
        }

        var tabs = new List<GameObject> { gameTab, roleTab };
        foreach (var ct in customTabs)
        {
            tabs.Add(ct.Value);
        }

        var settingsHighlightMap = new Dictionary<GameObject, SpriteRenderer>
        {
            [gameSettingMenu.RegularGameSettings] = gameSettingMenu.GameSettingsHightlight,
            [gameSettingMenu.RolesSettings.gameObject] = gameSettingMenu.RolesSettingsHightlight,
        };
        foreach (var cs in customSettings)
        {
            settingsHighlightMap[cs.Value.gameObject] = customTabHighlights[cs.Key];
        }

        for (var i = 0; i < tabs.Count; i++)
        {
            var button = tabs[i].GetComponentInChildren<PassiveButton>();
            if (button == null) continue;
            var copiedIndex = i;
            button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.OnClick.AddListener((System.Action) (() =>
            {
                SetListener(settingsHighlightMap, copiedIndex);
            }));
        }

        DestroyOptions(
            customMenus.Select(cm => cm.Value.GetComponentsInChildren<OptionBehaviour>().ToList())
                .ToList());

        var customOptions = new Dictionary<string, List<OptionBehaviour>>();

        var menus = new Dictionary<string, Transform>();
        var optionBehaviours =
            new Dictionary<string, List<OptionBehaviour>>();

        foreach (var cst in CustomSettingsTab.SettingsTabs)
        {
            customOptions[cst.Key] = new List<OptionBehaviour>();
            menus[cst.Key] = customMenus[cst.Key].transform;
            optionBehaviours[cst.Key] = customOptions[cst.Key];
        }

        foreach (var cst in CustomSettingsTab.SettingsTabs)
        {
            for (var settingIndex = 0; settingIndex < cst.Settings.Count; settingIndex++)
            {
                var setting = cst.Settings[settingIndex];
                if (setting.OptionBehaviour == null)
                {
                    var stringOption = UnityEngine.Object.Instantiate(template, menus[cst.Key]);
                    optionBehaviours[cst.Key].Add(stringOption);
                    stringOption.OnValueChanged = new Action<OptionBehaviour>((_) => { });
                    stringOption.TitleText.text = setting.Name;
                    stringOption.Value = stringOption.oldValue = setting.SelectionIndex;
                    stringOption.ValueText.text = setting.StringSelections[setting.SelectionIndex];

                    setting.OptionBehaviour = stringOption;
                }

                setting.OptionBehaviour.gameObject.SetActive(true);
            }
        }

        SetOptions(customMenus.Values.ToList(), optionBehaviours.Values.ToList(), customSettings.Values.ToList());

        UnlockTaskCount(gameOptionsMenu);
        UnlockImpostorCount(gameOptionsMenu);
    }

    private static void SetOptions(
        IReadOnlyList<GameOptionsMenu> menus,
        IReadOnlyList<List<OptionBehaviour>> options,
        IReadOnlyList<GameObject> settings)
    {
        if (menus.Count != options.Count || options.Count != settings.Count)
        {
            EnoModPlugin.Logger.LogError("List counts are not equal");
            return;
        }

        for (var i = 0; i < menus.Count; i++)
        {
            menus[i].Children = options[i].ToArray();
            settings[i].gameObject.SetActive(false);
        }
    }

    private static void DestroyOptions(List<List<OptionBehaviour>> optionBehavioursList)
    {
        foreach (var optionBehaviours in optionBehavioursList)
        {
            foreach (var option in optionBehaviours)
                UnityEngine.Object.Destroy(option.gameObject);
        }
    }

    private static void SetListener(Dictionary<GameObject, SpriteRenderer> settingsHighlightMap, int index)
    {
        foreach (var entry in settingsHighlightMap)
        {
            entry.Key.SetActive(false);
            entry.Value.enabled = false;
        }

        settingsHighlightMap.ElementAt(index).Key.SetActive(true);
        settingsHighlightMap.ElementAt(index).Value.enabled = true;
    }

    private static SpriteRenderer GetTabHighlight(GameObject tab, string tabName, string tabSpritePath)
    {
        var tabHighlight = tab.transform.FindChild("Hat Button").FindChild("Tab Background")
            .GetComponent<SpriteRenderer>();
        tab.transform.FindChild("Hat Button").FindChild("Icon").GetComponent<SpriteRenderer>().sprite =
            Helpers.LoadSpriteFromResources(tabSpritePath, 100f);
        tab.name = "tabName";

        return tabHighlight;
    }

    private static GameOptionsMenu GetMenu(GameObject setting, string settingName)
    {
        var menu = setting.transform.FindChild("GameGroup").FindChild("SliderInner").GetComponent<GameOptionsMenu>();
        setting.name = settingName;

        return menu;
    }

    private static bool SetNames(Dictionary<string, string> gameObjectNameDisplayNameMap)
    {
        foreach (var entry in gameObjectNameDisplayNameMap)
        {
            if (GameObject.Find(entry.Key) != null)
            {
                // Settings setup has already been performed, fixing the title of the tab and returning
                GameObject.Find(entry.Key).transform.FindChild("GameGroup").FindChild("Text")
                    .GetComponent<TMPro.TextMeshPro>().SetText(entry.Value);
                return true;
            }
        }

        return false;
    }

    private static void UnlockTaskCount(GameOptionsMenu __instance)
    {
        var commonTasksOption =
            __instance.Children.FirstOrDefault(x => x.name == "NumCommonTasks")?.TryCast<NumberOption>();
        if (commonTasksOption != null) commonTasksOption.ValidRange = new FloatRange(0f, 4f);
        var shortTasksOption =
            __instance.Children.FirstOrDefault(x => x.name == "NumShortTasks")?.TryCast<NumberOption>();
        if (shortTasksOption != null) shortTasksOption.ValidRange = new FloatRange(0f, 23f);
        var longTasksOption =
            __instance.Children.FirstOrDefault(x => x.name == "NumLongTasks")?.TryCast<NumberOption>();
        if (longTasksOption != null) longTasksOption.ValidRange = new FloatRange(0f, 15f);
    }

    private static void UnlockImpostorCount(GameOptionsMenu __instance)
    {
        var impostorsCountOption =
            __instance.Children.FirstOrDefault(x => x.name == "NumImpostors")?.TryCast<NumberOption>();
        if (impostorsCountOption != null) impostorsCountOption.ValidRange = new FloatRange(1f, 4f);
    }
}

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
public class GameOptionsMenuUpdatePatch
{
    private static float _timer = 1f;

    public static void Postfix(GameOptionsMenu __instance)
    {
        // Return Menu Update if in normal among us settings
        var gameSettingMenu = UnityEngine.Object.FindObjectsOfType<GameSettingMenu>().FirstOrDefault();
        if (gameSettingMenu != null && (gameSettingMenu.RegularGameSettings.active ||
                                        gameSettingMenu.RolesSettings.gameObject.active)) return;

        __instance.GetComponentInParent<Scroller>().ContentYBounds.max = -0.5F + __instance.Children.Length * 0.55F;
        _timer += Time.deltaTime;
        if (_timer < 0.1f) return;
        _timer = 0f;

        foreach (var cst in CustomSettingsTab.SettingsTabs)
        {
            var offset = 2.75f;
            foreach (var setting in cst.Settings)
            {
                if (setting.OptionBehaviour == null || setting.OptionBehaviour.gameObject == null) continue;
                var enabled = true;
                var parent = setting.Parent;
                while (parent != null && enabled)
                {
                    enabled = parent.SelectionIndex != 0;
                    parent = parent.Parent;
                }

                setting.OptionBehaviour.gameObject.SetActive(enabled);
                if (!enabled) continue;
                offset -= setting.IsHeader ? 0.75f : 0.5f;
                var transform = setting.OptionBehaviour.transform;
                var localPosition = transform.localPosition;
                transform.localPosition = new Vector3(localPosition.x, offset, localPosition.z);
            }
        }
    }
}
