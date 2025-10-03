using System.Collections.Generic;
using PixelCrushers;
using UnityEngine;

public enum FocusKey { Dock, DialogueMenu, Settings, Inventory, Custom1, Custom2 }

public class FocusHub : MonoBehaviour
{
    public static FocusHub Instance { get; private set; }

    private readonly Dictionary<FocusKey, UIPanel> map = new Dictionary<FocusKey, UIPanel>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(FocusKey key, UIPanel panel) => map[key] = panel;

    public void Unregister(UIPanel panel)
    {
        foreach (var kv in new List<KeyValuePair<FocusKey, UIPanel>>(map))
            if (kv.Value == panel) map.Remove(kv.Key);
    }

    public void Focus(FocusKey key, GameObject preferred = null)
    {
        if (!map.TryGetValue(key, out var panel) || panel == null) return;
        panel.TakeFocus();
        if (preferred != null) panel.SetFocus(preferred);
        else if (panel.firstSelected != null) panel.SetFocus(panel.firstSelected);
        else panel.CheckFocus();
    }
}

