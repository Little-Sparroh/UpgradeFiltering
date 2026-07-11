using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Logging;


public static class PriorityPatches
{
    static PriorityPatches()
    {
        priorityOrder = LoadPriorityOrder();
    }

    private static List<PriorityCriteria> priorityOrder;
    private static bool performingPrioritySort;
    public static GearDetailsWindow currentWindow;

    private static bool isWindowOpen = false;
    private static HashSet<PriorityCriteria> usedCriteria = new();

    public static bool IsWindowOpen => isWindowOpen;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GearDetailsWindow), "OnOpen")]
    public static void OnOpen_Postfix(GearDetailsWindow __instance)
    {
        isWindowOpen = true;
        currentWindow = __instance;

    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GearDetailsWindow), "OnCloseCallback")]
    public static void OnCloseCallback_Postfix()
    {
        performingPrioritySort = false;
        isWindowOpen = false;

    }

    public static bool SortUpgrades_Prefix(GearDetailsWindow __instance, int i)
    {
        try
        {
            if (performingPrioritySort)
            {
                try
                {
                    priorityOrder = LoadPriorityOrder();
                    performingPrioritySort = false;
                    usedCriteria.Clear();
                    List<GearUpgradeUI> upgradeUIs = Traverse.Create(__instance).Field<List<GearUpgradeUI>>("upgradeUIs").Value;
                    upgradeUIs.Sort(GetPriorityComparison(priorityOrder));
                    Traverse.Create(__instance).Method("UpdateUpgradeOrder").GetValue();
                    Traverse.Create(__instance).Method("SetUpgradeListScroll", 1f).GetValue();
                    return false;
                }
                catch (Exception ex)
                {
                    SparrohPlugin.Logger.LogError($"Failed to perform priority sort: {ex.Message}");
                    performingPrioritySort = false;
                    return true;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Critical error in SortUpgrades_Prefix: {ex.Message}");
            return true;
        }
    }

    public static void Patch(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(GearDetailsWindow), "SortUpgrades", new Type[] { typeof(int) }), prefix: new HarmonyMethod(typeof(PriorityPatches), "SortUpgrades_Prefix"));
    }

    public static void TriggerPrioritySort()
    {
        currentWindow = Menu.Instance.WindowSystem.GetTop() as GearDetailsWindow;
        if (currentWindow != null)
        {
            performingPrioritySort = true;
            currentWindow.SortUpgrades(0);
        }
    }

    public static Comparison<GearUpgradeUI> GetPriorityComparison(List<PriorityCriteria> order)
    {
        return (a, b) =>
        {
            if (a == null) return 1;
            if (b == null) return -1;
            if (a.Upgrade == null) return 1;
            if (b.Upgrade == null) return -1;
            int cmpActive = (a.gameObject.activeSelf ? 0 : 1).CompareTo(b.gameObject.activeSelf ? 0 : 1);
            if (cmpActive != 0) return cmpActive;
            foreach (var criteria in order)
            {
                int cmp = CompareByCriteria(a, b, criteria);
                if (cmp != 0)
                {
                    usedCriteria.Add(criteria);
                    return cmp;
                }
            }
            string nameA = a.Upgrade.Upgrade.GetInstanceName(a.Upgrade.Seed);
            string nameB = b.Upgrade.Upgrade.GetInstanceName(b.Upgrade.Seed);
            int finalCmp = nameA.CompareTo(nameB);
            if (finalCmp != 0)
            {
            }
            return finalCmp;
        };
    }

    public static int CompareByCriteria(GearUpgradeUI a, GearUpgradeUI b, PriorityCriteria criteria)
    {
        int cmp = 0;
        switch (criteria)
        {
            case PriorityCriteria.Favorited:
                cmp = - (a.Upgrade.Favorite ? 1 : 0).CompareTo(b.Upgrade.Favorite ? 1 : 0);
                return cmp;
            case PriorityCriteria.NotFavorited:
                cmp = - (a.Upgrade.Favorite ? 0 : 1).CompareTo(b.Upgrade.Favorite ? 0 : 1);
                return cmp;
            case PriorityCriteria.Unlocked:
                cmp = - a.Upgrade.IsUnlocked.CompareTo(b.Upgrade.IsUnlocked);
                return cmp;
            case PriorityCriteria.Locked:
                cmp = - ((a.Upgrade.IsUnlocked ? 0 : 1).CompareTo(b.Upgrade.IsUnlocked ? 0 : 1));
                return cmp;
            case PriorityCriteria.Turbocharged:
                bool isTurboA = Traverse.Create(a.Upgrade).Property<bool>("IsTurbocharged").Value;
                bool isTurboB = Traverse.Create(b.Upgrade).Property<bool>("IsTurbocharged").Value;
                cmp = - (isTurboA ? 1 : 0).CompareTo(isTurboB ? 1 : 0);
                return cmp;
            case PriorityCriteria.Trashed:
                try
                {
                    bool isTrashedA = Traverse.Create(a.Upgrade).Property<bool>("IsTrashed").Value;
                    bool isTrashedB = Traverse.Create(b.Upgrade).Property<bool>("IsTrashed").Value;
                    cmp = (isTrashedA ? 1 : 0).CompareTo(isTrashedB ? 1 : 0);
                    return cmp;
                }
                catch
                {
                    SparrohPlugin.Logger.LogInfo($"Checked {criteria} for {a.Upgrade.Upgrade.Name} vs {b.Upgrade.Upgrade.Name}: cmp=0 (Property not found)");
                    return 0;
                }
            case PriorityCriteria.NotTurbocharged:
                bool notTurboA = !Traverse.Create(a.Upgrade).Property<bool>("IsTurbocharged").Value;
                bool notTurboB = !Traverse.Create(b.Upgrade).Property<bool>("IsTurbocharged").Value;
                cmp = - (notTurboA ? 1 : 0).CompareTo(notTurboB ? 1 : 0);
                return cmp;
            case PriorityCriteria.NotTrashed:
                try
                {
                    bool notTrashedA = !Traverse.Create(a.Upgrade).Property<bool>("IsTrashed").Value;
                    bool notTrashedB = !Traverse.Create(b.Upgrade).Property<bool>("IsTrashed").Value;
                    cmp = - (notTrashedA ? 1 : 0).CompareTo(notTrashedB ? 1 : 0);
                    return cmp;
                }
                catch
                {
                    SparrohPlugin.Logger.LogInfo($"Checked {criteria} for {a.Upgrade.Upgrade.Name} vs {b.Upgrade.Upgrade.Name}: cmp=0 (Property not found)");
                    return 0;
                }
            case PriorityCriteria.RecentlyAcquired:
                cmp = - a.Upgrade.TimeUnlocked.CompareTo(b.Upgrade.TimeUnlocked);
                return cmp;
            case PriorityCriteria.RecentlyUsed:
                cmp = - a.Upgrade.TimeUnequipped.CompareTo(b.Upgrade.TimeUnequipped);
                return cmp;
            case PriorityCriteria.InstanceName:
                string nameA = a.Upgrade.Upgrade.GetInstanceName(a.Upgrade.Seed);
                if (string.IsNullOrEmpty(nameA)) nameA = a.Upgrade.Upgrade.Name;
                string nameB = b.Upgrade.Upgrade.GetInstanceName(b.Upgrade.Seed);
                if (string.IsNullOrEmpty(nameB)) nameB = b.Upgrade.Upgrade.Name;
                cmp = nameA.CompareTo(nameB);
                return cmp;
            case PriorityCriteria.Oddity:
                cmp = - ((a.Upgrade.Upgrade.Rarity == Rarity.Oddity ? 1 : 0).CompareTo(b.Upgrade.Upgrade.Rarity == Rarity.Oddity ? 1 : 0));
                return cmp;
            case PriorityCriteria.Exotic:
                cmp = - ((a.Upgrade.Upgrade.Rarity == Rarity.Exotic ? 1 : 0).CompareTo(b.Upgrade.Upgrade.Rarity == Rarity.Exotic ? 1 : 0));
                return cmp;
            case PriorityCriteria.Epic:
                cmp = - ((a.Upgrade.Upgrade.Rarity == Rarity.Epic ? 1 : 0).CompareTo(b.Upgrade.Upgrade.Rarity == Rarity.Epic ? 1 : 0));
                return cmp;
            case PriorityCriteria.Rare:
                cmp = - ((a.Upgrade.Upgrade.Rarity == Rarity.Rare ? 1 : 0).CompareTo(b.Upgrade.Upgrade.Rarity == Rarity.Rare ? 1 : 0));
                return cmp;
            case PriorityCriteria.Standard:
                cmp = - ((a.Upgrade.Upgrade.Rarity == Rarity.Standard ? 1 : 0).CompareTo(b.Upgrade.Upgrade.Rarity == Rarity.Standard ? 1 : 0));
                return cmp;
            default:
                return 0;
        }
    }

    public static List<PriorityCriteria> LoadPriorityOrder()
    {
        try
        {
            if (PlayerOptions.TryGetConfig<string>("SortPriority.Order", out var json))
            {
                try
                {
                    var data = PriorityData.FromJson(json);
                    List<PriorityCriteria> list = new List<PriorityCriteria>();
                    foreach (int i in data.order)
                    {
                        if (Enum.IsDefined(typeof(PriorityCriteria), i))
                            list.Add((PriorityCriteria)i);
                    }
                    if (list.Count > 0)
                    {
                        return list;
                    }
                }
                catch (Exception ex)
                {
                    SparrohPlugin.Logger.LogWarning($"Failed to parse priority order from config, using defaults: {ex.Message}");
                }
            }
            var defaultOrder = new PriorityData().order.ConvertAll(i => (PriorityCriteria)i);
            return defaultOrder;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Critical error loading priority order: {ex.Message}");
            return new PriorityData().order.ConvertAll(i => (PriorityCriteria)i);
        }
    }

    public static void SavePriorityOrder(List<PriorityCriteria> order)
    {
        try
        {
            var data = new PriorityData { order = order.ConvertAll(c => (int)c) };
            string json = data.ToJson();
            PlayerOptions.SetConfig("SortPriority.Order", json);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Failed to save priority order: {ex.Message}");
        }
    }
}
