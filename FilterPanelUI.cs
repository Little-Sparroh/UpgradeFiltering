using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Sparroh.UI;

/// <summary>
/// Themed upgrade filter panel (SparrohUILib UIWindow).
/// Opened via GearActionBar "Filter" button.
/// </summary>
public class FilterPanelUI
{
    private UIWindow _window;
    private bool isExpanded;
    private bool isInitialized;

    private readonly Dictionary<Rarity, UIButton> _rarityButtons = new Dictionary<Rarity, UIButton>();
    private UIButton _favShowAll;
    private UIButton _favOnly;
    private UIButton _favHide;
    private UIScrollView _statScroll;

    public bool IsExpanded => isExpanded;

    public void Toggle()
    {
        if (!isInitialized)
        {
            CreateFilterPanel();
            if (!isInitialized) return;
        }

        isExpanded = !isExpanded;
        if (isExpanded)
        {
            RegenerateStatFilters();
            _window.Show();
        }
        else
        {
            _window.Hide(invokeClose: false);
        }
    }

    public void RegenerateStatFilters()
    {
        try
        {
            if (!isInitialized || _statScroll == null) return;
            UpgradeFilteringPlugin.CurrentFilters.StatIncludeList.Clear();
            UpgradeFilteringPlugin.CurrentFilters.FilterStats = false;
            RebuildStatToggles();
        }
        catch { /* ignore */ }
    }

    public void RebuildFilterPanel()
    {
        try
        {
            bool wasExpanded = isExpanded;
            if (_window != null)
            {
                _window.Destroy();
                _window = null;
            }
            _rarityButtons.Clear();
            isInitialized = false;
            isExpanded = false;
            CreateFilterPanel();
            if (wasExpanded && isInitialized)
            {
                isExpanded = true;
                _window.Show();
            }
        }
        catch { /* ignore */ }
    }

    private void CreateFilterPanel()
    {
        if (isInitialized) return;

        try
        {
            UITheme.Initialize();
            _window = UIWindow.Create("UpgradeFilter", new Vector2(280f, 480f), "Upgrade Filters",
                scrollable: true, closeButton: true, sortingOrder: UITheme.WindowSortingOrder + 7);
            _window.OnClose(() => { isExpanded = false; });

            var body = _window.Content;
            UIFactory.AddVerticalLayout(body.gameObject, UITheme.S(6f), UITheme.ScaledPadding(6, 6, 6, 6));

            UIButton.Create(body, "Clear All Filters", ClearAllFilters, UIButtonStyle.Danger,
                preferredHeight: UITheme.S(28f));

            UIText.Create(body, "RarityLbl", "Hide Rarities", UITheme.ScaledFontSmall, UIColors.TextSecondary);
            var rarities = new (string Name, Rarity R)[]
            {
                ("Standard", Rarity.Standard),
                ("Rare", Rarity.Rare),
                ("Epic", Rarity.Epic),
                ("Exotic", Rarity.Exotic),
                ("Oddity", Rarity.Oddity)
            };
            UpgradeFilteringPlugin.CurrentFilters.HiddenRarities.Clear();
            foreach (var r in rarities)
            {
                var rarity = r.R;
                var btn = UIButton.Create(body, r.Name, () =>
                {
                    if (UpgradeFilteringPlugin.CurrentFilters.HiddenRarities.Contains(rarity))
                    {
                        UpgradeFilteringPlugin.CurrentFilters.HiddenRarities.Remove(rarity);
                        _rarityButtons[rarity].SetStyle(UIButtonStyle.Default);
                    }
                    else
                    {
                        UpgradeFilteringPlugin.CurrentFilters.HiddenRarities.Add(rarity);
                        _rarityButtons[rarity].SetStyle(UIButtonStyle.Danger);
                    }
                    RefreshUpgrades();
                }, UIButtonStyle.Default, preferredHeight: UITheme.S(24f));
                _rarityButtons[rarity] = btn;
            }

            UIText.Create(body, "FavLbl", "Favorites", UITheme.ScaledFontSmall, UIColors.TextSecondary);
            _favShowAll = UIButton.Create(body, "Show All", () =>
            {
                UpgradeFilteringPlugin.CurrentFilters.FavoriteSetting = SortHandlingMod.FavoriteFilter.ShowAll;
                UpdateFavoriteHighlights();
                RefreshUpgrades();
            }, UIButtonStyle.Active, preferredHeight: UITheme.S(24f));
            _favOnly = UIButton.Create(body, "Only Favorite", () =>
            {
                UpgradeFilteringPlugin.CurrentFilters.FavoriteSetting = SortHandlingMod.FavoriteFilter.ShowOnlyFavorited;
                UpdateFavoriteHighlights();
                RefreshUpgrades();
            }, UIButtonStyle.Default, preferredHeight: UITheme.S(24f));
            _favHide = UIButton.Create(body, "Hide Favorite", () =>
            {
                UpgradeFilteringPlugin.CurrentFilters.FavoriteSetting = SortHandlingMod.FavoriteFilter.HideFavorited;
                UpdateFavoriteHighlights();
                RefreshUpgrades();
            }, UIButtonStyle.Default, preferredHeight: UITheme.S(24f));

            UIText.Create(body, "StatLbl", "Show Only With", UITheme.ScaledFontSmall, UIColors.TextSecondary);
            _statScroll = UIScrollView.Create(body, "StatScroll");
            UIHelpers.EnsureLayoutElement(_statScroll.GameObject, preferredHeight: UITheme.S(160f), minHeight: UITheme.S(120f));
            RebuildStatToggles();

            _window.Hide(invokeClose: false);
            isInitialized = true;
            isExpanded = false;
        }
        catch (Exception)
        {
            isInitialized = false;
        }
    }

