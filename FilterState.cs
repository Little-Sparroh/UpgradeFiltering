using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class FilterState
{
    internal static FilterPanelUI FilterPanel;

    internal static readonly Dictionary<Rarity, int> RarityOrder = new()
    {
        { Rarity.Oddity, 5 },
        { Rarity.Exotic, 4 },
        { Rarity.Epic, 3 },
        { Rarity.Rare, 2 },
        { Rarity.Standard, 1 },
        { Rarity.None, 0 }
    };

    internal static FilterSettings CurrentFilters = new()
    {
        HiddenRarities = new List<Rarity>(),
        FilterStats = false,
        StatIncludeList = new List<string>(),
        FavoriteSetting = FavoriteFilter.ShowAll
    };

    internal static bool? PreviousSkinMode;

    private static FieldInfo _upgradeUIsField;
    private static FieldInfo _upgradeUICountField;

    public static bool HasActiveFilters()
    {
        return CurrentFilters.HiddenRarities.Any() ||
               CurrentFilters.FavoriteSetting != FavoriteFilter.ShowAll ||
               (CurrentFilters.FilterStats && CurrentFilters.StatIncludeList.Any());
    }


    public static void ApplyVisibilityOnly(GearDetailsWindow window)
    {
        if (window == null) return;

        var upgradeUIs = GetUpgradeUIs();
        if (upgradeUIs == null || upgradeUIs.Count == 0) return;

        var upgradeUICount = GetUpgradeUICount(window);
        if (upgradeUICount <= 0)
            upgradeUICount = upgradeUIs.Count;
        upgradeUICount = Mathf.Min(upgradeUICount, upgradeUIs.Count);

        var isGrid = PriorityPatches.GetIsGridView(window);
        var visible = 0;
        var hidden = 0;
        var reenabled = 0;

        for (var i = 0; i < upgradeUICount; i++)
        {
            var ui = upgradeUIs[i];
            if (ui == null) continue;

            if (ui.Upgrade?.Upgrade == null)
            {
                if (ui.gameObject.activeSelf)
                    ui.gameObject.SetActive(false);
                hidden++;
                continue;
            }

            var show = ShouldShow(ui);
            if (show)
            {
                var wasInactive = !ui.gameObject.activeSelf;
                if (wasInactive)
                {
                    ui.gameObject.SetActive(true);
                    try
                    {
                        ui.SetUpgrade(ui.Upgrade);
                    }
                    catch
                    {
                    }

                    reenabled++;
                }

                try
                {
                    ui.EnableGridView(isGrid);
                }
                catch
                {
                }

                visible++;
            }
            else
            {
                if (ui.gameObject.activeSelf)
                    ui.gameObject.SetActive(false);
                hidden++;
            }
        }

        UpgradeFilteringPlugin.Logger.LogInfo(
            $"Filter visibility: count={upgradeUICount}, visible={visible}, hidden={hidden}, " +
            $"reenabled={reenabled}, fav={CurrentFilters.FavoriteSetting}");
    }


    public static void ApplyToWindow(GearDetailsWindow window)
    {
        if (window == null)
        {
            UpgradeFilteringPlugin.Logger.LogWarning("ApplyToWindow: window null");
            return;
        }

        var upgradeUIs = GetUpgradeUIs();
        if (upgradeUIs == null || upgradeUIs.Count == 0)
        {
            UpgradeFilteringPlugin.Logger.LogWarning("ApplyToWindow: upgradeUIs empty");
            return;
        }

        var upgradeUICount = GetUpgradeUICount(window);
        if (upgradeUICount <= 0)
            upgradeUICount = upgradeUIs.Count;
        upgradeUICount = Mathf.Min(upgradeUICount, upgradeUIs.Count);

        var rarityStr = CurrentFilters.HiddenRarities != null && CurrentFilters.HiddenRarities.Count > 0
            ? string.Join(",", CurrentFilters.HiddenRarities)
            : "(none)";
        UpgradeFilteringPlugin.Logger.LogInfo(
            $"Filter apply: count={upgradeUICount}, hiddenRarities=[{rarityStr}], " +
            $"fav={CurrentFilters.FavoriteSetting}, stats={CurrentFilters.FilterStats}/" +
            $"{CurrentFilters.StatIncludeList?.Count ?? 0}");

        ApplyVisibilityOnly(window);

        if (PriorityPatches.PrioritySortActive)
        {
            var order = PriorityPatches.FilterOrderForAvailableMods(
                PriorityPatches.LoadPriorityOrder());
            PriorityPatches.ApplyVisualPriorityOrder(window, upgradeUIs, upgradeUICount, order);
        }
        else
        {
            PriorityPatches.ForceLayoutVisibleOnly(window, upgradeUIs, upgradeUICount);
        }

        try
        {
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            typeof(GearDetailsWindow)
                .GetMethod("SetUpgradeListScroll", bf, null, new[] { typeof(float) }, null)
                ?.Invoke(window, new object[] { 1f });
        }
        catch
        {
        }
    }

    internal static List<GearUpgradeUI> GetUpgradeUIs()
    {
        try
        {
            if (_upgradeUIsField == null)
            {
                const BindingFlags f =
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                _upgradeUIsField = typeof(GearDetailsWindow).GetField("upgradeUIs", f);
            }

            if (_upgradeUIsField == null) return null;
            return _upgradeUIsField.GetValue(null) as List<GearUpgradeUI>;
        }
        catch
        {
            return null;
        }
    }

    internal static int GetUpgradeUICount(GearDetailsWindow window)
    {
        if (window == null) return 0;
        try
        {
            if (_upgradeUICountField == null)
            {
                const BindingFlags f =
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                _upgradeUICountField = typeof(GearDetailsWindow).GetField("upgradeUICount", f);
            }

            if (_upgradeUICountField == null) return 0;
            return (int)_upgradeUICountField.GetValue(window);
        }
        catch
        {
            return 0;
        }
    }

    internal static GearDetailsWindow GetOpenWindow()
    {
        try
        {
            if (Menu.Instance?.WindowSystem != null)
            {
                var top = Menu.Instance.WindowSystem.GetTop() as GearDetailsWindow;
                if (top != null) return top;
            }
        }
        catch
        {
        }

        if (PriorityPatches.currentWindow != null)
            try
            {
                if (PriorityPatches.currentWindow.gameObject != null)
                    return PriorityPatches.currentWindow;
            }
            catch
            {
            }

        try
        {
            return Object.FindObjectOfType<GearDetailsWindow>();
        }
        catch
        {
            return null;
        }
    }

    public static bool ShouldShow(GearUpgradeUI ui)
    {
        if (ui?.Upgrade?.Upgrade == null) return false;

        var show = true;

        if (CurrentFilters.HiddenRarities != null && CurrentFilters.HiddenRarities.Count > 0)
            show &= !CurrentFilters.HiddenRarities.Contains(ui.Upgrade.Upgrade.Rarity);

        switch (CurrentFilters.FavoriteSetting)
        {
            case FavoriteFilter.ShowOnlyFavorited:
                show &= ui.Upgrade.Favorite;
                break;
            case FavoriteFilter.HideFavorited:
                show &= !ui.Upgrade.Favorite;
                break;
        }

        if (CurrentFilters.FilterStats && CurrentFilters.StatIncludeList != null &&
            CurrentFilters.StatIncludeList.Count > 0)
            foreach (var requiredProperty in CurrentFilters.StatIncludeList)
            {
                var propertyFound = false;
                var properties = ui.Upgrade.Upgrade.GetProperties();
                while (properties.MoveNext())
                {
                    var property = properties.Current;
                    if (property == null) continue;
                    var propName = property.GetType().Name;
                    if (propName.StartsWith("UpgradeProperty_"))
                        propName = propName.Substring("UpgradeProperty_".Length);
                    else if (propName.StartsWith("SkinUpgradeProperty_"))
                        propName = propName.Substring("SkinUpgradeProperty_".Length);
                    if (propName == requiredProperty)
                    {
                        propertyFound = true;
                        break;
                    }
                }

                if (!propertyFound)
                    return false;
            }

        return show;
    }
}