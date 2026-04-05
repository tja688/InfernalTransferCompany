#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModeProjectSafetyGuard
{
    private static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
    private static readonly Type ProjectBrowserType = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");

    private static double s_LastBlockedDeleteLogTime;

    static PlayModeProjectSafetyGuard()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        var selectedAssets = Selection.objects?
            .Where(AssetDatabase.Contains)
            .ToArray() ?? Array.Empty<UnityEngine.Object>();

        if (selectedAssets.Length > 0)
        {
            var assetList = string.Join("\n", selectedAssets.Select(AssetDatabase.GetAssetPath));
            Debug.LogWarning(
                "[PlayModeProjectSafetyGuard] Cleared selected Project assets before Play to avoid accidental deletion.\n" +
                assetList);

            Selection.objects = Array.Empty<UnityEngine.Object>();
        }

        FocusGameViewIfProjectWindowWasFocused();
    }

    private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        var current = Event.current;
        if (current == null)
        {
            return;
        }

        if (current.type != EventType.ValidateCommand && current.type != EventType.ExecuteCommand)
        {
            return;
        }

        if (current.commandName != "Delete" && current.commandName != "SoftDelete")
        {
            return;
        }

        if (current.type == EventType.ExecuteCommand)
        {
            LogBlockedDelete();
        }

        current.Use();
    }

    private static void LogBlockedDelete()
    {
        var now = EditorApplication.timeSinceStartup;
        if (now - s_LastBlockedDeleteLogTime < 0.25d)
        {
            return;
        }

        s_LastBlockedDeleteLogTime = now;

        var selectedAssets = Selection.objects?
            .Where(AssetDatabase.Contains)
            .Select(AssetDatabase.GetAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .ToArray() ?? Array.Empty<string>();

        var focusedWindow = EditorWindow.focusedWindow != null
            ? EditorWindow.focusedWindow.GetType().FullName
            : "<none>";

        var selectionText = selectedAssets.Length > 0
            ? string.Join("\n", selectedAssets)
            : "<no asset selection>";

        Debug.LogWarning(
            "[PlayModeProjectSafetyGuard] Blocked a Project delete command during Play.\n" +
            $"FocusedWindow: {focusedWindow}\n" +
            $"SelectedAssets:\n{selectionText}");
    }

    private static void FocusGameViewIfProjectWindowWasFocused()
    {
        if (GameViewType == null)
        {
            return;
        }

        var focusedWindow = EditorWindow.focusedWindow;
        if (focusedWindow == null || ProjectBrowserType == null || focusedWindow.GetType() != ProjectBrowserType)
        {
            return;
        }

        EditorWindow.GetWindow(GameViewType)?.Focus();
    }
}
#endif
