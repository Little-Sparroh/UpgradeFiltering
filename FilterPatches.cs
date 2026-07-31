using System;
using HarmonyLib;

[HarmonyPatch(typeof(GearDetailsWindow))]
public static class GearDetailsWindowPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GearDetailsWindow.OnOpen))]
    private static void OnOpen(GearDetailsWindow __instance)
    {
        try
        {
            var currentSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
            if (currentSkinModeField != null)
            {
                var currentSkinMode = (bool)currentSkinModeField.GetValue(__instance);

                if (FilterState.PreviousSkinMode.HasValue &&
                    FilterState.PreviousSkinMode.Value != currentSkinMode)
                    if (FilterState.FilterPanel != null)
                        FilterState.FilterPanel.RegenerateStatFilters();

                FilterState.PreviousSkinMode = currentSkinMode;
            }
        }
        catch
        {
        }


        if (FilterState.FilterPanel == null)
            FilterState.FilterPanel = new FilterPanelUI();
    }


    [HarmonyPrefix]
    [HarmonyPatch(nameof(GearDetailsWindow.SwitchUpgradeView))]
    private static void SwitchUpgradeView_Prefix()
    {
        ListRebuildReapply.BeginViewSwitch();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(GearDetailsWindow.SwitchUpgradeView))]
    private static void SwitchUpgradeView_Postfix(GearDetailsWindow __instance)
    {
        try
        {
            ListRebuildReapply.After(__instance, "SwitchUpgradeView");
        }
        finally
        {
            ListRebuildReapply.EndViewSwitch();
        }
    }
}

[HarmonyPatch(typeof(GearDetailsWindow), "SetupUpgrades", typeof(IUpgradable), typeof(bool), typeof(bool))]
public static class SetupUpgradesPatch
{
    [HarmonyPostfix]
    public static void Postfix(GearDetailsWindow __instance, bool skins)
    {
        if (ListRebuildReapply.SuppressNestedReapply)
            return;

        ListRebuildReapply.After(__instance, $"SetupUpgrades(skins={skins})");
    }
}

[HarmonyPatch(typeof(GearDetailsWindow), "SortUpgrades", typeof(GearDetailsWindow.SortingMethod), typeof(bool))]
public static class SortUpgradesMethodPatch
{
    private static bool? lastSkinMode;

    [HarmonyPostfix]
    public static void Postfix(GearDetailsWindow __instance, GearDetailsWindow.SortingMethod method,
        bool resetScroll)
    {
        if (ListRebuildReapply.SuppressNestedReapply)
            return;


        if (PriorityPatches.PrioritySortActive)
            return;

        var inSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
        var currentSkinMode = false;
        if (inSkinModeField != null)
            try
            {
                currentSkinMode = (bool)inSkinModeField.GetValue(__instance);
            }
            catch
            {
            }

        var isMenuSwitch = lastSkinMode.HasValue && lastSkinMode.Value != currentSkinMode;
        lastSkinMode = currentSkinMode;

        if (isMenuSwitch)
            return;

        try
        {
            FilterState.ApplyToWindow(__instance);
        }
        catch
        {
        }
    }
}

internal static class ListRebuildReapply
{
    public static bool SuppressNestedReapply { get; private set; }

    public static void After(GearDetailsWindow window, string reason)
    {
        if (window == null) return;

        try
        {
            PriorityPatches.currentWindow = window;
            PriorityPatches.CancelDeferredLayout();

            var list = FilterState.GetUpgradeUIs();
            var count = FilterState.GetUpgradeUICount(window);
            if (list == null)
            {
                UpgradeFilteringPlugin.Logger.LogWarning($"List rebuild ({reason}): upgradeUIs null");
                return;
            }

            if (count <= 0 || count > list.Count)
                count = list.Count;


            PriorityPatches.ClearStalePoolSlots(list, count);


            if (!PriorityPatches.LogUniqueInstanceIds($"BEFORE repair ({reason})", list, count))
                if (!PriorityPatches.TryRepairDuplicateUiRefs(window, list, count))
                {
                    UpgradeFilteringPlugin.Logger.LogError(
                        $"List rebuild ({reason}): pool repair failed — reopen gear window.");
                    return;
                }


            PriorityPatches.ActivateAllLiveSlots(list, count);

            if (PriorityPatches.PrioritySortActive)
            {
                PriorityPatches.ApplyPrioritySort(window, false, true);
                UpgradeFilteringPlugin.Logger.LogInfo(
                    $"List rebuild re-apply ({reason}): visual priority + filters, isGrid={PriorityPatches.GetIsGridView(window)}");
            }
            else if (FilterState.HasActiveFilters())
            {
                FilterState.ApplyToWindow(window);
                UpgradeFilteringPlugin.Logger.LogInfo(
                    $"List rebuild re-apply ({reason}): filters only, isGrid={PriorityPatches.GetIsGridView(window)}");
            }
            else
            {
                PriorityPatches.ForceLayoutVisibleOnly(window, list, count);
                UpgradeFilteringPlugin.Logger.LogInfo(
                    $"List rebuild re-apply ({reason}): layout only, isGrid={PriorityPatches.GetIsGridView(window)}");
            }
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogError(
                $"List rebuild re-apply failed ({reason}): {ex.Message}\n{ex.StackTrace}");
        }
    }

    public static void BeginViewSwitch()
    {
        SuppressNestedReapply = true;
        PriorityPatches.CancelDeferredLayout();
    }

    public static void EndViewSwitch()
    {
        SuppressNestedReapply = false;
    }
}