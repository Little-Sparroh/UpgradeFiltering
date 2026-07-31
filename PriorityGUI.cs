using System.Collections.Generic;
using System.Linq;
using Sparroh.UI;
using UnityEngine;
using UnityEngine.UI;

public class PriorityGUI : MonoBehaviour
{
    private static readonly PriorityCriteria[] BaseDefaultOrder =
    {
        PriorityCriteria.Favorited,
        PriorityCriteria.NotFavorited,
        PriorityCriteria.Unlocked,
        PriorityCriteria.Locked,
        PriorityCriteria.Oddity,
        PriorityCriteria.Exotic,
        PriorityCriteria.Epic,
        PriorityCriteria.Rare,
        PriorityCriteria.Standard,
        PriorityCriteria.Turbocharged,
        PriorityCriteria.NotTurbocharged,
        PriorityCriteria.RecentlyUsed,
        PriorityCriteria.RecentlyAcquired,
        PriorityCriteria.InstanceName
    };

    private static readonly PriorityCriteria[] TrashCriteria =
    {
        PriorityCriteria.Trashed, PriorityCriteria.NotTrashed
    };

    private UIDragList _list;
    private UIWindow _window;
    private List<PriorityCriteria> currentOrder;
    private bool showWindow;

    public static PriorityGUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentOrder = PriorityPatches.LoadPriorityOrder();
    }

    private void Update()
    {
        var gearOpen = PriorityPatches.IsWindowOpen || GearActionBar.IsGearMenuOpen();
        if (!gearOpen && showWindow)
            CloseWindow(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private static List<PriorityCriteria> BuildDefaultOrder()
    {
        var list = BaseDefaultOrder.ToList();
        if (PriorityPatches.IsBatchScrappingPresent())
        {
            var idx = list.IndexOf(PriorityCriteria.Locked);
            if (idx < 0) idx = list.Count - 1;
            list.InsertRange(idx + 1, TrashCriteria);
        }

        return list;
    }

    public static void EnsureExists()
    {
        var existing = FindObjectOfType<PriorityGUI>();
        if (existing != null)
        {
            Destroy(existing.gameObject);
            Instance = null;
        }

        var go = new GameObject("PriorityGUI");
        go.AddComponent<PriorityGUI>();
    }

    public static void ToggleWindowStatic()
    {
        if (Instance == null) return;
        Instance.ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (showWindow)
            CloseWindow(true);
        else
            OpenWindow();
    }

    private void OpenWindow()
    {
        showWindow = true;
        currentOrder = PriorityPatches.LoadPriorityOrder();
        if (currentOrder == null || currentOrder.Count == 0)
            currentOrder = BuildDefaultOrder();
        else
            currentOrder = PriorityPatches.FilterOrderForAvailableMods(currentOrder);

        if (_window == null)
        {
            _window = UIWindow.Create("SortPriority", new Vector2(340f, 560f), "Sort Priority");
            _window.OnClose(() => CloseWindow(true));

            var body = _window.Content;
            UIFactory.AddVerticalLayout(body.gameObject, UITheme.S(8f), UITheme.ScaledPadding(8, 8, 8, 8));

            UIText.Create(body, "Hint",
                "Higher = applied first. Put rarities above Name/Recently* or they won't matter.",
                UITheme.ScaledFontSmall, UIColors.TextSecondary);

            _list = UIDragList.Create(body, "PriorityList");
            UIHelpers.EnsureLayoutElement(_list.GameObject, preferredHeight: UITheme.S(400f),
                minHeight: UITheme.S(280f));
            var listLe = _list.GameObject.GetComponent<LayoutElement>();
            if (listLe != null) listLe.flexibleHeight = 1f;

            _list.OnReordered((from, to) =>
            {
                if (from < 0 || to < 0 || from >= currentOrder.Count || to >= currentOrder.Count)
                    return;
                var item = currentOrder[from];
                currentOrder.RemoveAt(from);
                currentOrder.Insert(to, item);
            });

            var btnRow = UIFactory.CreateRect("Buttons", body);
            UIHelpers.EnsureLayoutElement(btnRow.gameObject,
                preferredHeight: UITheme.ScaledButtonHeight + UITheme.S(4f));
            UIFactory.AddHorizontalLayout(btnRow.gameObject, UITheme.S(8f), new RectOffset(0, 0, 0, 0),
                TextAnchor.MiddleCenter, false);

            UIButton.Create(btnRow, "Save", () =>
            {
                PriorityPatches.SavePriorityOrder(currentOrder);
                PriorityPatches.TriggerPrioritySort(currentOrder);
                CloseWindow(false);
            }, UIButtonStyle.Primary).SetWidth(UITheme.S(90f));

            UIButton.Create(btnRow, "Cancel", () => CloseWindow(true))
                .SetWidth(UITheme.S(90f));

            UIButton.Create(btnRow, "Reset", () =>
            {
                currentOrder = BuildDefaultOrder();
                RefreshList();
            }, UIButtonStyle.Danger).SetWidth(UITheme.S(90f));
        }

        RefreshList();
        _window.Show();
    }

    private void RefreshList()
    {
        if (_list == null || currentOrder == null)
            return;
        var labels = new List<string>();
        foreach (var c in currentOrder)
            labels.Add(GetCriteriaName(c));
        _list.SetItems(labels);
    }

    private void CloseWindow(bool reload)
    {
        showWindow = false;
        if (reload)
            currentOrder = PriorityPatches.LoadPriorityOrder();
        if (_window != null)
            _window.Hide();
    }

    private string GetCriteriaName(PriorityCriteria criteria)
    {
        return criteria switch
        {
            PriorityCriteria.Favorited => "Favorited",
            PriorityCriteria.NotFavorited => "Not Favorited",
            PriorityCriteria.Unlocked => "Unlocked",
            PriorityCriteria.Locked => "Locked",
            PriorityCriteria.RecentlyUsed => "Recently Used",
            PriorityCriteria.RecentlyAcquired => "Recently Acquired",
            PriorityCriteria.InstanceName => "Name (tie-break)",
            PriorityCriteria.Oddity => "Oddity",
            PriorityCriteria.Exotic => "Exotic",
            PriorityCriteria.Epic => "Epic",
            PriorityCriteria.Rare => "Rare",
            PriorityCriteria.Standard => "Standard",
            PriorityCriteria.Turbocharged => "Turbocharged",
            PriorityCriteria.Trashed => "Trashed (BatchScrapping)",
            PriorityCriteria.NotTurbocharged => "Not Turbocharged",
            PriorityCriteria.NotTrashed => "Not Trashed (BatchScrapping)",
            _ => "Unknown"
        };
    }
}