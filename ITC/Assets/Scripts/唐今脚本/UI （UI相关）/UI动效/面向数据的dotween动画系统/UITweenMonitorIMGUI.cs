using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight runtime IMGUI visualisation for the UITween monitor.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class UITweenMonitorIMGUI : MonoBehaviour
{
    private static UITweenMonitorIMGUI _instance;
    private readonly List<UITweenMonitor.UITweenMonitorEntry> _entries = new();
    private Rect _windowRect = new Rect(20f, 20f, 720f, 420f);
    private Vector2 _scroll;
    private bool _visible;
    private string _search = string.Empty;
    private bool _showPending = true;
    private bool _showPlaying = true;
    private bool _showCompleted = true;
    private bool _showInterrupted = true;

    public KeyCode toggleKey = KeyCode.F8;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;
        var go = new GameObject("[UITweenMonitorIMGUI]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<UITweenMonitorIMGUI>();
        go.hideFlags = HideFlags.HideAndDontSave;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _visible = !_visible;
        }
    }

    private void OnGUI()
    {
        if (!_visible) return;
        _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "UITween Monitor", GUILayout.Width(720f), GUILayout.Height(420f));
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search", GUILayout.Width(60f));
        _search = GUILayout.TextField(_search ?? string.Empty);
        if (GUILayout.Button("×", GUILayout.Width(28f)))
        {
            _search = string.Empty;
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(72f)))
        {
            _visible = false;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        ToggleFilter("Pending", ref _showPending);
        ToggleFilter("Playing", ref _showPlaying);
        ToggleFilter("Completed", ref _showCompleted);
        ToggleFilter("Interrupted", ref _showInterrupted);
        GUILayout.EndHorizontal();

        var monitor = UITweenMonitor.Instance;
        monitor.GetEntries(_entries);

        string searchLower = string.IsNullOrEmpty(_search) ? null : _search.ToLowerInvariant();
        double now = Time.realtimeSinceStartup;

        _scroll = GUILayout.BeginScrollView(_scroll, false, true);
        foreach (var entry in _entries)
        {
            if (!IsStatusVisible(entry.status)) continue;
            if (!PassesSearch(entry, searchLower)) continue;
            DrawEntry(entry, now);
        }
        GUILayout.EndScrollView();

        GUILayout.Label("Toggle with " + toggleKey + " · Entries: " + _entries.Count, EditorStylesLike.SmallLabel);
        GUI.DragWindow();
    }

    private bool IsStatusVisible(UITweenMonitor.EntryStatus status)
    {
        return (status == UITweenMonitor.EntryStatus.Pending && _showPending)
            || (status == UITweenMonitor.EntryStatus.Playing && _showPlaying)
            || (status == UITweenMonitor.EntryStatus.Completed && _showCompleted)
            || (status == UITweenMonitor.EntryStatus.Interrupted && _showInterrupted);
    }

    private bool PassesSearch(in UITweenMonitor.UITweenMonitorEntry entry, string searchLower)
    {
        if (string.IsNullOrEmpty(searchLower)) return true;
        if (!string.IsNullOrEmpty(entry.presetName) && entry.presetName.ToLowerInvariant().Contains(searchLower)) return true;
        if (!string.IsNullOrEmpty(entry.responderName) && entry.responderName.ToLowerInvariant().Contains(searchLower)) return true;
        if (!string.IsNullOrEmpty(entry.responderPath) && entry.responderPath.ToLowerInvariant().Contains(searchLower)) return true;
        if (!string.IsNullOrEmpty(entry.initiatorName) && entry.initiatorName.ToLowerInvariant().Contains(searchLower)) return true;
        if (!string.IsNullOrEmpty(entry.initiatorDetails) && entry.initiatorDetails.ToLowerInvariant().Contains(searchLower)) return true;
        if (!string.IsNullOrEmpty(entry.initiatorType) && entry.initiatorType.ToLowerInvariant().Contains(searchLower)) return true;
        return false;
    }

    private void DrawEntry(in UITweenMonitor.UITweenMonitorEntry entry, double now)
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label(string.Format("#{0} · {1} · {2}{3}",
            entry.requestId,
            entry.status,
            entry.presetName,
            entry.reversed ? " (Reversed)" : string.Empty));

        double start = entry.startedAt > 0.0 ? entry.startedAt : entry.createdAt;
        double end = entry.endedAt > 0.0 ? entry.endedAt : now;
        double duration = Mathf.Max(0f, (float)(end - start));
        GUILayout.Label(string.Format("Duration: {0:0.000}s  Frames: {1} → {2}", duration, entry.startFrame, entry.endFrame));

        GUILayout.Label("Responder: " + entry.responderName + " · " + entry.responderPath);
        GUILayout.Label("Initiator: " + BuildInitiatorLine(entry));
        if (!string.IsNullOrEmpty(entry.interruptionReason))
        {
            GUILayout.Label("Reason: " + entry.interruptionReason);
        }

        GUILayout.EndVertical();
    }

    private string BuildInitiatorLine(in UITweenMonitor.UITweenMonitorEntry entry)
    {
        var pieces = new List<string>(3);
        if (!string.IsNullOrEmpty(entry.initiatorType)) pieces.Add(entry.initiatorType);
        if (!string.IsNullOrEmpty(entry.initiatorName)) pieces.Add(entry.initiatorName);
        if (!string.IsNullOrEmpty(entry.initiatorDetails)) pieces.Add(entry.initiatorDetails);
        if (pieces.Count == 0 && !string.IsNullOrEmpty(entry.initiatorMethod)) pieces.Add(entry.initiatorMethod);
        return string.Join(" · ", pieces);
    }

    private void ToggleFilter(string label, ref bool value)
    {
        value = GUILayout.Toggle(value, label, GUILayout.Width(110f));
    }

    private static class EditorStylesLike
    {
        private static GUIStyle _smallLabel;
        public static GUIStyle SmallLabel
        {
            get
            {
                if (_smallLabel == null)
                {
                    _smallLabel = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = Color.gray }
                    };
                }
                return _smallLabel;
            }
        }
    }
}
