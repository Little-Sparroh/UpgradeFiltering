using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Sparroh.UI;
using UnityEngine;

public class FilterPanelUI
{
    private readonly Dictionary<Rarity, UIButton> _rarityButtons = new();
    private UIButton _favHide;
    private UIButton _favOnly;
    private UIButton _favShowAll;
    private UIScrollView _statScroll;
    private UIWindow _window;
    private bool isInitialized;

    public bool IsExpanded { get; private set; }

    public void Toggle()
    {
        if (!isInitialized)
        {
            CreateFilterPanel();
            if (!isInitialized) return;
        }

        IsExpanded = !IsExpanded;
        if (IsExpanded)
        {
            RegenerateStatFilters();
            _window.Show();
        }
        else
        {
            _window.Hide(false);
        }
    }

    public void RegenerateStatFilters()
    {
        try
        {
            if (!isInitialized || _statScroll == null) return;
            FilterState.CurrentFilters.StatIncludeList.Clear();
            FilterState.CurrentFilters.FilterStats = false;
            RebuildStatToggles();
        }
        catch
        {
        }
    }

    public void RebuildFilterPanel()
    {
        try
        {
            var wasExpanded = IsExpanded;
            if (_window != null)
            {
                _window.Destroy();
                _window = null;
            }

            _rarityButtons.Clear();
            isInitialized = false;
            IsExpanded = false;
            CreateFilterPanel();
            if (wasExpanded && isInitialized)
            {
                IsExpanded = true;
                _window.Show();
            }
        }
        catch
        {
        }
    }

    private void CreateFilterPanel()
    {
        if (isInitialized) return;

        try
        {
            UITheme.Initialize();
            _window = UIWindow.Create("UpgradeFilter", new Vector2(280f, 480f), "Upgrade Filters",
                true, true, UITheme.WindowSortingOrder + 7);
            _window.OnClose(() => { IsExpanded = false; });

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

            foreach (var r in rarities)

            {
                var rarity = r.R;
                var initiallyHidden = FilterState.CurrentFilters.HiddenRarities.Contains(rarity);
                var btn = UIButton.Create(body, r.Name, () =>
                    {
                        if (FilterState.CurrentFilters.HiddenRarities.Contains(rarity))
                        {
                            FilterState.CurrentFilters.HiddenRarities.Remove(rarity);
                            _rarityButtons[rarity].SetStyle(UIButtonStyle.Default);
                            UpgradeFilteringPlugin.Logger.LogInfo($"Filter: show rarity {rarity}");
                        }
                        else
                        {
                            FilterState.CurrentFilters.HiddenRarities.Add(rarity);
                            _rarityButtons[rarity].SetStyle(UIButtonStyle.Danger);
                            UpgradeFilteringPlugin.Logger.LogInfo($"Filter: hide rarity {rarity}");
                        }

                        UpgradeFilteringPlugin.Logger.LogInfo(
                            $"Filter: HiddenRarities=[{string.Join(",", FilterState.CurrentFilters.HiddenRarities)}]");
                        RefreshUpgrades();
                    }, initiallyHidden ? UIButtonStyle.Danger : UIButtonStyle.Default,
                    preferredHeight: UITheme.S(24f));

                _rarityButtons[rarity] = btn;
            }


            UIText.Create(body, "FavLbl", "Favorites", UITheme.ScaledFontSmall, UIColors.TextSecondary);
            _favShowAll = UIButton.Create(body, "Show All", () =>
            {
                FilterState.CurrentFilters.FavoriteSetting = FavoriteFilter.ShowAll;
                UpdateFavoriteHighlights();
                RefreshUpgrades();
            }, UIButtonStyle.Active, preferredHeight: UITheme.S(24f));
            _favOnly = UIButton.Create(body, "Only Favorite", () =>
            {
                FilterState.CurrentFilters.FavoriteSetting =
                    FavoriteFilter.ShowOnlyFavorited;
                UpdateFavoriteHighlights();
                RefreshUpgrades();
            }, preferredHeight: UITheme.S(24f));
            _favHide = UIButton.Create(body, "Hide Favorite", () =>
            {
                FilterState.CurrentFilters.FavoriteSetting = FavoriteFilter.HideFavorited;
                UpdateFavoriteHighlights();
                RefreshUpgrades();
            }, preferredHeight: UITheme.S(24f));

            UIText.Create(body, "StatLbl", "Show Only With", UITheme.ScaledFontSmall, UIColors.TextSecondary);
            _statScroll = UIScrollView.Create(body, "StatScroll");
            UIHelpers.EnsureLayoutElement(_statScroll.GameObject, preferredHeight: UITheme.S(160f),
                minHeight: UITheme.S(120f));
            RebuildStatToggles();

            _window.Hide(false);
            isInitialized = true;
            IsExpanded = false;
        }
        catch (Exception)
        {
            isInitialized = false;
        }
    }

    private void UpdateFavoriteHighlights()
    {
        var sel = FilterState.CurrentFilters.FavoriteSetting;
        if (_favShowAll != null)
            _favShowAll.SetStyle(sel == FavoriteFilter.ShowAll
                ? UIButtonStyle.Active
                : UIButtonStyle.Default);
        if (_favOnly != null)
            _favOnly.SetStyle(sel == FavoriteFilter.ShowOnlyFavorited
                ? UIButtonStyle.Active
                : UIButtonStyle.Default);
        if (_favHide != null)
            _favHide.SetStyle(sel == FavoriteFilter.HideFavorited
                ? UIButtonStyle.Active
                : UIButtonStyle.Default);
    }

    private void ClearAllFilters()
    {
        FilterState.CurrentFilters.HiddenRarities.Clear();
        FilterState.CurrentFilters.FavoriteSetting = FavoriteFilter.ShowAll;
        FilterState.CurrentFilters.FilterStats = false;
        FilterState.CurrentFilters.StatIncludeList.Clear();

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
                    FilterState.CurrentFilters.FilterStats = true;
                    if (!FilterState.CurrentFilters.StatIncludeList.Contains(prop))
                        FilterState.CurrentFilters.StatIncludeList.Add(prop);
                }
                else
                {
                    FilterState.CurrentFilters.StatIncludeList.Remove(prop);
                    if (FilterState.CurrentFilters.StatIncludeList.Count == 0)
                        FilterState.CurrentFilters.FilterStats = false;
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
            var window = FilterState.GetOpenWindow();
            var isSkinMode = false;
            if (window != null)

            {
                var inSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
                if (inSkinModeField != null)
                    try
                    {
                        isSkinMode = (bool)inSkinModeField.GetValue(window);
                    }
                    catch
                    {
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
                var propertyName = type.Name;
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
        var window = PriorityPatches.ResolveWindow() ?? FilterState.GetOpenWindow();
        if (window == null)
        {
            UpgradeFilteringPlugin.Logger.LogWarning("Filter refresh: no GearDetailsWindow.");
            return;
        }

        UpgradeFilteringPlugin.Logger.LogInfo(
            $"Filter refresh: window={window.name}, priorityActive={PriorityPatches.PrioritySortActive}");

        try
        {
            FilterState.ApplyToWindow(window);
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogError($"Filter refresh failed: {ex.Message}\n{ex.StackTrace}");
        }
    }
}