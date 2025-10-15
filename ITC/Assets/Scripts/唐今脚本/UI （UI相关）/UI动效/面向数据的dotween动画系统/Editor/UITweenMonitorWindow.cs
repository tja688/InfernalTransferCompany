#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for inspecting UITween monitor traffic during Play Mode.
/// </summary>
public class UITweenMonitorWindow : EditorWindow
{
    private readonly List<UITweenMonitor.UITweenMonitorEntry> _entries = new();
    private readonly HashSet<long> _expanded = new();
    private Vector2 _scroll;
    private string _search = string.Empty;
    private bool _showPending = true;
    private bool _showPlaying = true;
    private bool _showCompleted = true;
    private bool _showInterrupted = true;

    [MenuItem("Window/UI/UITween Monitor")]
    public static void Open()
    {
        var window = GetWindow<UITweenMonitorWindow>("UITween Monitor");
        window.Show();
    }

    private void OnEnable()
    {
        UITweenMonitor.Instance.Changed += Repaint;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        UITweenMonitor.Instance.Changed -= Repaint;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            UITweenMonitor.Instance.Clear();
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        var monitor = UITweenMonitor.Instance;
        monitor.GetEntries(_entries);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to capture runtime UITween traffic.", MessageType.Info);
        }

        string searchLower = string.IsNullOrEmpty(_search) ? null : _search.ToLowerInvariant();
        double now = EditorApplication.isPlaying ? Time.realtimeSinceStartup : 0d;

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var entry in _entries)
        {
            if (!IsStatusVisible(entry.status)) continue;
            if (!PassesSearch(entry, searchLower)) continue;
            DrawEntry(entry, now);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Search", GUILayout.Width(50f));
        _search = GUILayout.TextField(_search, EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60f)))
        {
            _search = string.Empty;
        }
        GUILayout.FlexibleSpace();
        _showPending = GUILayout.Toggle(_showPending, "Pending", EditorStyles.toolbarButton);
        _showPlaying = GUILayout.Toggle(_showPlaying, "Playing", EditorStyles.toolbarButton);
        _showCompleted = GUILayout.Toggle(_showCompleted, "Completed", EditorStyles.toolbarButton);
        _showInterrupted = GUILayout.Toggle(_showInterrupted, "Interrupted", EditorStyles.toolbarButton);
        if (GUILayout.Button("Clear Buffer", EditorStyles.toolbarButton, GUILayout.Width(90f)))
        {
            UITweenMonitor.Instance.Clear();
        }
        EditorGUILayout.EndHorizontal();
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
        if (!string.IsNullOrEmpty(entry.initiatorMethod) && entry.initiatorMethod.ToLowerInvariant().Contains(searchLower)) return true;
        return false;
    }

    private void DrawEntry(in UITweenMonitor.UITweenMonitorEntry entry, double now)
    {
        bool expanded = _expanded.Contains(entry.requestId);
        string header = string.Format("#{0} · {1} · {2}{3}",
            entry.requestId,
            entry.status,
            entry.presetName,
            entry.reversed ? " (Reversed)" : string.Empty);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        expanded = EditorGUILayout.Foldout(expanded, header, true);
        if (expanded)
        {
            _expanded.Add(entry.requestId);
            EditorGUI.indentLevel++;

            double start = entry.startedAt > 0.0 ? entry.startedAt : entry.createdAt;
            double end = entry.endedAt > 0.0 ? entry.endedAt : (EditorApplication.isPlaying ? now : start);
            double duration = Mathf.Max(0f, (float)(end - start));
            EditorGUILayout.LabelField("Duration", duration.ToString("0.000") + " s");
            EditorGUILayout.LabelField("Frames", entry.startFrame + " → " + entry.endFrame);
            EditorGUILayout.LabelField("Responder", entry.responderName + " · " + entry.responderPath);
            EditorGUILayout.LabelField("Initiator", BuildInitiatorLine(entry));
            if (!string.IsNullOrEmpty(entry.interruptionReason))
            {
                EditorGUILayout.LabelField("Reason", entry.interruptionReason);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Player", EditorStyles.miniButtonLeft) && entry.responderObject != null)
            {
                EditorGUIUtility.PingObject(entry.responderObject);
                Selection.activeObject = entry.responderObject;
            }
            if (GUILayout.Button("Ping Initiator", EditorStyles.miniButtonMid) && entry.initiatorObject != null)
            {
                EditorGUIUtility.PingObject(entry.initiatorObject);
                Selection.activeObject = entry.initiatorObject;
            }
            if (GUILayout.Button("Copy Stack", EditorStyles.miniButtonRight) && !string.IsNullOrEmpty(entry.initiatorStack))
            {
                EditorGUIUtility.systemCopyBuffer = entry.initiatorStack;
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(entry.initiatorStack))
            {
                EditorGUILayout.LabelField("Stack Trace:");
                EditorGUILayout.TextArea(entry.initiatorStack, GUILayout.MinHeight(60f));
            }

            EditorGUI.indentLevel--;
        }
        else
        {
            _expanded.Remove(entry.requestId);
        }
        EditorGUILayout.EndVertical();
    }

    private string BuildInitiatorLine(in UITweenMonitor.UITweenMonitorEntry entry)
    {
        var pieces = new List<string>(4);
        if (!string.IsNullOrEmpty(entry.initiatorType)) pieces.Add(entry.initiatorType);
        if (!string.IsNullOrEmpty(entry.initiatorName)) pieces.Add(entry.initiatorName);
        if (!string.IsNullOrEmpty(entry.initiatorDetails)) pieces.Add(entry.initiatorDetails);
        if (!string.IsNullOrEmpty(entry.initiatorMethod)) pieces.Add(entry.initiatorMethod);
        return string.Join(" · ", pieces);
    }
}
#endif
