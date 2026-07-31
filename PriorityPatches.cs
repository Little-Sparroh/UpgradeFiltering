using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PriorityPatches
{
    private static List<PriorityCriteria> priorityOrder = new();
    public static GearDetailsWindow currentWindow;

    private static bool? _batchScrappingPresent;
    private static MethodInfo _isTrashMarkedMethod;

    private static Coroutine _deferredLayoutCoroutine;
    private static bool _deferredLayoutCancelled;

    private static FieldInfo _isGridViewField;
    private static FieldInfo _upgradeListParentField;
    private static bool _layoutLookupsDone;

    static PriorityPatches()
    {
        priorityOrder = LoadPriorityOrder();
        try
        {
            PrioritySortActive = PlayerOptions.TryGetConfig<string>("SortPriority.Order", out var json)
                                 && !string.IsNullOrEmpty(json);
        }
        catch
        {
            PrioritySortActive = false;
        }
    }


    public static bool PrioritySortActive { get; private set; }

    public static bool IsWindowOpen { get; private set; }


    public static bool IsBatchScrappingPresent()
    {
        if (_batchScrappingPresent.HasValue)
            return _batchScrappingPresent.Value;

        try
        {
            foreach (var plugin in Chainloader.PluginInfos.Values)
                if (plugin?.Metadata?.GUID == "sparroh.batchscrapping")
                {
                    _batchScrappingPresent = true;
                    CacheTrashApi();
                    return true;
                }
        }
        catch
        {
        }

        try
        {
            var t = AccessTools.TypeByName("ScrapHandlingMod");
            if (t != null)
            {
                _batchScrappingPresent = true;
                CacheTrashApi();
                return true;
            }
        }
        catch
        {
        }

        _batchScrappingPresent = false;
        return false;
    }

    private static void CacheTrashApi()
    {
        try
        {
            var t = AccessTools.TypeByName("ScrapHandlingMod");
            _isTrashMarkedMethod = t != null
                ? AccessTools.Method(t, "IsTrashMarked", new[] { typeof(UpgradeInstance) })
                : null;
        }
        catch
        {
            _isTrashMarkedMethod = null;
        }
    }

    public static bool IsTrashMarked(UpgradeInstance instance)
    {
        if (instance == null || !IsBatchScrappingPresent())
            return false;

        if (_isTrashMarkedMethod != null)
            try
            {
                return (bool)_isTrashMarkedMethod.Invoke(null, new object[] { instance });
            }
            catch
            {
            }


        try
        {
            var flagsField = AccessTools.Field(typeof(UpgradeInstance), "flags");
            if (flagsField == null) return false;
            var flags = Convert.ToByte(flagsField.GetValue(instance));
            return (flags & 0x20) != 0;
        }
        catch
        {
            return false;
        }
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(GearDetailsWindow), "OnOpen")]
    public static void OnOpen_Postfix(GearDetailsWindow __instance)
    {
        IsWindowOpen = true;
        currentWindow = __instance;

        if (PrioritySortActive)
            try
            {
                ApplyPrioritySort(__instance, priorityOrder, false);
            }
            catch (Exception ex)
            {
                UpgradeFilteringPlugin.Logger.LogWarning(
                    $"Priority sort on open failed: {ex.Message}");
            }
        else
            try
            {
                FilterState.ApplyToWindow(__instance);
            }
            catch
            {
            }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GearDetailsWindow), "OnCloseCallback")]
    public static void OnCloseCallback_Postfix()
    {
        IsWindowOpen = false;
        currentWindow = null;
    }


    public static bool SortUpgradesInt_Prefix(int i)
    {
        if (PrioritySortActive)
        {
            PrioritySortActive = false;
            UpgradeFilteringPlugin.Logger.LogInfo(
                $"Priority sort disabled (vanilla sort button {i}).");
        }

        return true;
    }


    public static bool SortUpgradesMethod_Prefix(GearDetailsWindow __instance,
        GearDetailsWindow.SortingMethod method, bool resetScroll)
    {
        if (!PrioritySortActive)
            return true;


        if (ListRebuildReapply.SuppressNestedReapply)
            return true;

        try
        {
            ApplyPrioritySort(__instance, priorityOrder, resetScroll);
            return false;
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogError(
                $"Priority sort prefix failed: {ex.Message}\n{ex.StackTrace}");
            return true;
        }
    }

    public static bool SortUpgradesMethodPublic_Prefix(GearDetailsWindow __instance,
        GearDetailsWindow.SortingMethod method)
    {
        if (!PrioritySortActive)
            return true;

        if (ListRebuildReapply.SuppressNestedReapply)
            return true;

        try
        {
            ApplyPrioritySort(__instance, priorityOrder, true);
            return false;
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogError($"Priority sort public prefix failed: {ex.Message}");
            return true;
        }
    }


    public static void Patch(Harmony harmony)
    {
        var sortInt = AccessTools.Method(typeof(GearDetailsWindow), "SortUpgrades", new[] { typeof(int) });
        if (sortInt != null)
            harmony.Patch(sortInt,
                new HarmonyMethod(typeof(PriorityPatches), nameof(SortUpgradesInt_Prefix)));
        else
            UpgradeFilteringPlugin.Logger.LogWarning("Could not find SortUpgrades(int).");

        var sortMethod = AccessTools.Method(typeof(GearDetailsWindow), "SortUpgrades",
            new[] { typeof(GearDetailsWindow.SortingMethod), typeof(bool) });
        if (sortMethod != null)
        {
            harmony.Patch(sortMethod,
                new HarmonyMethod(typeof(PriorityPatches), nameof(SortUpgradesMethod_Prefix)));
            UpgradeFilteringPlugin.Logger.LogInfo("Patched SortUpgrades(SortingMethod, bool) for sticky priority.");
        }
        else
        {
            var sortMethodPublic = AccessTools.Method(typeof(GearDetailsWindow), "SortUpgrades",
                new[] { typeof(GearDetailsWindow.SortingMethod) });
            if (sortMethodPublic != null)
                harmony.Patch(sortMethodPublic,
                    new HarmonyMethod(typeof(PriorityPatches), nameof(SortUpgradesMethodPublic_Prefix)));
            else
                UpgradeFilteringPlugin.Logger.LogWarning(
                    "Could not find SortUpgrades(SortingMethod) — sticky priority may not hook vanilla sorts.");
        }
    }


    public static void TriggerPrioritySort(List<PriorityCriteria> order = null)
    {
        if (order != null && order.Count > 0)
            priorityOrder = new List<PriorityCriteria>(order);
        else
            priorityOrder = LoadPriorityOrder();

        PrioritySortActive = true;

        var window = ResolveWindow();
        if (window == null)
        {
            UpgradeFilteringPlugin.Logger.LogWarning(
                "Priority sort: no GearDetailsWindow found — order saved; will apply next open.");
            return;
        }

        currentWindow = window;
        ApplyPrioritySort(window, priorityOrder, true);
    }

    public static GearDetailsWindow ResolveWindow()
    {
        if (currentWindow != null)
            try
            {
                if (currentWindow.gameObject != null && currentWindow.gameObject.activeInHierarchy)
                    return currentWindow;
            }
            catch
            {
                currentWindow = null;
            }

        var w = FilterState.GetOpenWindow();
        if (w != null) return w;

        try
        {
            return Object.FindObjectOfType<GearDetailsWindow>();
        }
        catch
        {
            return null;
        }
    }

    public static void ApplyPrioritySort(GearDetailsWindow window, bool resetScroll = true,
        bool skipDeferred = false)
    {
        ApplyPrioritySort(window, priorityOrder ?? LoadPriorityOrder(), resetScroll, skipDeferred);
    }

    public static void CancelDeferredLayout()
    {
        _deferredLayoutCancelled = true;
        try
        {
            if (_deferredLayoutCoroutine != null && PriorityGUI.Instance != null)
            {
                PriorityGUI.Instance.StopCoroutine(_deferredLayoutCoroutine);
                _deferredLayoutCoroutine = null;
            }
        }
        catch
        {
        }
    }

    public static void ApplyPrioritySort(GearDetailsWindow window, List<PriorityCriteria> order,
        bool resetScroll = true, bool skipDeferred = false)
    {
        if (window == null) return;

        PrioritySortActive = true;
        if (order != null && order.Count > 0)
            priorityOrder = new List<PriorityCriteria>(order);

        var effectiveOrder = FilterOrderForAvailableMods(priorityOrder);

        var upgradeUIs = FilterState.GetUpgradeUIs();
        if (upgradeUIs == null || upgradeUIs.Count == 0)
        {
            UpgradeFilteringPlugin.Logger.LogWarning("Priority sort: upgradeUIs empty.");
            return;
        }

        var count = FilterState.GetUpgradeUICount(window);
        if (count <= 0 || count > upgradeUIs.Count)
            count = upgradeUIs.Count;

        UpgradeFilteringPlugin.Logger.LogInfo(
            $"Sort begin (visual-only): window={window.name}, listCount={upgradeUIs.Count}, uiCount={count}, " +
            $"criteria=[{string.Join(" > ", effectiveOrder)}]");


        ActivateAllLiveSlots(upgradeUIs, count);

        if (!LogUniqueInstanceIds("BEFORE visual sort", upgradeUIs, count))
            if (!TryRepairDuplicateUiRefs(window, upgradeUIs, count))
            {
                UpgradeFilteringPlugin.Logger.LogError(
                    "Pool corrupt and repair failed — skip visual reorder; reopen gear window.");
                FilterState.ApplyVisibilityOnly(window);
                return;
            }


        FilterState.ApplyVisibilityOnly(window);
        ApplyVisualPriorityOrder(window, upgradeUIs, count, effectiveOrder);

        LogRowSnapshot("AFTER visual priority+filter", upgradeUIs, count);

        if (resetScroll)
            try
            {
                const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                typeof(GearDetailsWindow).GetMethod("SetUpgradeListScroll", bf, null, new[] { typeof(float) }, null)
                    ?.Invoke(window, new object[] { 1f });
            }
            catch (Exception ex)
            {
                UpgradeFilteringPlugin.Logger.LogWarning($"SetUpgradeListScroll failed: {ex.Message}");
            }

        try
        {
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            typeof(GearDetailsWindow).GetMethod("DisableSortButtonContainer", bf, null, Type.EmptyTypes, null)
                ?.Invoke(window, null);
        }
        catch
        {
        }
    }


    public static void ClearStalePoolSlots(List<GearUpgradeUI> upgradeUIs, int liveCount)
    {
        if (upgradeUIs == null) return;
        liveCount = Mathf.Max(0, liveCount);
        var cleared = 0;
        for (var i = liveCount; i < upgradeUIs.Count; i++)
        {
            var ui = upgradeUIs[i];
            if (ui == null) continue;
            try
            {
                if (ui.gameObject.activeSelf)
                    ui.gameObject.SetActive(false);
                if (ui.Upgrade != null)
                {
                    ui.Upgrade = null;
                    cleared++;
                }
            }
            catch
            {
                try
                {
                    ui.gameObject.SetActive(false);
                }
                catch
                {
                }
            }
        }

        if (cleared > 0)
            UpgradeFilteringPlugin.Logger.LogInfo(
                $"ClearStalePoolSlots: cleared Upgrade on {cleared} slots past liveCount={liveCount}");
    }


    public static bool TryRepairDuplicateUiRefs(GearDetailsWindow window, List<GearUpgradeUI> upgradeUIs,
        int count)
    {
        if (upgradeUIs == null || window == null || count <= 0) return true;
        count = Mathf.Min(count, upgradeUIs.Count);

        var seen = new HashSet<GearUpgradeUI>();
        var dupIndices = new List<int>();
        for (var i = 0; i < count; i++)
        {
            var ui = upgradeUIs[i];
            if (ui == null)
            {
                dupIndices.Add(i);
                continue;
            }

            if (!seen.Add(ui))
                dupIndices.Add(i);
        }

        if (dupIndices.Count == 0)
            return true;

        UpgradeFilteringPlugin.Logger.LogWarning(
            $"Repairing {dupIndices.Count} duplicate/null GearUpgradeUI refs in live pool.");


        var free = new Queue<GearUpgradeUI>();
        for (var i = 0; i < upgradeUIs.Count; i++)
        {
            var ui = upgradeUIs[i];
            if (ui == null) continue;
            if (seen.Contains(ui)) continue;
            free.Enqueue(ui);
        }

        RectTransform parent = null;
        try
        {
            EnsureLayoutLookups();
            parent = _upgradeListParentField?.GetValue(window) as RectTransform;
        }
        catch
        {
        }

        FieldInfo prefabField = null;
        try
        {
            const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            prefabField = typeof(GearDetailsWindow).GetField("upgradeUIPrefab", f);
        }
        catch
        {
        }

        foreach (var idx in dupIndices)
        {
            var old = upgradeUIs[idx];
            var boundUpgrade = old?.Upgrade;

            GearUpgradeUI replacement = null;
            if (free.Count > 0)
                replacement = free.Dequeue();
            else if (prefabField != null && parent != null)
                try
                {
                    var prefab = prefabField.GetValue(window) as GearUpgradeUI;
                    if (prefab != null)
                    {
                        replacement = Object.Instantiate(prefab, parent);
                        upgradeUIs.Add(replacement);
                    }
                }
                catch (Exception ex)
                {
                    UpgradeFilteringPlugin.Logger.LogWarning($"Instantiate repair UI failed: {ex.Message}");
                }

            if (replacement == null)
            {
                UpgradeFilteringPlugin.Logger.LogError($"Could not repair duplicate at index {idx}");
                return false;
            }

            upgradeUIs[idx] = replacement;
            seen.Add(replacement);
            try
            {
                if (boundUpgrade != null)
                {
                    replacement.gameObject.SetActive(true);
                    replacement.SetUpgrade(boundUpgrade);
                    replacement.EnableGridView(GetIsGridView(window));
                }
                else
                {
                    replacement.gameObject.SetActive(false);
                }
            }
            catch
            {
            }
        }

        return LogUniqueInstanceIds("AFTER pool ref repair", upgradeUIs, count);
    }


    public static void ActivateAllLiveSlots(List<GearUpgradeUI> upgradeUIs, int count)
    {
        if (upgradeUIs == null) return;
        count = Mathf.Min(count, upgradeUIs.Count);
        ClearStalePoolSlots(upgradeUIs, count);

        var isGrid = GetIsGridView(currentWindow);
        var activated = 0;
        for (var i = 0; i < count; i++)
        {
            var ui = upgradeUIs[i];
            if (ui?.Upgrade?.Upgrade == null) continue;
            try
            {
                if (!ui.gameObject.activeSelf)
                {
                    ui.gameObject.SetActive(true);


                    activated++;
                }

                ui.EnableGridView(isGrid);
            }
            catch
            {
            }
        }

        UpgradeFilteringPlugin.Logger.LogInfo(
            $"ActivateAllLiveSlots: count={count}, reactivated={activated}, isGrid={isGrid}");
    }


    public static void ApplyVisualPriorityOrder(GearDetailsWindow window, List<GearUpgradeUI> upgradeUIs,
        int count, List<PriorityCriteria> order)
    {
        if (window == null || upgradeUIs == null) return;
        count = Mathf.Min(count, upgradeUIs.Count);


        var visible = new List<(GearUpgradeUI ui, int[] keys)>(count);
        for (var i = 0; i < count; i++)
        {
            var ui = upgradeUIs[i];
            if (ui == null || !ui.gameObject.activeSelf) continue;
            if (ui.Upgrade?.Upgrade == null) continue;
            if (!FilterState.ShouldShow(ui)) continue;

            var keyLen = (order?.Count ?? 0) + 2;
            var keys = new int[keyLen];
            var k = 0;
            if (order != null)
                foreach (var c in order)
                    keys[k++] = ScoreCriteria(ui, c);
            keys[k++] = ui.Upgrade.InstanceID;
            keys[k] = -i;
            visible.Add((ui, keys));
        }

        visible.Sort((a, b) =>
        {
            var ka = a.keys;
            var kb = b.keys;
            var len = Math.Min(ka.Length, kb.Length);
            for (var i = 0; i < len; i++)
            {
                var cmp = kb[i].CompareTo(ka[i]);
                if (cmp != 0) return cmp;
            }

            return 0;
        });

        EnsureLayoutLookups();
        var isGrid = GetIsGridView(window);
        RectTransform listParent = null;
        try
        {
            listParent = _upgradeListParentField?.GetValue(window) as RectTransform;
        }
        catch
        {
        }


        for (var i = 0; i < count; i++)
            try
            {
                upgradeUIs[i]?.transform.SetAsLastSibling();
            }
            catch
            {
            }

        for (var vis = 0; vis < visible.Count; vis++)
        {
            var ui = visible[vis].ui;
            try
            {
                ui.transform.SetSiblingIndex(vis);
                ui.EnableGridView(isGrid);
                SetUpgradeAnchoredPosition(ui, vis, isGrid, listParent);
            }
            catch (Exception ex)
            {
                UpgradeFilteringPlugin.Logger.LogWarning($"Visual order place failed: {ex.Message}");
            }
        }


        var sib = visible.Count;
        for (var i = 0; i < count; i++)
        {
            var ui = upgradeUIs[i];
            if (ui == null || ui.gameObject.activeSelf) continue;
            try
            {
                ui.transform.SetSiblingIndex(sib++);
            }
            catch
            {
            }
        }

        try
        {
            Canvas.ForceUpdateCanvases();
        }
        catch
        {
        }

        UpgradeFilteringPlugin.Logger.LogInfo(
            $"Visual priority order: visiblePlaced={visible.Count}, isGrid={isGrid}");
    }


    private static IEnumerator DeferredLayout(GearDetailsWindow window)
    {
        yield return null;
        _deferredLayoutCoroutine = null;
        if (_deferredLayoutCancelled || window == null) yield break;
        try
        {
            if (FilterState.HasActiveFilters() || PrioritySortActive)
            {
                FilterState.ApplyToWindow(window);
            }
            else
            {
                var list = FilterState.GetUpgradeUIs();
                var count = FilterState.GetUpgradeUICount(window);
                if (list == null) yield break;
                if (count <= 0 || count > list.Count) count = list.Count;
                ForceLayout(window, list, count);
            }

            UpgradeFilteringPlugin.Logger.LogInfo("Deferred layout pass complete.");
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogWarning($"Deferred layout failed: {ex.Message}");
        }
    }


    internal static void LogRowSnapshot(string label, List<GearUpgradeUI> upgradeUIs, int count)
    {
        try
        {
            var sb = new StringBuilder();
            sb.Append(label).Append(':');
            var n = 0;
            for (var i = 0; i < count && n < 6; i++)
            {
                var ui = upgradeUIs[i];
                if (ui == null) continue;
                var name = "?";
                var rarity = "?";
                try
                {
                    if (ui.Upgrade?.Upgrade != null)
                    {
                        name = ui.Upgrade.Upgrade.Name ?? "?";
                        rarity = ui.Upgrade.Upgrade.Rarity.ToString();
                    }
                }
                catch
                {
                }

                var rt = ui.transform as RectTransform;
                var pos = rt != null ? rt.anchoredPosition : Vector2.zero;
                var sib = ui.transform.GetSiblingIndex();
                var parent = ui.transform.parent != null ? ui.transform.parent.name : "null";
                sb.Append("\n  [").Append(i).Append("] ").Append(rarity)
                    .Append(" | ").Append(name)
                    .Append(" | active=").Append(ui.gameObject.activeSelf)
                    .Append(" | sib=").Append(sib)
                    .Append(" | pos=").Append(pos)
                    .Append(" | parent=").Append(parent);
                n++;
            }

            UpgradeFilteringPlugin.Logger.LogInfo(sb.ToString());
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogWarning($"LogRowSnapshot failed: {ex.Message}");
        }
    }

    private static void EnsureLayoutLookups()
    {
        if (_layoutLookupsDone) return;
        _layoutLookupsDone = true;


        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.Public | BindingFlags.NonPublic;
        var t = typeof(GearDetailsWindow);
        _isGridViewField = t.GetField("isGridView", flags);
        _upgradeListParentField = t.GetField("upgradeListParent", flags);

        UpgradeFilteringPlugin.Logger.LogInfo(
            $"Layout lookups: isGridView={_isGridViewField != null}, " +
            $"listParent={_upgradeListParentField != null}");
    }


    internal static bool GetIsGridView(GearDetailsWindow window)
    {
        EnsureLayoutLookups();
        try
        {
            if (_isGridViewField == null) return false;
            return _isGridViewField.IsStatic
                ? (bool)_isGridViewField.GetValue(null)
                : (bool)_isGridViewField.GetValue(window);
        }
        catch
        {
            return false;
        }
    }


    internal static void ForceLayout(GearDetailsWindow window, List<GearUpgradeUI> upgradeUIs, int count)
    {
        if (upgradeUIs == null || window == null) return;
        count = Mathf.Min(count, upgradeUIs.Count);
        EnsureLayoutLookups();

        var isGrid = false;
        try
        {
            if (_isGridViewField != null)
                isGrid = _isGridViewField.IsStatic
                    ? (bool)_isGridViewField.GetValue(null)
                    : (bool)_isGridViewField.GetValue(window);
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogWarning($"isGridView read failed: {ex.Message}");
        }

        RectTransform listParent = null;
        try
        {
            listParent = _upgradeListParentField?.GetValue(window) as RectTransform;
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogWarning($"upgradeListParent read failed: {ex.Message}");
        }


        string headBefore = null;
        try
        {
            if (count > 0 && upgradeUIs[0]?.Upgrade?.Upgrade != null)
                headBefore = upgradeUIs[0].Upgrade.Upgrade.Name;
        }
        catch
        {
        }

        Vector2 firstPosBefore = default;
        if (count > 0 && upgradeUIs[0] != null)
        {
            var rt0 = upgradeUIs[0].transform as RectTransform;
            if (rt0 != null) firstPosBefore = rt0.anchoredPosition;
        }


        for (var i = 0; i < count; i++)
        {
            var ui = upgradeUIs[i];
            if (ui == null) continue;
            try
            {
                ui.transform.SetAsLastSibling();
            }
            catch
            {
            }
        }

        var visibleIndex = 0;
        for (var i = 0; i < count; i++)
        {
            var ui = upgradeUIs[i];
            if (ui == null) continue;

            try
            {
                ui.transform.SetSiblingIndex(i);
            }
            catch (Exception ex)
            {
                UpgradeFilteringPlugin.Logger.LogWarning($"Sibling set failed @ {i}: {ex.Message}");
            }

            if (!ui.gameObject.activeSelf)
                continue;

            try
            {
                ui.EnableGridView(isGrid);

                SetUpgradeAnchoredPosition(ui, visibleIndex, isGrid, listParent);
            }
            catch (Exception ex)
            {
                UpgradeFilteringPlugin.Logger.LogWarning($"Inline pos failed @ {i}: {ex.Message}");
            }

            visibleIndex++;
        }


        try
        {
            Canvas.ForceUpdateCanvases();
        }
        catch
        {
        }

        string headAfter = null;
        try
        {
            if (count > 0 && upgradeUIs[0]?.Upgrade?.Upgrade != null)
                headAfter = upgradeUIs[0].Upgrade.Upgrade.Name;
        }
        catch
        {
        }

        Vector2 firstPosAfter = default;
        if (count > 0 && upgradeUIs[0] != null)
        {
            var rt0 = upgradeUIs[0].transform as RectTransform;
            if (rt0 != null) firstPosAfter = rt0.anchoredPosition;
        }

        if (headBefore != null && headAfter != null && headBefore != headAfter)
            UpgradeFilteringPlugin.Logger.LogError(
                $"ForceLayout mutated list head! '{headBefore}' -> '{headAfter}'");

        UpgradeFilteringPlugin.Logger.LogInfo(
            $"ForceLayout done: isGrid={isGrid}, listParent={(listParent != null ? listParent.name : "null")}, " +
            $"visiblePlaced={visibleIndex}, head={headAfter}, firstPos {firstPosBefore} -> {firstPosAfter}");
    }


    private static void SetUpgradeAnchoredPosition(GearUpgradeUI ui, int index, bool isGrid,
        RectTransform listParent)
    {
        var rt = (RectTransform)ui.transform;
        var h = rt.rect.height > 1f ? rt.rect.height : 52f;
        var w = rt.rect.width > 1f ? rt.rect.width : 300f;

        if (isGrid && listParent != null)
        {
            var cellW = w + 10f;
            var parentW = listParent.rect.width > 1f ? listParent.rect.width : 400f;
            var cols = Mathf.Max(Mathf.FloorToInt((parentW - 6f) / cellW), 1);
            var x = 3f + index % cols * (w + 10f);
            var y = -3f - index / cols * (h + 10f);
            rt.anchoredPosition = new Vector2(x, y);
        }
        else
        {
            var min = rt.offsetMin;
            var max = rt.offsetMax;
            rt.offsetMin = new Vector2(0f, min.y);
            rt.offsetMax = new Vector2(0f, max.y);
            rt.anchoredPosition = new Vector2(0f, -3f - (h + 4f) * index);
        }
    }


    public static List<PriorityCriteria> FilterOrderForAvailableMods(List<PriorityCriteria> order)
    {
        if (order == null) return new List<PriorityCriteria>();
        var filtered = new List<PriorityCriteria>(order.Count);
        var batch = IsBatchScrappingPresent();
        foreach (var c in order)
        {
            if (!batch && (c == PriorityCriteria.Trashed || c == PriorityCriteria.NotTrashed))
                continue;
            filtered.Add(c);
        }

        return MigrateTotalKeysAfterRarities(filtered);
    }


    public static List<PriorityCriteria> MigrateTotalKeysAfterRarities(List<PriorityCriteria> order)
    {
        if (order == null || order.Count == 0)
            return order ?? new List<PriorityCriteria>();

        bool IsRarity(PriorityCriteria c)
        {
            return c is PriorityCriteria.Oddity or PriorityCriteria.Exotic or PriorityCriteria.Epic
                or PriorityCriteria.Rare or PriorityCriteria.Standard;
        }

        bool IsTotalKey(PriorityCriteria c)
        {
            return c is PriorityCriteria.InstanceName or PriorityCriteria.RecentlyUsed
                or PriorityCriteria.RecentlyAcquired;
        }

        var firstRarity = -1;
        var lastRarity = -1;
        for (var i = 0; i < order.Count; i++)
        {
            if (!IsRarity(order[i])) continue;
            if (firstRarity < 0) firstRarity = i;
            lastRarity = i;
        }

        if (firstRarity < 0)
            return order;


        var needsMigrate = false;
        for (var i = 0; i < order.Count; i++)
        {
            if (!IsTotalKey(order[i])) continue;
            if (i < lastRarity)
            {
                needsMigrate = true;
                break;
            }
        }

        if (!needsMigrate)
            return order;

        var head = new List<PriorityCriteria>(order.Count);
        var tail = new List<PriorityCriteria>(4);
        foreach (var c in order)
            if (IsTotalKey(c))
                tail.Add(c);
            else
                head.Add(c);


        head.AddRange(tail);
        UpgradeFilteringPlugin.Logger.LogInfo(
            "Migrated sort order: moved Name/Recently* after rarities so rarity criteria apply.");
        return head;
    }


    public static bool TryDecorateSortLiveSlice(List<GearUpgradeUI> upgradeUIs, int count,
        List<PriorityCriteria> order)
    {
        if (upgradeUIs == null || count <= 0) return true;
        count = Mathf.Min(count, upgradeUIs.Count);

        var n = count;
        var entries = new SortEntry[n];
        var keyLen = (order?.Count ?? 0) + 2;

        for (var i = 0; i < n; i++)
        {
            var ui = upgradeUIs[i];
            var keys = new int[keyLen];
            var k = 0;
            if (order != null)
                foreach (var c in order)
                    keys[k++] = ScoreCriteria(ui, c);


            keys[k++] = ui?.Upgrade != null ? ui.Upgrade.InstanceID : int.MinValue;

            keys[k] = -i;


            entries[i] = new SortEntry { Ui = ui, Keys = keys };
        }

        Array.Sort(entries, SortEntryComparer.Instance);


        var before = new HashSet<int>();
        var after = new HashSet<int>();
        for (var i = 0; i < n; i++)
        {
            if (upgradeUIs[i]?.Upgrade != null) before.Add(upgradeUIs[i].Upgrade.InstanceID);
            if (entries[i].Ui?.Upgrade != null) after.Add(entries[i].Ui.Upgrade.InstanceID);
        }

        if (before.Count != after.Count || !before.SetEquals(after))
        {
            UpgradeFilteringPlugin.Logger.LogError(
                $"Decorate-sort validation failed: beforeIds={before.Count} afterIds={after.Count}");
            return false;
        }


        var seenRef = new HashSet<GearUpgradeUI>();
        for (var i = 0; i < n; i++)
        {
            var ui = entries[i].Ui;
            if (ui == null) continue;
            if (!seenRef.Add(ui))
            {
                UpgradeFilteringPlugin.Logger.LogError(
                    "Decorate-sort validation failed: duplicate GearUpgradeUI reference.");
                return false;
            }
        }

        for (var i = 0; i < n; i++)
            upgradeUIs[i] = entries[i].Ui;

        return true;
    }


    private static int ScoreCriteria(GearUpgradeUI ui, PriorityCriteria criteria)
    {
        var u = ui?.Upgrade;
        if (u?.Upgrade == null) return int.MinValue;

        switch (criteria)
        {
            case PriorityCriteria.Favorited:
                return u.Favorite ? 1 : 0;
            case PriorityCriteria.NotFavorited:
                return u.Favorite ? 0 : 1;
            case PriorityCriteria.Unlocked:
                return u.IsUnlocked ? 1 : 0;
            case PriorityCriteria.Locked:
                return u.IsUnlocked ? 0 : 1;
            case PriorityCriteria.Turbocharged:
                return u.IsTurbocharged ? 1 : 0;
            case PriorityCriteria.NotTurbocharged:
                return u.IsTurbocharged ? 0 : 1;
            case PriorityCriteria.Trashed:
                return IsTrashMarked(u) ? 0 : 1;
            case PriorityCriteria.NotTrashed:
                return IsTrashMarked(u) ? 0 : 1;
            case PriorityCriteria.RecentlyAcquired:

                return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, u.TimeUnlocked));
            case PriorityCriteria.RecentlyUsed:

                return (int)(u.TimeUnequipped * 1000f);
            case PriorityCriteria.InstanceName:
            {
                return 0;
            }
            case PriorityCriteria.Oddity:
                return u.Upgrade.Rarity == Rarity.Oddity ? 1 : 0;
            case PriorityCriteria.Exotic:
                return u.Upgrade.Rarity == Rarity.Exotic ? 1 : 0;
            case PriorityCriteria.Epic:
                return u.Upgrade.Rarity == Rarity.Epic ? 1 : 0;
            case PriorityCriteria.Rare:
                return u.Upgrade.Rarity == Rarity.Rare ? 1 : 0;
            case PriorityCriteria.Standard:
                return u.Upgrade.Rarity == Rarity.Standard ? 1 : 0;
            default:
                return 0;
        }
    }

    public static Comparison<GearUpgradeUI> GetPriorityComparison(List<PriorityCriteria> order)
    {
        return (a, b) =>
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            if (a.Upgrade == null && b.Upgrade == null) return 0;
            if (a.Upgrade == null) return 1;
            if (b.Upgrade == null) return -1;
            if (a.Upgrade.Upgrade == null && b.Upgrade.Upgrade == null) return 0;
            if (a.Upgrade.Upgrade == null) return 1;
            if (b.Upgrade.Upgrade == null) return -1;

            if (order != null)
                foreach (var criteria in order)
                {
                    var sa = ScoreCriteria(a, criteria);
                    var sb = ScoreCriteria(b, criteria);
                    var cmp = sb.CompareTo(sa);
                    if (cmp != 0) return cmp;
                }

            return a.Upgrade.InstanceID.CompareTo(b.Upgrade.InstanceID);
        };
    }


    public static bool LogUniqueInstanceIds(string label, List<GearUpgradeUI> upgradeUIs, int count)
    {
        try
        {
            var ids = new HashSet<int>();
            var nulls = 0;
            for (var i = 0; i < count; i++)
            {
                var ui = upgradeUIs[i];
                if (ui?.Upgrade == null)
                {
                    nulls++;
                    continue;
                }

                ids.Add(ui.Upgrade.InstanceID);
            }

            if (ids.Count + nulls != count)
            {
                UpgradeFilteringPlugin.Logger.LogError(
                    $"{label}: LIST CORRUPTION uniqueIds={ids.Count} nulls={nulls} count={count}");
                return false;
            }

            UpgradeFilteringPlugin.Logger.LogInfo(
                $"{label}: uniqueIds={ids.Count} nulls={nulls} count={count} OK");
            return true;
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogWarning($"LogUniqueInstanceIds failed: {ex.Message}");
            return false;
        }
    }


    public static void ForceLayoutVisibleOnly(GearDetailsWindow window, List<GearUpgradeUI> upgradeUIs,
        int count)
    {
        if (window == null || upgradeUIs == null) return;
        count = Mathf.Min(count, upgradeUIs.Count);
        EnsureLayoutLookups();

        var isGrid = GetIsGridView(window);
        RectTransform listParent = null;
        try
        {
            listParent = _upgradeListParentField?.GetValue(window) as RectTransform;
        }
        catch
        {
        }

        var visibleIndex = 0;
        for (var i = 0; i < count; i++)
        {
            var ui = upgradeUIs[i];
            if (ui == null || !ui.gameObject.activeSelf) continue;
            try
            {
                ui.transform.SetSiblingIndex(visibleIndex);
                ui.EnableGridView(isGrid);
                SetUpgradeAnchoredPosition(ui, visibleIndex, isGrid, listParent);
                visibleIndex++;
            }
            catch
            {
            }
        }

        try
        {
            Canvas.ForceUpdateCanvases();
        }
        catch
        {
        }

        UpgradeFilteringPlugin.Logger.LogInfo(
            $"ForceLayoutVisibleOnly: visiblePlaced={visibleIndex}, isGrid={isGrid}");
    }


    public static int CompareByCriteria(GearUpgradeUI a, GearUpgradeUI b, PriorityCriteria criteria)
    {
        switch (criteria)
        {
            case PriorityCriteria.Favorited:
                return -(a.Upgrade.Favorite ? 1 : 0).CompareTo(b.Upgrade.Favorite ? 1 : 0);
            case PriorityCriteria.NotFavorited:
                return -(a.Upgrade.Favorite ? 0 : 1).CompareTo(b.Upgrade.Favorite ? 0 : 1);
            case PriorityCriteria.Unlocked:
                return -a.Upgrade.IsUnlocked.CompareTo(b.Upgrade.IsUnlocked);
            case PriorityCriteria.Locked:
                return -(a.Upgrade.IsUnlocked ? 0 : 1).CompareTo(b.Upgrade.IsUnlocked ? 0 : 1);
            case PriorityCriteria.Turbocharged:
            {
                var isTurboA = a.Upgrade.IsTurbocharged;
                var isTurboB = b.Upgrade.IsTurbocharged;
                return -(isTurboA ? 1 : 0).CompareTo(isTurboB ? 1 : 0);
            }
            case PriorityCriteria.Trashed:
            {
                if (!IsBatchScrappingPresent()) return 0;
                var isTrashedA = IsTrashMarked(a.Upgrade);
                var isTrashedB = IsTrashMarked(b.Upgrade);

                return (isTrashedA ? 1 : 0).CompareTo(isTrashedB ? 1 : 0);
            }
            case PriorityCriteria.NotTurbocharged:
            {
                var notTurboA = !a.Upgrade.IsTurbocharged;
                var notTurboB = !b.Upgrade.IsTurbocharged;
                return -(notTurboA ? 1 : 0).CompareTo(notTurboB ? 1 : 0);
            }
            case PriorityCriteria.NotTrashed:
            {
                if (!IsBatchScrappingPresent()) return 0;
                var notTrashedA = !IsTrashMarked(a.Upgrade);
                var notTrashedB = !IsTrashMarked(b.Upgrade);
                return -(notTrashedA ? 1 : 0).CompareTo(notTrashedB ? 1 : 0);
            }
            case PriorityCriteria.RecentlyAcquired:
                return -a.Upgrade.TimeUnlocked.CompareTo(b.Upgrade.TimeUnlocked);
            case PriorityCriteria.RecentlyUsed:
                return -a.Upgrade.TimeUnequipped.CompareTo(b.Upgrade.TimeUnequipped);
            case PriorityCriteria.InstanceName:
            {
                var nameA = a.Upgrade.Upgrade.GetInstanceName(a.Upgrade.Seed);
                if (string.IsNullOrEmpty(nameA)) nameA = a.Upgrade.Upgrade.Name;
                var nameB = b.Upgrade.Upgrade.GetInstanceName(b.Upgrade.Seed);
                if (string.IsNullOrEmpty(nameB)) nameB = b.Upgrade.Upgrade.Name;
                return string.Compare(nameA, nameB, StringComparison.Ordinal);
            }
            case PriorityCriteria.Oddity:
                return -(a.Upgrade.Upgrade.Rarity == Rarity.Oddity ? 1 : 0).CompareTo(
                    b.Upgrade.Upgrade.Rarity == Rarity.Oddity ? 1 : 0);
            case PriorityCriteria.Exotic:
                return -(a.Upgrade.Upgrade.Rarity == Rarity.Exotic ? 1 : 0).CompareTo(
                    b.Upgrade.Upgrade.Rarity == Rarity.Exotic ? 1 : 0);
            case PriorityCriteria.Epic:
                return -(a.Upgrade.Upgrade.Rarity == Rarity.Epic ? 1 : 0).CompareTo(
                    b.Upgrade.Upgrade.Rarity == Rarity.Epic ? 1 : 0);
            case PriorityCriteria.Rare:
                return -(a.Upgrade.Upgrade.Rarity == Rarity.Rare ? 1 : 0).CompareTo(
                    b.Upgrade.Upgrade.Rarity == Rarity.Rare ? 1 : 0);
            case PriorityCriteria.Standard:
                return -(a.Upgrade.Upgrade.Rarity == Rarity.Standard ? 1 : 0).CompareTo(
                    b.Upgrade.Upgrade.Rarity == Rarity.Standard ? 1 : 0);
            default:
                return 0;
        }
    }

    public static List<PriorityCriteria> LoadPriorityOrder()
    {
        try
        {
            if (PlayerOptions.TryGetConfig<string>("SortPriority.Order", out var json))
                try
                {
                    var data = PriorityData.FromJson(json);
                    var list = new List<PriorityCriteria>();
                    if (data?.order != null)
                        foreach (var i in data.order)
                            if (Enum.IsDefined(typeof(PriorityCriteria), i))
                                list.Add((PriorityCriteria)i);
                    if (list.Count > 0)
                        return FilterOrderForAvailableMods(list);
                }
                catch (Exception ex)
                {
                    UpgradeFilteringPlugin.Logger.LogWarning(
                        $"Failed to parse priority order from config, using defaults: {ex.Message}");
                }

            return FilterOrderForAvailableMods(
                new PriorityData().order.ConvertAll(i => (PriorityCriteria)i));
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogError($"Critical error loading priority order: {ex.Message}");
            return FilterOrderForAvailableMods(
                new PriorityData().order.ConvertAll(i => (PriorityCriteria)i));
        }
    }

    public static void SavePriorityOrder(List<PriorityCriteria> order)
    {
        try
        {
            if (order == null) return;
            priorityOrder = new List<PriorityCriteria>(order);
            var data = new PriorityData { order = order.ConvertAll(c => (int)c) };
            var json = data.ToJson();
            PlayerOptions.SetConfig("SortPriority.Order", json);
            UpgradeFilteringPlugin.Logger.LogInfo($"Saved priority order ({order.Count} criteria).");
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogError($"Failed to save priority order: {ex.Message}");
        }
    }

    private struct SortEntry
    {
        public GearUpgradeUI Ui;
        public int[] Keys;
    }

    private sealed class SortEntryComparer : IComparer<SortEntry>
    {
        public static readonly SortEntryComparer Instance = new();

        public int Compare(SortEntry x, SortEntry y)
        {
            var kx = x.Keys;
            var ky = y.Keys;
            if (kx == null && ky == null) return 0;
            if (kx == null) return 1;
            if (ky == null) return -1;
            var len = Math.Min(kx.Length, ky.Length);
            for (var i = 0; i < len; i++)
            {
                var cmp = ky[i].CompareTo(kx[i]);
                if (cmp != 0) return cmp;
            }

            return kx.Length.CompareTo(ky.Length);
        }
    }
}