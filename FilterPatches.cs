using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public static class SortHandlingMod
{
    public struct FilterSettings
    {
        public List<Rarity> HiddenRarities;
        public bool FilterStats;
        public List<string> StatIncludeList;
        public SortHandlingMod.FavoriteFilter FavoriteSetting;
    }

    public enum FavoriteFilter
    {
        ShowAll,
        ShowOnlyFavorited,
        HideFavorited
    }
}

public static class UpgradeSortingPlugin
{
    internal static FilterPanelUI FilterPanel;

    internal static readonly Dictionary<Rarity, int> RarityOrder = new System.Collections.Generic.Dictionary<Rarity, int> {
        { Rarity.Oddity, 5 },
        { Rarity.Exotic, 4 },
        { Rarity.Epic, 3 },
        { Rarity.Rare, 2 },
        { Rarity.Standard, 1 },
        { Rarity.None, 0 }
    };
    internal static SortHandlingMod.FilterSettings CurrentFilters = new SortHandlingMod.FilterSettings {
        HiddenRarities = new List<Rarity>(),
        FilterStats = false,
        StatIncludeList = new List<string>(),
        FavoriteSetting = SortHandlingMod.FavoriteFilter.ShowAll
    };
    internal static bool? PreviousSkinMode = null;

    // FilterPanelUI moved to FilterPanelUI.cs (SparrohUILib themed window)
    private class FilterPanelUI_LegacyRemoved
        {
            private GameObject filterPanel;
            private bool isExpanded;
            private bool isInitialized;

            public FilterPanelUI_LegacyRemoved()
            {
            }


            private void CreateFilterPanel()
            {
                if (isInitialized) return;

                var upgradeList = GameObject.Find("Gear Details/UpgradeList");
                Transform parentTransform = null;

                if (upgradeList != null)
                {
                    parentTransform = upgradeList.transform;
                }
                else
                {
                    var gearDetails = GameObject.Find("Gear Details");
                    if (gearDetails != null)
                    {
                        parentTransform = gearDetails.transform;
                    }
                    else
                    {
                        return;
                    }
                }

                filterPanel = new GameObject("UpgradeFilterPanel");
                filterPanel.transform.SetParent(parentTransform, false);

                var bgImage = filterPanel.AddComponent<Image>();
                bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

                var layoutGroup = filterPanel.AddComponent<VerticalLayoutGroup>();
                layoutGroup.padding = new RectOffset(5, 5, 5, 5);
                layoutGroup.spacing = 3;
                layoutGroup.childAlignment = TextAnchor.UpperLeft;
                layoutGroup.childControlHeight = false;
                layoutGroup.childControlWidth = false;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = false;

                var rectTransform = filterPanel.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.sizeDelta = new Vector2(200, 300);
                rectTransform.anchoredPosition = new Vector2(-200, -80);


                if (filterPanel == null)
                {
                    return;
                }

                if (filterPanel.transform == null)
                {
                    return;
                }

                try
                {
                    CreateClearAllFiltersButton();

                    CreateRarityFilter();

                    CreateFavoriteFilter();

                    CreateStatFilters();

                }
                catch (Exception e)
                {
                    return;
                }

                filterPanel.SetActive(false);
                isInitialized = true;
            }

            private void CreateRarityFilter()
            {
                var rarityLabel = CreateUITextObject("RarityFilterLabel", "Hide Rarities:", 12, filterPanel.transform);
                var rarityRect = rarityLabel.GetComponent<RectTransform>();
                rarityRect.sizeDelta = new Vector2(180, 20);

                var rarities = new[]
                {
                    new { Name = "Standard", Rarity = Rarity.Standard },
                    new { Name = "Rare", Rarity = Rarity.Rare },
                    new { Name = "Epic", Rarity = Rarity.Epic },
                    new { Name = "Exotic", Rarity = Rarity.Exotic },
                    new { Name = "Oddity", Rarity = Rarity.Oddity }
                };

                UpgradeSortingPlugin.CurrentFilters.HiddenRarities.Clear();

                foreach (var rarity in rarities)
                {
                    CreateRarityToggleButton(rarity.Name, rarity.Rarity);
                }

                FixRarityButtonTextPosition();
            }

            private GameObject CreateRarityToggleButton(string text, Rarity rarity)
            {
                try
                {
                    if (filterPanel == null)
                    {
                        return null;
                    }

                    var buttonGo = new GameObject($"FilterButton_{text.Replace(" ", "_")}");
                    if (buttonGo == null)
                    {
                        return null;
                    }

                    var buttonRectTransform = buttonGo.AddComponent<RectTransform>();
                    buttonGo.transform.SetParent(filterPanel.transform, false);

                    var buttonImage = buttonGo.AddComponent<Image>();
                    buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

                    var button = buttonGo.AddComponent<Button>();
                    if (button == null)
                    {
                        return null;
                    }

                    var buttonTextGo = new GameObject("Text");
                    if (buttonTextGo == null)
                    {
                        return null;
                    }

                    buttonTextGo.transform.SetParent(buttonGo.transform, false);
                    var buttonText = buttonTextGo.AddComponent<TextMeshProUGUI>();
                    buttonText.text = text;
                    buttonText.fontSize = 10;
                    buttonText.color = Color.white;

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (UpgradeSortingPlugin.CurrentFilters.HiddenRarities.Contains(rarity))
                        {
                            UpgradeSortingPlugin.CurrentFilters.HiddenRarities.Remove(rarity);
                            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                        }
                        else
                        {
                            UpgradeSortingPlugin.CurrentFilters.HiddenRarities.Add(rarity);
                            buttonImage.color = new Color(0.6f, 0.3f, 0.3f, 1f);
                        }

                        RefreshUpgrades();
                    });

