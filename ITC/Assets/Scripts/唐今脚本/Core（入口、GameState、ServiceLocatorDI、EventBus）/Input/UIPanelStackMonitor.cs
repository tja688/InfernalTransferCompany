// UIPanelStackMonitor.cs  (修正版：支持属性/字段双通道)
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PixelCrushers;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIPanelStackMonitor : MonoBehaviour
{
    [Header("Toggles")]
    public bool showOverlay = true;
    public bool logTopChange = true;
    public bool logStackChange = true;
    public bool logSelectionChange = false;
    public bool logRequests = true;
    public float pollInterval = 0.1f;

    // 反射缓存
    private static FieldInfo fiPanelStack;       // private static List<UIPanel> panelStack
    private static PropertyInfo piTopPanel;      // public static UIPanel topPanel { get; }

    private readonly List<UIPanel> lastStack = new List<UIPanel>();
    private UIPanel lastTop;
    private GameObject lastSelected;
    private string overlayText = "";

    void Awake()
    {
        var t = typeof(UIPanel);
        fiPanelStack = t.GetField("panelStack", BindingFlags.Static | BindingFlags.NonPublic);
        // 注意：新版是“属性”不是“字段”
        piTopPanel   = t.GetProperty("topPanel", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (fiPanelStack == null)
            Debug.LogWarning("[UIPanelStackMonitor] 找不到 UIPanel.panelStack 字段，版本可能变更。");
        if (piTopPanel == null)
            Debug.LogWarning("[UIPanelStackMonitor] 找不到 UIPanel.topPanel 属性，使用 panelStack 推导。");
    }

    void OnEnable()  => StartCoroutine(Poll());
    void OnDisable() => StopAllCoroutines();

    IEnumerator Poll()
    {
        var wait = new WaitForSeconds(pollInterval);
        while (true) { Tick(false); yield return wait; }
    }

    public void ForceRefreshOnce() => Tick(true);

    public void RequestTakeFocus(UIPanel panel, string reason = null)
    {
        if (panel == null) return;
        if (logRequests) Debug.Log($"[UIPanelStackMonitor] RequestTakeFocus -> {Nice(panel)}  reason: {reason}");
        panel.TakeFocus();
        panel.CheckFocus();
        ForceRefreshOnce();
    }

    private void Tick(bool force)
    {
        var stack = GetStack();
        var top   = GetTop(stack);
        var sel   = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;

        if (force || !Same(lastStack, stack))
        {
            if (logStackChange) Debug.Log($"[UIPanelStackMonitor] Stack changed:\n{FormatStack(stack)}");
            lastStack.Clear(); lastStack.AddRange(stack);
        }
        if (force || top != lastTop)
        {
            if (logTopChange) Debug.Log($"[UIPanelStackMonitor] TopPanel: {(lastTop?Nice(lastTop):"null")} -> {(top?Nice(top):"null")}");
            lastTop = top;
        }
        if (force || sel != lastSelected)
        {
            if (logSelectionChange)
                Debug.Log($"[UIPanelStackMonitor] Selected: {(sel?sel.name:"null")}  owner={OwnerOf(sel)}");
            lastSelected = sel;
        }
        BuildOverlay(stack, top, sel);
    }

    private List<UIPanel> GetStack()
    {
        var list = new List<UIPanel>();
        if (fiPanelStack != null)
        {
            var obj = fiPanelStack.GetValue(null) as System.Collections.IList;
            if (obj != null) foreach (var it in obj) if (it is UIPanel p) list.Add(p);
        }
        return list;
    }

    private UIPanel GetTop(List<UIPanel> stack)
    {
        // 先尝试属性
        if (piTopPanel != null)
        {
            try { var p = piTopPanel.GetValue(null, null) as UIPanel; if (p != null) return p; }
            catch { /* ignore */ }
        }
        // 回落：用 stack 推导
        return (stack != null && stack.Count > 0) ? stack[stack.Count - 1] : null;
    }

    private bool Same(List<UIPanel> a, List<UIPanel> b)
    {
        if (a == null || b == null || a.Count != b.Count) return false;
        for (int i=0;i<a.Count;i++) if (a[i]!=b[i]) return false;
        return true;
    }

    private string Nice(UIPanel p) => p ? $"{p.GetType().Name} \"{p.name}\"" : "null";

    private string OwnerOf(GameObject go)
    {
        if (!go) return "null";
        var panel = go.GetComponentInParent<UIPanel>();
        return panel ? Nice(panel) : "none";
    }

    private string FormatStack(List<UIPanel> stack)
    {
        if (stack == null || stack.Count == 0) return "  <empty>";
        var lines = new List<string>();
        for (int i=0;i<stack.Count;i++)
        {
            var p = stack[i];
            lines.Add($"  [{i}] {Nice(p)}  isOpen={p?.isOpen} active={(p && p.gameObject.activeInHierarchy)}");
        }
        return string.Join("\n", lines);
    }

    private void BuildOverlay(List<UIPanel> stack, UIPanel top, GameObject sel)
    {
        if (!showOverlay) { overlayText = ""; return; }
        overlayText =
            $"UIPanel Stack (bottom → top)\n{FormatStack(stack)}\n\n" +
            $"TopPanel: {(top?Nice(top):"null")}\n" +
            $"Selected: {(sel?sel.name:"null")} owner={OwnerOf(sel)}\n" +
            $"EventSystem: {(EventSystem.current?EventSystem.current.name:"null")}";
    }

    void OnGUI()
    {
        if (!showOverlay || string.IsNullOrEmpty(overlayText)) return;
        var old = GUI.color;
        GUI.color = new Color(0,0,0,0.6f);
        GUI.Box(new Rect(10,10,580,210), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(18,16,560,190), overlayText);
        GUI.color = old;
    }
}