    private void UpdateFavoriteHighlights()
    {
        var sel = UpgradeFilteringPlugin.CurrentFilters.FavoriteSetting;
        if (_favShowAll != null)
            _favShowAll.SetStyle(sel == SortHandlingMod.FavoriteFilter.ShowAll ? UIButtonStyle.Active : UIButtonStyle.Default);
        if (_favOnly != null)
            _favOnly.SetStyle(sel == SortHandlingMod.FavoriteFilter.ShowOnlyFavorited ? UIButtonStyle.Active : UIButtonStyle.Default);
        if (_favHide != null)
            _favHide.SetStyle(sel == SortHandlingMod.FavoriteFilter.HideFavorited ? UIButtonStyle.Active : UIButtonStyle.Default);
    }

    private void ClearAllFilters()
    {
        UpgradeFilteringPlugin.CurrentFilters.HiddenRarities.Clear();
        UpgradeFilteringPlugin.CurrentFilters.FavoriteSetting = SortHandlingMod.FavoriteFilter.ShowAll;
        UpgradeFilteringPlugin.CurrentFilters.FilterStats = false;
        UpgradeFilteringPlugin.CurrentFilters.StatIncludeList.Clear();

        foreach (var kv in _rarityButtons)
            kv.Value.SetStyle(UIButtonStyle.Default);
        UpdateFavoriteHighlights();
        RebuildStatToggles();
        RefreshUpgrades();
    }

    private void RebuildStatToggles()
    {
        if (_statScroll == null) return;
        UIHelpers.DestroyChildren(_statScroll.Content);

        foreach (var propertyName in GetContextAwareProperties().OrderBy(p => p))
        {
            var displayName = propertyName.Replace("_", " ");
            var prop = propertyName;
            var toggle = UIToggle.Create(_statScroll.Content, displayName, false, value =>
            {
                if (value)
                {
                    UpgradeFilteringPlugin.CurrentFilters.FilterStats = true;
                    if (!UpgradeFilteringPlugin.CurrentFilters.StatIncludeList.Contains(prop))
                        UpgradeFilteringPlugin.CurrentFilters.StatIncludeList.Add(prop);
                }
                else
                {
                    UpgradeFilteringPlugin.CurrentFilters.StatIncludeList.Remove(prop);
                    if (UpgradeFilteringPlugin.CurrentFilters.StatIncludeList.Count == 0)
                        UpgradeFilteringPlugin.CurrentFilters.FilterStats = false;
                }
                RefreshUpgrades();
            });
            UIHelpers.EnsureLayoutElement(toggle.GameObject, preferredHeight: UITheme.S(22f));
        }
    }

    private List<string> GetContextAwareProperties()
    {
        try
        {
            var window = GameObject.Find("Gear Details")?.GetComponent<GearDetailsWindow>();
            bool isSkinMode = false;
            if (window != null)
            {
                var inSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
                if (inSkinModeField != null)
                {
                    try { isSkinMode = (bool)inSkinModeField.GetValue(window); } catch { /* ignore */ }
                }
            }
            return isSkinMode ? DiscoverSkinProperties() : GetCuratedUpgradeProperties();
        }
        catch
        {
            return GetCuratedUpgradeProperties();
        }
    }

    private static List<string> GetCuratedUpgradeProperties()
    {
        return new List<string>
        {
            "AmmoCapacity", "AutomaticFire", "BatteryCapacity", "BulletsPerShot", "BurstFire",
            "Carver_Blood", "Charge", "Damage", "FireInterval", "Globbler_Globblometer",
            "Health", "HealthRegenDelay", "HitForce", "MagazineSize", "MaxBounces",
            "MeleeDamage", "Range", "Recoil", "Reload", "Speed"
        };
    }