                    var buttonRect = buttonGo.GetComponent<RectTransform>();
                    if (buttonRect == null)
                    {
                        return null;
                    }

                    buttonRect.sizeDelta = new Vector2(170, 20);

                    return buttonGo;
                }
                catch (Exception e)
                {
                    return null;
                }
            }

            private void CreateFavoriteFilter()
            {
                var favLabel = CreateUITextObject("FavoriteFilterLabel", "Favorites:", 12, filterPanel.transform);
                favLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 20);

                GameObject showAllBtn = null, onlyFavBtn = null, hideFavBtn = null;

                showAllBtn = CreateFilterButton("Show All", () =>
                {
                    UpgradeSortingPlugin.CurrentFilters.FavoriteSetting = SortHandlingMod.FavoriteFilter.ShowAll;

                    UpdateFavoriteButtonHighlights(showAllBtn, onlyFavBtn, hideFavBtn, SortHandlingMod.FavoriteFilter.ShowAll);

                    RefreshUpgrades();
                });

                onlyFavBtn = CreateFilterButton("Only Favorite", () =>
                {
                    UpgradeSortingPlugin.CurrentFilters.FavoriteSetting = SortHandlingMod.FavoriteFilter.ShowOnlyFavorited;

                    UpdateFavoriteButtonHighlights(showAllBtn, onlyFavBtn, hideFavBtn,
                        SortHandlingMod.FavoriteFilter.ShowOnlyFavorited);

                    RefreshUpgrades();
                });

                hideFavBtn = CreateFilterButton("Hide Favorite", () =>
                {
                    UpgradeSortingPlugin.CurrentFilters.FavoriteSetting = SortHandlingMod.FavoriteFilter.HideFavorited;

                    UpdateFavoriteButtonHighlights(showAllBtn, onlyFavBtn, hideFavBtn, SortHandlingMod.FavoriteFilter.HideFavorited);

                    RefreshUpgrades();
                });

                if (showAllBtn != null) showAllBtn.GetComponent<Button>().interactable = true;
                if (onlyFavBtn != null) onlyFavBtn.GetComponent<Button>().interactable = true;
                if (hideFavBtn != null) hideFavBtn.GetComponent<Button>().interactable = true;

                UpdateFavoriteButtonHighlights(showAllBtn, onlyFavBtn, hideFavBtn, SortHandlingMod.FavoriteFilter.ShowAll);

                FixFavoriteButtonTextPosition(showAllBtn);
                FixFavoriteButtonTextPosition(onlyFavBtn);
                FixFavoriteButtonTextPosition(hideFavBtn);
            }

            private void UpdateFavoriteButtonHighlights(GameObject showAllBtn, GameObject onlyFavBtn,
                GameObject hideFavBtn, SortHandlingMod.FavoriteFilter selectedFilter)
            {
                if (filterPanel == null) return;

                Color normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                Color highlightColor = new Color(0.5f, 0.7f, 0.5f, 1f);

                if (showAllBtn != null) showAllBtn.GetComponent<Image>().color = normalColor;
                if (onlyFavBtn != null) onlyFavBtn.GetComponent<Image>().color = normalColor;
                if (hideFavBtn != null) hideFavBtn.GetComponent<Image>().color = normalColor;

                switch (selectedFilter)
                {
                    case SortHandlingMod.FavoriteFilter.ShowAll:
                        if (showAllBtn != null) showAllBtn.GetComponent<Image>().color = highlightColor;
                        break;
                    case SortHandlingMod.FavoriteFilter.ShowOnlyFavorited:
                        if (onlyFavBtn != null) onlyFavBtn.GetComponent<Image>().color = highlightColor;
                        break;
                    case SortHandlingMod.FavoriteFilter.HideFavorited:
                        if (hideFavBtn != null) hideFavBtn.GetComponent<Image>().color = highlightColor;
                        break;
                }

            }

            private void CreateStatFilters()
            {
                var statLabel = CreateUITextObject("StatFilterLabel", "Show Only Upgrades With:", 12,
                    filterPanel.transform);
                var statRect = statLabel.GetComponent<RectTransform>();
                statRect.sizeDelta = new Vector2(180, 20);

                var scrollViewGo = new GameObject("StatPropertyScrollView");
                scrollViewGo.transform.SetParent(filterPanel.transform, false);

                var scrollRect = scrollViewGo.AddComponent<ScrollRect>();
                var scrollRectTransform = scrollViewGo.GetComponent<RectTransform>();
                scrollRectTransform.sizeDelta = new Vector2(180, 120);

                var viewportGo = new GameObject("Viewport");
                viewportGo.transform.SetParent(scrollViewGo.transform, false);
                var viewportImage = viewportGo.AddComponent<Image>();
                viewportImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                var viewportRect = viewportGo.GetComponent<RectTransform>();
                viewportRect.sizeDelta = scrollRectTransform.sizeDelta;

                var contentGo = new GameObject("Content");
                contentGo.transform.SetParent(viewportGo.transform, false);
                var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
                contentLayout.padding = new RectOffset(2, 2, 2, 2);
                contentLayout.spacing = 2;
                contentLayout.childControlHeight = false;
                contentLayout.childControlWidth = false;
                contentLayout.childForceExpandHeight = false;
                contentLayout.childForceExpandWidth = false;

                var contentRect = contentGo.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(0, 1);
                contentRect.pivot = new Vector2(0, 1);
                contentRect.sizeDelta = new Vector2(scrollRectTransform.sizeDelta.x - 4, 0);

                var contentSizeFitter = contentGo.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                contentSizeFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                contentSizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

                var mask = viewportGo.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                scrollRect.viewport = viewportRect;
                scrollRect.content = contentRect;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.scrollSensitivity = 1f;
                scrollRect.elasticity = 0.01f;
                scrollRect.inertia = false;
                scrollRect.decelerationRate = 0.01f;

                var contextProperties = GetContextAwareProperties();

                var existingToggles = new List<GameObject>();
                foreach (Transform child in contentGo.transform)
                {
                    if (child.gameObject.name.StartsWith("StatToggle_") || child.gameObject.name == "Toggle")
                    {
                        existingToggles.Add(child.gameObject);
                    }
                }

                foreach (var toggleGo in existingToggles)
                {
                    UnityEngine.Object.DestroyImmediate(toggleGo);
                }

                foreach (var propertyName in contextProperties.OrderBy(p => p))
                {
                    var displayName = propertyName.Replace("_", " ");
                    var statToggle = CreateUIToggleObject($"StatToggle_{propertyName}", displayName, 8,
                        contentGo.transform,
                        (value) =>
                        {
                            if (value)
                            {
                                UpgradeSortingPlugin.CurrentFilters.FilterStats = true;
                                if (!UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Contains(propertyName))
                                {
                                    UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Add(propertyName);
                                }
                            }
                            else
                            {
                                UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Remove(propertyName);
                                if (UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Count == 0)
                                {
                                    UpgradeSortingPlugin.CurrentFilters.FilterStats = false;
                                }
                            }

                            RefreshUpgrades();
                        });
                    var toggleRect = statToggle.GetComponent<RectTransform>();
                    toggleRect.sizeDelta = new Vector2(170, 16);
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
                            try
                            {
                                isSkinMode = (bool)inSkinModeField.GetValue(window);
                            }
                            catch (Exception e)
                            {
                            }
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                    }

                    if (isSkinMode)
                    {
                        var skinProperties = DiscoverSkinProperties();
                        return skinProperties;
                    }
                    else
                    {
                        var upgradeProperties = GetCuratedUpgradeProperties();
                        return upgradeProperties;
                    }
                }
                catch (Exception e)
                {
                    return GetCuratedUpgradeProperties();
                }
            }

            private List<string> GetCuratedUpgradeProperties()
            {
                return new List<string>
                {
                    "AmmoCapacity",
                    "AutomaticFire",
                    "BatteryCapacity",
                    "BulletsPerShot",
                    "BurstFire",
                    "Carver_Blood",
                    "Charge",
                    "Damage",
                    "FireInterval",
                    "Globbler_Globblometer",
                    "Health",
                    "HealthRegenDelay",
                    "HitForce",
                    "MagazineSize",
                    "MaxBounces",
                    "MeleeDamage",
                    "Range",
                    "Recoil",
                    "Reload",
                    "Speed"
                };
            }

            private List<string> DiscoverSkinProperties()
            {
                var properties = new List<string>();

                try
                {
                    var skinUpgradePropertyType = typeof(GearUpgradeUI).Assembly.GetType("SkinUpgradeProperty");
                    if (skinUpgradePropertyType == null)
                    {
                        return properties;
                    }

                    var skinTypes = skinUpgradePropertyType.Assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract &&
                                    skinUpgradePropertyType.IsAssignableFrom(t) &&
                                    t.FullName != null && t.FullName.StartsWith("SkinUpgradeProperty_"))
                        .ToList();

                    foreach (var type in skinTypes)
                    {
                        string propertyName = type.Name;
                        if (propertyName.StartsWith("SkinUpgradeProperty_"))
                        {
                            propertyName = propertyName.Substring("SkinUpgradeProperty_".Length);
                        }

                        properties.Add(propertyName);
                    }

                    properties = properties.Distinct().OrderBy(p => p).ToList();

                    return properties;
                }
                catch (Exception e)
                {
                    return properties;
                }
            }

            private List<string> DiscoverUpgradeProperties()
            {
                var properties = new List<string>();

                try
                {
                    var upgradePropertyType = typeof(GearUpgradeUI).Assembly.GetType("UpgradeProperty");
                    if (upgradePropertyType == null)
                    {
                        return GetFallbackProperties();
                    }

                    var skinUpgradePropertyType = typeof(GearUpgradeUI).Assembly.GetType("SkinUpgradeProperty");
                    var allTypes = upgradePropertyType.Assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract &&
                                    (t == upgradePropertyType || upgradePropertyType.IsAssignableFrom(t)) &&
                                    t.FullName != null && t.FullName.StartsWith("UpgradeProperty_"))
                        .ToList();

                    if (skinUpgradePropertyType != null)
                    {
                        allTypes = allTypes.Where(t => !skinUpgradePropertyType.IsAssignableFrom(t)).ToList();
                    }

                    foreach (var type in allTypes)
                    {
                        string propertyName = type.Name;
                        if (propertyName.StartsWith("UpgradeProperty_"))
                        {
                            propertyName = propertyName.Substring("UpgradeProperty_".Length);
                        }

                        properties.Add(propertyName);
                    }

                    properties = properties.Distinct().OrderBy(p => p).ToList();

                    if (properties.Count == 0)
                    {
                        return GetFallbackProperties();
                    }

                    return properties;
                }
                catch (Exception e)
                {
                    return GetFallbackProperties();
                }
            }

            private List<string> GetFallbackProperties()
            {
                return new List<string>
                {
                    "Damage", "AmmoCapacity", "FireInterval", "AutomaticFire",
                    "BurstFire", "BulletsPerShot", "Aim", "AmmoOnFire",
                    "Recoil", "Reload", "Range", "Health", "Speed"
                };
            }

            private GameObject CreateFilterButton(string text, UnityAction action)
            {
                try
                {
                    if (filterPanel == null)
                    {
                        return null;
                    }

                    var buttonGo = new GameObject($"FilterButton_{text.Replace(" ", "_")}");
                    if (buttonGo == null)
                    {
                        return null;
                    }

                    var buttonRectTransform = buttonGo.AddComponent<RectTransform>();
                    buttonGo.transform.SetParent(filterPanel.transform, false);

                    var buttonImage = buttonGo.AddComponent<Image>();
                    buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

                    var button = buttonGo.AddComponent<Button>();
                    if (button == null)
                    {
                        return null;
                    }

                    var buttonTextGo = new GameObject("Text");
                    if (buttonTextGo == null)
                    {
                        return null;
                    }

                    buttonTextGo.transform.SetParent(buttonGo.transform, false);
                    var buttonText = buttonTextGo.AddComponent<TextMeshProUGUI>();
                    buttonText.text = text;
                    buttonText.fontSize = 10;
                    buttonText.color = Color.white;

                    button.onClick.AddListener(action);

                    var buttonRect = buttonGo.GetComponent<RectTransform>();
                    if (buttonRect == null)
                    {
                        return null;
                    }

                    buttonRect.sizeDelta = new Vector2(170, 20);

                    return buttonGo;
                }
                catch (Exception e)
                {
                    return null;
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
                    {
                        isInSkinMode = (bool)inSkinModeField.GetValue(window);
                    }

                    var sortingMethodField = isInSkinMode
                        ? AccessTools.Field(typeof(GearDetailsWindow), "currentSortingMethodSkins")
                        : AccessTools.Field(typeof(GearDetailsWindow), "currentSortingMethodUpgrades");

                    GearDetailsWindow.SortingMethod currentMethod = GearDetailsWindow.SortingMethod.Name;
                    if (sortingMethodField != null)
                    {
                        try
                        {
                            currentMethod = (GearDetailsWindow.SortingMethod)sortingMethodField.GetValue(window);
                        }
                        catch (Exception e)
                        {
                        }
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

                                if (UpgradeSortingPlugin.CurrentFilters.HiddenRarities.Any())
                                {
                                    show &= !UpgradeSortingPlugin.CurrentFilters.HiddenRarities.Contains(ui.Upgrade
                                        .Upgrade.Rarity);
                                }

                                switch (UpgradeSortingPlugin.CurrentFilters.FavoriteSetting)
                                {
                                    case SortHandlingMod.FavoriteFilter.ShowOnlyFavorited:
                                        show &= ui.Upgrade.Favorite;
                                        break;
                                    case SortHandlingMod.FavoriteFilter.HideFavorited:
                                        show &= !ui.Upgrade.Favorite;
                                        break;
                                }

                                if (UpgradeSortingPlugin.CurrentFilters.FilterStats &&
                                    UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Any())
                                {
                                    bool hasAllProperties = true;
                                    foreach (var requiredProperty in
                                             UpgradeSortingPlugin.CurrentFilters.StatIncludeList)
                                    {
                                        bool propertyFound = false;
                                        var properties = ui.Upgrade.Upgrade.GetProperties();
                                        while (properties.MoveNext())
                                        {
                                            var property = properties.Current;
                                            string propName = property.GetType().Name;
                                            if (propName.StartsWith("UpgradeProperty_"))
                                            {
                                                propName = propName.Substring("UpgradeProperty_".Length);
                                            }
                                            else if (propName.StartsWith("SkinUpgradeProperty_"))
                                            {
                                                propName = propName.Substring("SkinUpgradeProperty_".Length);
                                            }

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
                            {
                                countField.SetValue(window, visibleCount);
                            }

                        }
                    }

                    AccessTools.Method(typeof(GearDetailsWindow), "SortUpgrades",
                            new Type[] { typeof(GearDetailsWindow.SortingMethod), typeof(bool) })
                        ?.Invoke(window, new object[] { currentMethod, false });

                    try
                    {
                        AccessTools.Method(typeof(GearDetailsWindow), "SwitchUpgradeView")
                            ?.Invoke(window, new object[0]);
                        AccessTools.Method(typeof(GearDetailsWindow), "SwitchUpgradeView")
                            ?.Invoke(window, new object[0]);
                    }
                    catch (Exception e)
                    {
                    }


                }
                catch (Exception e)
                {
                }
            }

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
                }
                else
                {
                }

                filterPanel.SetActive(isExpanded);

                var rectTransform = filterPanel.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = isExpanded ? new Vector2(-190, -80) : new Vector2(-200, -80);
            }

            public bool IsExpanded => isExpanded;

            public void RegenerateStatFilters()
            {
                try
                {
                    if (filterPanel == null || !isInitialized)
                    {
                        return;
                    }

                    var scrollViewGo = filterPanel.transform.Find("StatPropertyScrollView/Content");
                    if (scrollViewGo != null)
                    {
                        var contentGo = scrollViewGo.gameObject;

                        var existingToggles = new List<GameObject>();
                        foreach (Transform child in contentGo.transform)
                        {
                            if (child.gameObject.name.StartsWith("StatToggle_") || child.gameObject.name == "Toggle")
                            {
                                existingToggles.Add(child.gameObject);
                            }
                        }

                        foreach (var toggleGo in existingToggles)
                        {
                            UnityEngine.Object.DestroyImmediate(toggleGo);
                        }

                        UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Clear();
                        UpgradeSortingPlugin.CurrentFilters.FilterStats = false;

                        var contextProperties = GetContextAwareProperties();

                        foreach (var propertyName in contextProperties.OrderBy(p => p))
                        {
                            var displayName = propertyName.Replace("_", " ");
                            var statToggle = CreateUIToggleObject($"StatToggle_{propertyName}", displayName, 8,
                                contentGo.transform,
                                (value) =>
                                {
                                    if (value)
                                    {
                                        UpgradeSortingPlugin.CurrentFilters.FilterStats = true;
                                        if (!UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Contains(propertyName))
                                        {
                                            UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Add(propertyName);
                                        }
                                    }
                                    else
                                    {
                                        UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Remove(propertyName);
                                        if (UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Count == 0)
                                        {
                                            UpgradeSortingPlugin.CurrentFilters.FilterStats = false;
                                        }
                                    }

                                    RefreshUpgrades();
                                });
                            var toggleRect = statToggle.GetComponent<RectTransform>();
                            toggleRect.sizeDelta = new Vector2(170, 16);
                        }

                    }
                }
                catch (Exception e)
                {
                }
            }

            public void RebuildFilterPanel()
            {
                try
                {

                    bool wasExpanded = isExpanded && filterPanel != null && filterPanel.activeSelf;

                    if (filterPanel != null)
                    {
                        UnityEngine.Object.DestroyImmediate(filterPanel);
                    }

                    isInitialized = false;
                    isExpanded = false;

                    CreateFilterPanel();

                    if (wasExpanded)
                    {
                        filterPanel.SetActive(true);
                        var rectTransform = filterPanel.GetComponent<RectTransform>();
                        rectTransform.anchoredPosition = new Vector2(10, -80);
                    }

                }
                catch (Exception e)
                {
                }
            }

            private GameObject CreateUITextObject(string objName, string text, float fontSize, Transform parent)
            {
                var go = new GameObject(objName);
                go.transform.SetParent(parent, false);

                var textComponent = go.AddComponent<TextMeshProUGUI>();
                textComponent.text = text;
                textComponent.fontSize = fontSize;
                textComponent.color = Color.white;

                return go;
            }

            private GameObject CreateUIToggleObject(string objName, string text, float fontSize, Transform parent,
                UnityAction<bool> onValueChanged)
            {
                var go = new GameObject(objName);
                go.transform.SetParent(parent, false);

                var toggle = go.AddComponent<Toggle>();
                toggle.onValueChanged.AddListener(onValueChanged);

                var toggleImage = go.AddComponent<Image>();
                toggleImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);

                var textObj = new GameObject("Text");
                textObj.transform.SetParent(go.transform, false);
                var textComponent = textObj.AddComponent<TextMeshProUGUI>();
                textComponent.text = text;
                textComponent.fontSize = fontSize;
                textComponent.color = Color.white;
                textComponent.alignment = TextAlignmentOptions.Left;

                var checkMarkGo = new GameObject("Checkmark");
                checkMarkGo.transform.SetParent(go.transform, false);
                var checkImage = checkMarkGo.AddComponent<Image>();
                checkImage.color = Color.green;

                toggle.graphic = checkImage;

                var textRect = textComponent.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.3f, 0.1f);
                textRect.anchorMax = new Vector2(1f, 0.9f);
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                var checkRect = checkImage.GetComponent<RectTransform>();
                checkRect.anchorMin = new Vector2(0.05f, 0.2f);
                checkRect.anchorMax = new Vector2(0.25f, 0.8f);
                checkRect.offsetMin = Vector2.zero;
                checkRect.offsetMax = Vector2.zero;

                return go;
            }

            private void FixFavoriteButtonTextPosition(GameObject buttonGo)
            {
                try
                {
                    if (buttonGo == null) return;

                    var textComponents = buttonGo.GetComponentsInChildren<TextMeshProUGUI>();
                    if (textComponents.Length > 0)
                    {
                        var textRect = textComponents[0].GetComponent<RectTransform>();
                        textRect.anchorMin = new Vector2(0, 0);
                        textRect.anchorMax = new Vector2(1, 1);
                        textRect.offsetMin = new Vector2(5, 2);
                        textRect.offsetMax = new Vector2(-5, -2);
                        textRect.localPosition = Vector3.zero;
                    }
                }
                catch (Exception e)
                {
                }
            }

            private void CreateClearAllFiltersButton()
            {
                try
                {
                    if (filterPanel == null)
                    {
                        return;
                    }

                    var clearButtonGo = new GameObject("ClearAllFiltersButton");
                    if (clearButtonGo == null)
                    {
                        return;
                    }

                    clearButtonGo.transform.SetParent(filterPanel.transform, false);

                    var buttonImage = clearButtonGo.AddComponent<Image>();
                    buttonImage.color = new Color(0.8f, 0.4f, 0.4f, 1f);

                    var button = clearButtonGo.AddComponent<Button>();
                    if (button == null)
                    {
                        return;
                    }

                    var buttonTextGo = new GameObject("Text");
                    if (buttonTextGo == null)
                    {
                        return;
                    }

                    buttonTextGo.transform.SetParent(clearButtonGo.transform, false);
                    var buttonText = buttonTextGo.AddComponent<TextMeshProUGUI>();
                    buttonText.text = "Clear All Filters";
                    buttonText.fontSize = 12;
                    buttonText.color = Color.white;
                    buttonText.alignment = TextAlignmentOptions.Center;

                    var buttonRect = clearButtonGo.GetComponent<RectTransform>();
                    if (buttonRect == null)
                    {
                        return;
                    }

                    buttonRect.sizeDelta = new Vector2(180, 30);

                    var textRect = buttonText.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = new Vector2(5, 2);
                    textRect.offsetMax = new Vector2(-5, -2);

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {

                        UpgradeSortingPlugin.CurrentFilters.HiddenRarities.Clear();
                        UpgradeSortingPlugin.CurrentFilters.FavoriteSetting = SortHandlingMod.FavoriteFilter.ShowAll;
                        UpgradeSortingPlugin.CurrentFilters.FilterStats = false;
                        UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Clear();

                        if (filterPanel != null)
                        {
                            foreach (Transform child in filterPanel.transform)
                            {
                                if (child.name.StartsWith("FilterButton_"))
                                {
                                    var buttonImage = child.GetComponent<Image>();
                                    if (buttonImage != null &&
                                        child.GetComponentInChildren<TextMeshProUGUI>()?.text != null)
                                    {
                                        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                                    }
                                }
                            }
                        }

                        if (filterPanel != null)
                        {
                            GameObject showAllBtn = null, onlyFavBtn = null, hideFavBtn = null;
                            foreach (Transform child in filterPanel.transform)
                            {
                                if (child.name.StartsWith("FilterButton_"))
                                {
                                    var textComponent = child.GetComponentInChildren<TextMeshProUGUI>();
                                    if (textComponent != null)
                                    {
                                        string text = textComponent.text;
                                        if (text == "Show All") showAllBtn = child.gameObject;
                                        else if (text == "Only Favorite") onlyFavBtn = child.gameObject;
                                        else if (text == "Hide Favorite") hideFavBtn = child.gameObject;
                                    }
                                }
                            }

                            if (showAllBtn != null)
                                showAllBtn.GetComponent<Image>().color = new Color(0.5f, 0.7f, 0.5f, 1f);
                            if (onlyFavBtn != null)
                                onlyFavBtn.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
                            if (hideFavBtn != null)
                                hideFavBtn.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
                        }

                        RefreshUpgrades();
                        RegenerateStatFilters();

                    });

                    clearButtonGo.SetActive(true);
                    button.interactable = true;

                }
                catch (Exception e)
                {
                }
            }

            private void FixRarityButtonTextPosition()
            {
                try
                {
                    var rarityButtons = filterPanel.GetComponentsInChildren<Button>()
                        .Where(b => b.name.StartsWith("FilterButton_") &&
                                    (b.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains("Standard") == true ||
                                     b.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains("Rare") == true ||
                                     b.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains("Epic") == true ||
                                     b.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains("Exotic") == true ||
                                     b.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains("Oddity") == true))
                        .ToList();

                    foreach (var button in rarityButtons)
                    {
                        var textComponents = button.GetComponentsInChildren<TextMeshProUGUI>();
                        if (textComponents.Length > 0)
                        {
                            var textRect = textComponents[0].GetComponent<RectTransform>();
                            textRect.anchorMin = new Vector2(0, 0);
                            textRect.anchorMax = new Vector2(1, 1);
                            textRect.offsetMin = new Vector2(5, 2);
                            textRect.offsetMax = new Vector2(-5, -2);
                            textRect.localPosition = Vector3.zero;
                        }
                    }

                }
                catch (Exception e)
                {
                }
            }
        }
    }


