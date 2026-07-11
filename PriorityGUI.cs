using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sparroh.UI;

public class PriorityGUI : MonoBehaviour
{
    private bool showWindow;
    private List<PriorityCriteria> currentOrder;
    private UIWindow _window;
    private UIDragList _list;
    private bool _barRegistered;


    private static readonly PriorityCriteria[] DefaultOrder =
    {
        PriorityCriteria.Favorited, PriorityCriteria.NotFavorited, PriorityCriteria.Unlocked,
        PriorityCriteria.Locked, PriorityCriteria.RecentlyUsed, PriorityCriteria.RecentlyAcquired,
        PriorityCriteria.InstanceName, PriorityCriteria.Oddity, PriorityCriteria.Exotic,
        PriorityCriteria.Epic, PriorityCriteria.Rare, PriorityCriteria.Standard,
        PriorityCriteria.Turbocharged, PriorityCriteria.Trashed, PriorityCriteria.NotTurbocharged,
        PriorityCriteria.NotTrashed
    };

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        currentOrder = PriorityPatches.LoadPriorityOrder();
    }

    void Update()
    {
        GearActionBar.Tick();
        bool gearOpen = PriorityPatches.IsWindowOpen || GearActionBar.IsGearMenuOpen();
        if (gearOpen)
        {
            if (!_barRegistered)
            {
                GearActionBar.Register("priority", "Upgr. Sort", GearActionBar.OrderPriority, ToggleWindow, UIButtonStyle.Primary);
                _barRegistered = true;
            }
        }
        else if (showWindow)
        {
            CloseWindow(reload: true);
        }
    }


    private void ToggleWindow()
    {
        if (showWindow)
            CloseWindow(reload: true);
        else
            OpenWindow();
    }

    private void OpenWindow()
    {
        showWindow = true;
        currentOrder = PriorityPatches.LoadPriorityOrder();
        if (currentOrder == null || currentOrder.Count == 0)
            currentOrder = DefaultOrder.ToList();

        if (_window == null)
        {
            _window = UIWindow.Create("SortPriority", new Vector2(340f, 560f), "Sort Priority",
                scrollable: false, closeButton: true);
            _window.OnClose(() => CloseWindow(reload: true));

            var body = _window.Content;
            UIFactory.AddVerticalLayout(body.gameObject, UITheme.S(8f), UITheme.ScaledPadding(8, 8, 8, 8));

            _list = UIDragList.Create(body, "PriorityList");
            UIHelpers.EnsureLayoutElement(_list.GameObject, preferredHeight: UITheme.S(420f), minHeight: UITheme.S(300f));
            var listLe = _list.GameObject.GetComponent<UnityEngine.UI.LayoutElement>();
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
            UIHelpers.EnsureLayoutElement(btnRow.gameObject, preferredHeight: UITheme.ScaledButtonHeight + UITheme.S(4f));
            UIFactory.AddHorizontalLayout(btnRow.gameObject, UITheme.S(8f), new RectOffset(0, 0, 0, 0),
                TextAnchor.MiddleCenter, controlChildWidth: false, expandWidth: false);

            UIButton.Create(btnRow, "Save", () =>
            {
                PriorityPatches.SavePriorityOrder(currentOrder);
                PriorityPatches.TriggerPrioritySort();
                CloseWindow(reload: false);
            }, UIButtonStyle.Primary).SetWidth(UITheme.S(90f));

            UIButton.Create(btnRow, "Cancel", () => CloseWindow(reload: true), UIButtonStyle.Default)
                .SetWidth(UITheme.S(90f));

            UIButton.Create(btnRow, "Reset", () =>
            {
                currentOrder = DefaultOrder.ToList();
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

    string GetCriteriaName(PriorityCriteria criteria)
    {
        return criteria switch
        {
            PriorityCriteria.Favorited => "Favorited",
            PriorityCriteria.NotFavorited => "Not Favorited",
            PriorityCriteria.Unlocked => "Unlocked",
            PriorityCriteria.Locked => "Locked",
            PriorityCriteria.RecentlyUsed => "Recently Used",
            PriorityCriteria.RecentlyAcquired => "Recently Acquired",
            PriorityCriteria.InstanceName => "Upgrade Instance Name",
            PriorityCriteria.Oddity => "Oddity",
            PriorityCriteria.Exotic => "Exotic",
            PriorityCriteria.Epic => "Epic",
            PriorityCriteria.Rare => "Rare",
            PriorityCriteria.Standard => "Standard",
            PriorityCriteria.Turbocharged => "Turbocharged",
            PriorityCriteria.Trashed => "Trashed",
            PriorityCriteria.NotTurbocharged => "Not Turbocharged",
            PriorityCriteria.NotTrashed => "Not Trashed",
            _ => "Unknown"
        };
    }
}