    private static List<string> DiscoverSkinProperties()
    {
        var properties = new List<string>();
        try
        {
            var skinUpgradePropertyType = typeof(GearUpgradeUI).Assembly.GetType("SkinUpgradeProperty");
            if (skinUpgradePropertyType == null) return properties;
            var skinTypes = skinUpgradePropertyType.Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract &&
                            skinUpgradePropertyType.IsAssignableFrom(t) &&
                            t.FullName != null && t.FullName.StartsWith("SkinUpgradeProperty_"))
                .ToList();
            foreach (var type in skinTypes)
            {
                string propertyName = type.Name;
                if (propertyName.StartsWith("SkinUpgradeProperty_"))
                    propertyName = propertyName.Substring("SkinUpgradeProperty_".Length);
                properties.Add(propertyName);
            }
            return properties.Distinct().OrderBy(p => p).ToList();
        }
        catch
        {
            return properties;
        }
    }

    private void RefreshUpgrades()
    {
        var window = GameObject.Find("Gear Details")?.GetComponent<GearDetailsWindow>();
        if (window == null) return;

        try
        {
            var inSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
            bool isInSkinMode = false;
            if (inSkinModeField != null)
                isInSkinMode = (bool)inSkinModeField.GetValue(window);

            var sortingMethodField = isInSkinMode
                ? AccessTools.Field(typeof(GearDetailsWindow), "currentSortingMethodSkins")
                : AccessTools.Field(typeof(GearDetailsWindow), "currentSortingMethodUpgrades");

            GearDetailsWindow.SortingMethod currentMethod = GearDetailsWindow.SortingMethod.Name;
            if (sortingMethodField != null)
            {
                try { currentMethod = (GearDetailsWindow.SortingMethod)sortingMethodField.GetValue(window); }
                catch { /* ignore */ }
            }

            var upgradeUIsField = AccessTools.Field(typeof(GearDetailsWindow), "upgradeUIs") ??
                                  AccessTools.Field(typeof(GearDetailsWindow), "<upgradeUIs>k__BackingField") ??
                                  AccessTools.Field(typeof(GearDetailsWindow), "upgrades");

            if (upgradeUIsField != null)
            {
                var upgradeUIs = (List<GearUpgradeUI>)upgradeUIsField.GetValue(window);
                if (upgradeUIs != null)
                {
                    int visibleCount = 0;
                    foreach (var ui in upgradeUIs)
                    {
                        bool show = true;
                        if (UpgradeFilteringPlugin.CurrentFilters.HiddenRarities.Any())
                            show &= !UpgradeFilteringPlugin.CurrentFilters.HiddenRarities.Contains(ui.Upgrade.Upgrade.Rarity);

                        switch (UpgradeFilteringPlugin.CurrentFilters.FavoriteSetting)
                        {
                            case SortHandlingMod.FavoriteFilter.ShowOnlyFavorited:
                                show &= ui.Upgrade.Favorite;
                                break;
                            case SortHandlingMod.FavoriteFilter.HideFavorited:
                                show &= !ui.Upgrade.Favorite;
                                break;
                        }

                        if (UpgradeFilteringPlugin.CurrentFilters.FilterStats &&
                            UpgradeFilteringPlugin.CurrentFilters.StatIncludeList.Any())
                        {
                            bool hasAllProperties = true;
                            foreach (var requiredProperty in UpgradeFilteringPlugin.CurrentFilters.StatIncludeList)
                            {
                                bool propertyFound = false;
                                var properties = ui.Upgrade.Upgrade.GetProperties();
                                while (properties.MoveNext())
                                {
                                    var property = properties.Current;
                                    string propName = property.GetType().Name;
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
                                {
                                    hasAllProperties = false;
                                    break;
                                }
                            }
                            show &= hasAllProperties;
                        }

                        ui.gameObject.SetActive(show);
                        if (show)
                        {
                            visibleCount++;
                            ui.transform.SetSiblingIndex(visibleCount - 1);
                        }
                    }

                    var countField = AccessTools.Field(typeof(GearDetailsWindow), "upgradeUICount");
                    if (countField != null)
                        countField.SetValue(window, visibleCount);
                }
            }

            AccessTools.Method(typeof(GearDetailsWindow), "SortUpgrades",
                    new Type[] { typeof(GearDetailsWindow.SortingMethod), typeof(bool) })
                ?.Invoke(window, new object[] { currentMethod, false });

            try
            {
                AccessTools.Method(typeof(GearDetailsWindow), "SwitchUpgradeView")?.Invoke(window, new object[0]);
                AccessTools.Method(typeof(GearDetailsWindow), "SwitchUpgradeView")?.Invoke(window, new object[0]);
            }
            catch { /* ignore */ }
        }
        catch { /* ignore */ }
    }
}