public enum CustomSortingMethod
    {
        Name,
        Rarity,
        DamageType,
        RecentlyCollected,
        RecentlyUnequipped,
        Stat,
        Favorite,
        Modifier
    }

    public struct FilterSettings
    {
        public List<Rarity> HiddenRarities;
        public bool FilterStats;
        public List<string> StatIncludeList;
        public SortHandlingMod.FavoriteFilter FavoriteSetting;
    }

    public enum FavoriteFilter
    {
        ShowAll,
        ShowOnlyFavorited,
        HideFavorited
    }



    [HarmonyPatch(typeof(GearDetailsWindow))]
    public static class GearDetailsWindowpatches
    {
        private static readonly Comparison<GearUpgradeUI> CompareUpgradesByFullName = (a, b) =>
        {
            if (a.gameObject.activeSelf && !b.gameObject.activeSelf) return -1;
            if (!a.gameObject.activeSelf && b.gameObject.activeSelf) return 1;

            return a.Upgrade.Upgrade.Name.CompareTo(b.Upgrade.Upgrade.Name);
        };

        private static readonly Comparison<GearUpgradeUI> CompareUpgradesByCustomRarity = (a, b) =>
        {
            if (a.gameObject.activeSelf && !b.gameObject.activeSelf) return -1;
            if (!a.gameObject.activeSelf && b.gameObject.activeSelf) return 1;

            UpgradeSortingPlugin.RarityOrder.TryGetValue(a.Upgrade.Upgrade.Rarity, out var orderA);
            UpgradeSortingPlugin.RarityOrder.TryGetValue(b.Upgrade.Upgrade.Rarity, out var orderB);
            int rarityCompare = orderB.CompareTo(orderA);
            return (rarityCompare != 0) ? rarityCompare : CompareUpgradesByFullName(a, b);
        };

        private static readonly Comparison<GearUpgradeUI> CompareUpgradesByRecentlyAcquired = (a, b) =>
        {
            if (a.gameObject.activeSelf && !b.gameObject.activeSelf) return -1;
            if (!a.gameObject.activeSelf && b.gameObject.activeSelf) return 1;

            return -a.Upgrade.TimeUnlocked.CompareTo(b.Upgrade.TimeUnlocked);
        };

        public static Comparison<GearUpgradeUI> GetCustomComparison(CustomSortingMethod method)
        {
            switch (method)
            {
                case CustomSortingMethod.Name: return CompareUpgradesByFullName;
                case CustomSortingMethod.Rarity: return CompareUpgradesByCustomRarity;
                case CustomSortingMethod.RecentlyCollected: return CompareUpgradesByRecentlyAcquired;
                default: return CompareUpgradesByFullName;
            }
        }

        public static Comparison<GearUpgradeUI> CreateChainedComparison(List<CustomSortingMethod> priorities)
        {
            return (a, b) =>
            {
                foreach (var method in priorities)
                {
                    int result = GetCustomComparison(method)(a, b);
                    if (result != 0) return result;
                }
                return 0;
            };
        }

        public static List<CustomSortingMethod> GetDynamicPrioritiesForMethod(GearDetailsWindow.SortingMethod method)
        {
            switch (method)
            {
                case GearDetailsWindow.SortingMethod.Name:
                    return new List<CustomSortingMethod> { CustomSortingMethod.Name, CustomSortingMethod.Rarity };
                case GearDetailsWindow.SortingMethod.Rarity:
                    return new List<CustomSortingMethod> { CustomSortingMethod.Rarity, CustomSortingMethod.Name };
                case GearDetailsWindow.SortingMethod.RecentlyCollected:
                    return new List<CustomSortingMethod> { CustomSortingMethod.RecentlyCollected, CustomSortingMethod.Rarity, CustomSortingMethod.Name };
                default:
                    return new List<CustomSortingMethod> { CustomSortingMethod.Rarity, CustomSortingMethod.Name };
            }
        }

        public static List<GearUpgradeUI> ApplyFilters(List<GearUpgradeUI> upgradeUIs, FilterSettings filters)
        {
            var filtered = new List<GearUpgradeUI>(upgradeUIs.Count);
            foreach (var ui in upgradeUIs)
            {
                if (filters.FavoriteSetting == SortHandlingMod.FavoriteFilter.ShowOnlyFavorited && !ui.Upgrade.Favorite) continue;
                if (filters.FavoriteSetting == SortHandlingMod.FavoriteFilter.HideFavorited && ui.Upgrade.Favorite) continue;
                filtered.Add(ui);
            }
            return filtered;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(GearDetailsWindow.OnOpen))]
        private static void OnOpen(GearDetailsWindow __instance)
        {
            try
            {
                var currentSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
                bool currentSkinMode = false;
                if (currentSkinModeField != null)
                {
                    currentSkinMode = (bool)currentSkinModeField.GetValue(__instance);

                    if (UpgradeSortingPlugin.PreviousSkinMode.HasValue && UpgradeSortingPlugin.PreviousSkinMode.Value != currentSkinMode)
                    {

                        if (UpgradeSortingPlugin.FilterPanel != null)
                        {
                            UpgradeSortingPlugin.FilterPanel.RegenerateStatFilters();
                        }
                    }

                    UpgradeSortingPlugin.PreviousSkinMode = currentSkinMode;
                }
            }
            catch (Exception e)
            {
            }

            UpgradeSortingPlugin.FilterPanel = new FilterPanelUI();


        }


        private static void CreateFilterButton(GearDetailsWindow window)
        {

            try
            {
                var gearDetails = GameObject.Find("Gear Details");
                if (gearDetails == null)
                {
                    return;
                }

                var filterButtonGo = new GameObject("Mycopunk_FilterButton");
                filterButtonGo.transform.SetParent(gearDetails.transform, false);

                var filterImage = filterButtonGo.AddComponent<Image>();
                filterImage.color = new Color(0.8f, 0.2f, 0.9f, 1f);

                var filterButton = filterButtonGo.AddComponent<Button>();
                var filterText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
                filterText.transform.SetParent(filterButtonGo.transform, false);
                filterText.text = "FILTER";
                filterText.fontSize = 18;
                filterText.color = Color.white;
                filterText.alignment = TextAlignmentOptions.Center;

                var filterRect = filterButtonGo.GetComponent<RectTransform>();
                filterRect.anchorMin = new Vector2(0.5f, 1f);
                filterRect.anchorMax = new Vector2(0.5f, 1f);
                filterRect.pivot = new Vector2(0.5f, 1f);

                filterRect.anchoredPosition = new Vector2(50, -300);
                filterRect.sizeDelta = new Vector2(150, 45);

                var textRect = filterText.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                filterButtonGo.SetActive(true);
                filterButton.interactable = true;
                filterButton.transition = Selectable.Transition.ColorTint;
                filterButton.colors = ColorBlock.defaultColorBlock;

                filterButton.onClick.RemoveAllListeners();
                filterButton.onClick.AddListener(() => {
                    UpgradeSortingPlugin.FilterPanel?.Toggle();
                });



                LayoutRebuilder.ForceRebuildLayoutImmediate(filterRect);
                        Canvas.ForceUpdateCanvases();



            }
            catch (Exception e)
            {
            }
        }
        }
    

    [HarmonyPatch(typeof(GearDetailsWindow), "SortUpgrades", new Type[] { typeof(GearDetailsWindow.SortingMethod), typeof(bool) })]
    public static class SortUpgradesPatch
    {
        private static bool? lastSkinMode = null;

        private static string CleanNameForSorting(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(name, @"<[^>]*>", string.Empty);
        }

        public static int PropertyAwareNameComparison(GearUpgradeUI a, GearUpgradeUI b)
        {
            if (a.gameObject.activeSelf && !b.gameObject.activeSelf) return -1;
            if (!a.gameObject.activeSelf && b.gameObject.activeSelf) return 1;

            return a.Upgrade.Upgrade.Name.CompareTo(b.Upgrade.Upgrade.Name);
        }

        [HarmonyPrefix]
        public static bool Prefix(GearDetailsWindow __instance, GearDetailsWindow.SortingMethod method, bool resetScroll)
        {
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(GearDetailsWindow __instance, GearDetailsWindow.SortingMethod method, bool resetScroll)
        {
            var inSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
            bool currentSkinMode = false;
            if (inSkinModeField != null)
            {
                try
                {
                    currentSkinMode = (bool)inSkinModeField.GetValue(__instance);
                }
                catch { }
            }

            bool isMenuSwitch = lastSkinMode.HasValue && lastSkinMode.Value != currentSkinMode;
            lastSkinMode = currentSkinMode;

            if (!isMenuSwitch && HasActiveFilters())
            {
                try
                {
                    ApplyFiltersToSortedList(__instance);
                }
                catch (Exception e)
                {
                }
            }

            if (isMenuSwitch)
            {
            }
        }

        private static bool HasActiveFilters()
        {
            return UpgradeSortingPlugin.CurrentFilters.HiddenRarities.Any() ||
                   UpgradeSortingPlugin.CurrentFilters.FavoriteSetting != SortHandlingMod.FavoriteFilter.ShowAll ||
                   (UpgradeSortingPlugin.CurrentFilters.FilterStats && UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Any());
        }

        private static void ApplyFiltersToSortedList(GearDetailsWindow window)
        {
            var upgradeUIsField = AccessTools.Field(typeof(GearDetailsWindow), "upgradeUIs") ??
                                  AccessTools.Field(typeof(GearDetailsWindow), "<upgradeUIs>k__BackingField") ??
                                  AccessTools.Field(typeof(GearDetailsWindow), "upgrades");

            var upgradeUICountField = AccessTools.Field(typeof(GearDetailsWindow), "upgradeUICount");

            if (upgradeUIsField == null || upgradeUICountField == null) return;

            var upgradeUIs = (List<GearUpgradeUI>)upgradeUIsField.GetValue(window);
            if (upgradeUIs == null || upgradeUIs.Count == 0) return;

            int visibleCount = 0;

            foreach (var ui in upgradeUIs)
            {
                bool show = true;

                if (UpgradeSortingPlugin.CurrentFilters.HiddenRarities.Any())
                {
                    show &= !UpgradeSortingPlugin.CurrentFilters.HiddenRarities.Contains(ui.Upgrade.Upgrade.Rarity);
                }

                                switch (UpgradeSortingPlugin.CurrentFilters.FavoriteSetting)
                                {
                                    case SortHandlingMod.FavoriteFilter.ShowOnlyFavorited:
                                        show &= ui.Upgrade.Favorite;
                                        break;
                                    case SortHandlingMod.FavoriteFilter.HideFavorited:
                                        show &= !ui.Upgrade.Favorite;
                                        break;
                                }

                                if (UpgradeSortingPlugin.CurrentFilters.FilterStats && UpgradeSortingPlugin.CurrentFilters.StatIncludeList.Any())
                                {
                                    bool hasAllProperties = true;
                                    foreach (var requiredProperty in UpgradeSortingPlugin.CurrentFilters.StatIncludeList)
                                    {
                                        bool propertyFound = false;
                                        var properties = ui.Upgrade.Upgrade.GetProperties();
                                        while (properties.MoveNext())
                                        {
                                            var property = properties.Current;
                                        string propName = property.GetType().Name;
                                        if (propName.StartsWith("UpgradeProperty_"))
                                        {
                                            propName = propName.Substring("UpgradeProperty_".Length);
                                        }
                                        else if (propName.StartsWith("SkinUpgradeProperty_"))
                                        {
                                            propName = propName.Substring("SkinUpgradeProperty_".Length);
                                        }
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

                    try
                    {
                        upgradeUICountField.SetValue(window, visibleCount);
                    } catch { }

                }
            }
