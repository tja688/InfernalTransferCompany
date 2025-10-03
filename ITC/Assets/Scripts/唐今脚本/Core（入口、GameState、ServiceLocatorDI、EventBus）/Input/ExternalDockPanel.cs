using System.Collections;
using System.Collections.Generic;
using PixelCrushers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 如需直接引用标准类，添加命名空间；没有也不报错（可留空）
// using PixelCrushers.DialogueSystem;

public class ExternalDockPanel : UIPanel
{
    public enum LayoutMode { Horizontal, Vertical, Grid }

    [Header("Dock Items & Layout")]
    public List<Selectable> dockItems = new List<Selectable>();
    [SerializeField] private LayoutMode layout = LayoutMode.Horizontal;
    [SerializeField] private int gridColumns = 3;
    [SerializeField] private bool wrap = false;

    [Header("Auto-resolve Dialogue Menu Panel")]
    [Tooltip("自动查找当前激活的 Dialogue 菜单面板（如 StandardUIMenuPanel）")]
    [SerializeField] private bool autoResolveDialogueMenuPanel = true;

    [Tooltip("已解析到的对话菜单 UIPanel（运行时自动填充）")]
    public UIPanel dialogueMenuPanel;

    [Header("Edge → Dialogue 切换方向")]
    public MoveDirection startEdgeToDialogue = MoveDirection.Left;
    public MoveDirection endEdgeToDialogue   = MoveDirection.Right;

    [Header("Default Focus")]
    [SerializeField] private Selectable defaultFirstSelected;

    [Header("Auto Open On Start")]
    [SerializeField] private bool openOnStart = true;

    protected override void OnEnable()
    {
        base.OnEnable();
        ApplyFirstSelected();
        RebuildNavigation();
    }

    protected override void Start()
    {
        base.Start();

        if (openOnStart && !isOpen) Open();

        // 初次落焦到第 0 项
        if (dockItems != null && dockItems.Count > 0 && dockItems[0] != null)
            SetFocus(dockItems[0].gameObject);
        else if (defaultFirstSelected != null)
            SetFocus(defaultFirstSelected.gameObject);
        else
            CheckFocus();

        if (autoResolveDialogueMenuPanel)
            StartCoroutine(AutoResolveDialogueMenuPanelRoutine());
    }

    private IEnumerator AutoResolveDialogueMenuPanelRoutine()
    {
        // 刚进场/对话未开始时可能找不到，循环尝试直到找到为止
        var wait = new WaitForSeconds(0.25f);
        while (dialogueMenuPanel == null)
        {
            TryResolveDialogueMenuPanel();
            if (dialogueMenuPanel != null) break;
            yield return wait;
        }
    }

    private void TryResolveDialogueMenuPanel()
    {
        if (dialogueMenuPanel != null) return;

        // 1) 优先：在场景里查找任何激活的 UIPanel，其类型名包含 "MenuPanel"（覆盖官方/自定义）
#if UNITY_2023_1_OR_NEWER
        var allPanels = Object.FindObjectsByType<UIPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var allPanels = Resources.FindObjectsOfTypeAll<UIPanel>();
#endif
        UIPanel candidate = null;
        foreach (var p in allPanels)
        {
            if (p == null) continue;
            // 候选条件：名字或类型包含 "MenuPanel"，且在层级中处于激活（activeInHierarchy）
            var active = p.gameObject.activeInHierarchy;
            var typeName = p.GetType().Name;
            if ((typeName.Contains("MenuPanel") || p.name.Contains("MenuPanel")) && active)
            {
                candidate = p;
                break;
            }
        }

        // 2) 次优：找不到激活的，就退而求其次——取第一个名字/类型匹配的（即使当前未激活）
        if (candidate == null)
        {
            foreach (var p in allPanels)
            {
                if (p == null) continue;
                var typeName = p.GetType().Name;
                if (typeName.Contains("MenuPanel") || p.name.Contains("MenuPanel"))
                {
                    candidate = p;
                    break;
                }
            }
        }

        if (candidate != null)
        {
            dialogueMenuPanel = candidate;
            // 可选：如果它当前就是打开状态，顺手校验一下选中对象
            if (dialogueMenuPanel.isOpen)
            {
                var fs = dialogueMenuPanel.firstSelected;
                if (fs != null) dialogueMenuPanel.SetFocus(fs);
                else dialogueMenuPanel.CheckFocus();
            }
        }
    }

    private void ApplyFirstSelected()
    {
        if (dockItems != null && dockItems.Count > 0 && dockItems[0] != null)
            firstSelected = dockItems[0].gameObject;
        else if (defaultFirstSelected != null)
            firstSelected = defaultFirstSelected.gameObject;
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
                    nav.selectOnLeft  = GetSelectableForIndex(i - 1, i, true);
                    nav.selectOnRight = GetSelectableForIndex(i + 1, i, false);
                    break;
                case LayoutMode.Vertical:
                    nav.selectOnUp    = GetSelectableForIndex(i - 1, i, true);
                    nav.selectOnDown  = GetSelectableForIndex(i + 1, i, false);
                    break;
                case LayoutMode.Grid:
                    int cols = Mathf.Max(1, gridColumns);
                    nav.selectOnLeft  = GetSelectableForIndex(i - 1,  i, true);
                    nav.selectOnRight = GetSelectableForIndex(i + 1,  i, false);
                    nav.selectOnUp    = GetSelectableForIndex(i - cols, i, true);
                    nav.selectOnDown  = GetSelectableForIndex(i + cols, i, false);
                    break;
            }
            sel.navigation = nav;

            AttachEdgeTransfer(i);
        }

        ApplyFirstSelected();
    }

    private Selectable GetSelectableForIndex(int targetIndex, int selfIndex, bool isPrev)
    {
        if (wrap)
        {
            if (dockItems.Count == 0) return null;
            targetIndex = (targetIndex % dockItems.Count + dockItems.Count) % dockItems.Count;
            return dockItems[targetIndex];
        }
        if (targetIndex < 0 || targetIndex >= dockItems.Count) return null;
        return dockItems[targetIndex];
    }

    private void AttachEdgeTransfer(int i)
    {
        var sel = dockItems[i];
        if (sel == null) return;

        var old = sel.GetComponent<EdgeTransferOnMove>();
        if (old != null) DestroyImmediate(old);

        if (i == 0)
        {
            var t = sel.gameObject.AddComponent<EdgeTransferOnMove>();
            t.mode = EdgeTransferOnMove.Mode.ToRegisteredKey;
            t.registeredKey = FocusKey.DialogueMenu;  // 0 号 → 对话菜单
            t.triggerDirection = startEdgeToDialogue;
        }

        if (i == dockItems.Count - 1)
        {
            var t = sel.gameObject.AddComponent<EdgeTransferOnMove>();
            t.mode = EdgeTransferOnMove.Mode.ToRegisteredKey;
            t.registeredKey = FocusKey.DialogueMenu;  // 末位 → 对话菜单
            t.triggerDirection = endEdgeToDialogue;
        }
    }

    // 供别处调用：把 Dock 拉到栈顶并定位到某项
    public void FocusDock(int index = 0)
    {
        TakeFocus();
        var target = (dockItems != null && dockItems.Count > 0) ? dockItems[Mathf.Clamp(index, 0, dockItems.Count - 1)] : null;
        if (target != null) SetFocus(target.gameObject);
        else if (firstSelected != null) SetFocus(firstSelected);
        else CheckFocus();
    }
}
