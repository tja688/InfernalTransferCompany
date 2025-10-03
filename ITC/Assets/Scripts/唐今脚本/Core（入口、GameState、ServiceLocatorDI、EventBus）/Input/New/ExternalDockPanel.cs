using System;
using System.Collections.Generic;
using ITC.UI.Focus;
using PixelCrushers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExternalDockPanel : UIPanel
{
    public enum LayoutMode { Horizontal, Vertical, Grid }

    [Serializable]
    public class EdgeTransferRule
    {
        [Tooltip("Friendly name for the rule (purely descriptive).")]
        public string name;

        [Tooltip("Index of the button to monitor. -1 代表列表末尾。")]
        public int itemIndex = 0;

        [Tooltip("When the player moves in this direction from the indexed button, a panel transfer is requested.")]
        public MoveDirection triggerDirection = MoveDirection.Up;

        [Tooltip("Optional explicit target domain. Overrides mask if assigned.")]
        public FocusTag explicitDomain;

        [Tooltip("If explicitDomain 未指定，可用层级掩码来寻找面板。")]
        public FocusDomainMask domainMask = new FocusDomainMask(FocusTier.Base, 0);

        [Tooltip("如果提供，将作为转移后的首选选中对象。")]
        public Selectable preferredTarget;

        [Tooltip("是否允许在被高层 UI 覆盖时依然强制切换面板。")]
        public bool forceWhenCovered = false;

        internal int ResolveIndex(int count)
        {
            if (count <= 0) return -1;
            if (itemIndex < 0) return Mathf.Clamp(count + itemIndex, 0, count - 1);
            return Mathf.Clamp(itemIndex, 0, count - 1);
        }
    }

    private class EdgeSentinel : MonoBehaviour, IMoveHandler
    {
        private ExternalDockPanel owner;
        private EdgeTransferRule rule;

        public void Initialize(ExternalDockPanel owner, EdgeTransferRule rule)
        {
            this.owner = owner;
            this.rule = rule;
        }

        public void OnMove(AxisEventData eventData)
        {
            if (owner == null || rule == null) return;
            if (eventData.moveDir != rule.triggerDirection) return;

            if (owner.TryExecuteEdgeRule(rule))
            {
                eventData.Use();
            }
        }
    }

    [Header("Dock Items & Layout")]
    public List<Selectable> dockItems = new List<Selectable>();
    [SerializeField] private LayoutMode layout = LayoutMode.Horizontal;
    [SerializeField] private int gridColumns = 3;
    [SerializeField] private bool wrap = false;

    [Header("Edge Transfers")]
    [SerializeField] private List<EdgeTransferRule> edgeTransferRules = new List<EdgeTransferRule>
    {
        new EdgeTransferRule { name = "First → Up", itemIndex = 0, triggerDirection = MoveDirection.Up },
        new EdgeTransferRule { name = "Last → Down", itemIndex = -1, triggerDirection = MoveDirection.Down }
    };

    [Header("Default Focus")]
    [SerializeField] private Selectable defaultFirstSelected;

    [Header("Auto Open On Start")]
    [SerializeField] private bool openOnStart = true;

    private FocusTag focusDomain;

    protected override void Awake()
    {
        base.Awake();
        focusDomain = GetComponent<FocusTag>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ApplyFirstSelected();
        RebuildNavigation();
    }

    protected override void Start()
    {
        base.Start();

        if (openOnStart && !isOpen)
        {
            Open();
        }

        FocusDefaultOnStart();
    }

    private void FocusDefaultOnStart()
    {
        var target = GetDockItem(0) ?? defaultFirstSelected;
        if (focusDomain != null)
        {
            FocusHub.Instance?.Focus(focusDomain, target);
        }
        else if (target != null)
        {
            SetFocus(target.gameObject);
        }
        else
        {
            CheckFocus();
        }
    }

    [ContextMenu("Rebuild Navigation")]
    public void RebuildNavigation()
    {
        dockItems.RemoveAll(x => x == null);

        for (int i = 0; i < dockItems.Count; i++)
        {
            var sel = dockItems[i];
            if (sel == null) continue;

            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            switch (layout)
            {
                case LayoutMode.Horizontal:
                    nav.selectOnLeft = GetSelectableForIndex(i - 1);
                    nav.selectOnRight = GetSelectableForIndex(i + 1);
                    break;
                case LayoutMode.Vertical:
                    nav.selectOnUp = GetSelectableForIndex(i - 1);
                    nav.selectOnDown = GetSelectableForIndex(i + 1);
                    break;
                case LayoutMode.Grid:
                    int cols = Mathf.Max(1, gridColumns);
                    nav.selectOnLeft = GetSelectableForIndex(i - 1);
                    nav.selectOnRight = GetSelectableForIndex(i + 1);
                    nav.selectOnUp = GetSelectableForIndex(i - cols);
                    nav.selectOnDown = GetSelectableForIndex(i + cols);
                    break;
            }

            sel.navigation = nav;
        }

        AttachEdgeSentinels();
        ApplyFirstSelected();
    }

    private void AttachEdgeSentinels()
    {
        foreach (var item in dockItems)
        {
            if (item == null) continue;
            var sentinels = item.GetComponents<EdgeSentinel>();
            for (int i = 0; i < sentinels.Length; i++)
            {
                if (Application.isPlaying) Destroy(sentinels[i]);
                else DestroyImmediate(sentinels[i]);
            }
        }

        if (edgeTransferRules == null) return;

        foreach (var rule in edgeTransferRules)
        {
            if (rule == null) continue;
            int index = rule.ResolveIndex(dockItems.Count);
            if (index < 0 || index >= dockItems.Count) continue;

            var selectable = dockItems[index];
            if (selectable == null) continue;

            var sentinel = selectable.gameObject.AddComponent<EdgeSentinel>();
            sentinel.Initialize(this, rule);
        }
    }

    private Selectable GetSelectableForIndex(int index)
    {
        if (dockItems.Count == 0) return null;
        if (wrap)
        {
            index = (index % dockItems.Count + dockItems.Count) % dockItems.Count;
            return dockItems[index];
        }

        if (index < 0 || index >= dockItems.Count) return null;
        return dockItems[index];
    }

    private Selectable GetDockItem(int index)
    {
        if (dockItems == null || dockItems.Count == 0) return null;
        index = Mathf.Clamp(index, 0, dockItems.Count - 1);
        return dockItems[index];
    }

    private void ApplyFirstSelected()
    {
        var first = GetDockItem(0) ?? defaultFirstSelected;
        if (first != null)
        {
            firstSelected = first.gameObject;
        }
    }

    private bool TryExecuteEdgeRule(EdgeTransferRule rule)
    {
        if (FocusHub.Instance == null) return false;

        var domain = rule.explicitDomain != null ? rule.explicitDomain : FocusHub.Instance.Find(rule.domainMask);
        if (domain == null) return false;

        var preferred = rule.preferredTarget != null ? rule.preferredTarget.gameObject : null;
        var flags = rule.forceWhenCovered ? FocusHub.FocusRequestFlags.Force : FocusHub.FocusRequestFlags.None;
        return FocusHub.Instance.Focus(domain, preferred, flags);
    }

    // 供别处调用：把 Dock 拉到栈顶并定位到某项
    public void FocusDock(int index = 0)
    {
        var target = GetDockItem(index) ?? defaultFirstSelected;
        if (focusDomain != null)
        {
            FocusHub.Instance?.Focus(focusDomain, target);
        }
        else if (target != null)
        {
            TakeFocus();
            SetFocus(target.gameObject);
        }
        else
        {
            TakeFocus();
            CheckFocus();
        }
    }
}
