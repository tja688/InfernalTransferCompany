using PixelCrushers;
using UnityEngine;
using UnityEngine.UI;

public class ExternalDockPanel : UIPanel
{
    [Header("Default Focus (Selectable)")]
    [SerializeField] private Selectable defaultFirstSelected;

    [Header("Auto Open On Start")]
    [SerializeField] private bool openOnStart = true;

    // 让 UIPanel 的 firstSelected（GameObject）与我们 Inspector 里的 Selectable 对齐
    private void ApplyFirstSelected()
    {
        if (defaultFirstSelected != null)
            firstSelected = defaultFirstSelected.gameObject;
    }

    // UIPanel 有 OnEnable，可覆写；记得调用 base
    protected override void OnEnable()
    {
        ApplyFirstSelected();
        base.OnEnable();
    }

    // UIPanel 的 Start() 是 protected virtual；这里覆写并调用 base
    protected override void Start()
    {
        ApplyFirstSelected();
        base.Start();

        // 两种方式都可以：
        // 1) 在 Inspector 把 startState 设为 Open（更推荐）；
        // 2) 或者用脚本确保打开并设焦：
        if (openOnStart && !isOpen)
        {
            Open(); // 压入 UIPanel 栈，成为常驻面板
        }

        // 主动把当前选中切到我们的第一个可交互控件
        if (defaultFirstSelected != null)
        {
            SetFocus(defaultFirstSelected.gameObject); // 注意：SetFocus 需要传 GameObject
        }
        else
        {
            CheckFocus(); // 没配 firstSelected 也尝试一次校验聚焦
        }
    }

    // 运行时可随时调用，保证把焦点抢回到 Dock（比如对话结束时）
    public void ForceFocus()
    {
        if (!isOpen) Open();
        if (defaultFirstSelected != null)
            SetFocus(defaultFirstSelected.gameObject);
        else
            CheckFocus();
    }
}