#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UITweenTrack))]
public class UITweenTrackEditor : Editor
{
    SerializedProperty _tracksProp;
    SerializedProperty _useUnscaledProp;
    SerializedProperty _playFlowProp; // 新增

    // —— Editor-only: 测试区状态 —— //
    int _testTrackIndex = 0;
    GUIContent[] _trackNameOptions = Array.Empty<GUIContent>();

    void OnEnable()
    {
        _tracksProp = serializedObject.FindProperty("tracks");
        _useUnscaledProp = serializedObject.FindProperty("useUnscaledIntervals");
        _playFlowProp = serializedObject.FindProperty("playFlow"); // 新增
        RefreshTrackNameOptions();
    }

    public override void OnInspectorGUI()
    {
        if (serializedObject.isEditingMultipleObjects)
        {
            EditorGUILayout.HelpBox("暂不支持多对象同时编辑 UITweenTrack。", MessageType.Info);
            return;
        }

        serializedObject.Update();

        EditorGUILayout.PropertyField(_useUnscaledProp);
        EditorGUILayout.Space();

        // —— 运行测试（Play 模式） —— //
        DrawTestRunner();

        EditorGUILayout.Space(8);
        DrawTracks();

        serializedObject.ApplyModifiedProperties();

        // 便于编辑时动态刷新选项
        if (Event.current.type == EventType.Layout)
            RefreshTrackNameOptions();
    }

    void DrawTestRunner()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("运行测试（Play 模式）", EditorStyles.boldLabel);

            var track = (UITweenTrack)target;
            int trackCount = _tracksProp?.arraySize ?? 0;

            // 新增：播放流程控制
            EditorGUILayout.PropertyField(_playFlowProp, new GUIContent("播放流程"));
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(trackCount == 0))
            {
                _testTrackIndex = EditorGUILayout.Popup(
                    new GUIContent("测试轨道"),
                    Mathf.Clamp(_testTrackIndex, 0, Mathf.Max(0, trackCount - 1)),
                    _trackNameOptions);

                if (trackCount == 0)
                {
                    EditorGUILayout.HelpBox("尚未添加任何轨道。", MessageType.Info);
                }
            }


            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play 模式后才能运行测试。", MessageType.None);
            }

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || trackCount == 0))
            {
                // 正向播放
                if (GUILayout.Button("▶ 运行正向轨道", GUILayout.Height(28)))
                {
                    Undo.RecordObject(track, "Run Test Track Forward");
                    track.PlayTrack(_testTrackIndex);
                }

                // 反向播放按钮
                EditorGUILayout.LabelField("反向播放模式", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("◀ 默认反向", GUILayout.Height(24)))
                    {
                        Undo.RecordObject(track, "Run Test Track Reverse Default");
                        track.PlayTrackReverse(_testTrackIndex, UITweenTrack.ReversePlayMode.Default);
                    }

                    if (GUILayout.Button("◀ 正序反向", GUILayout.Height(24)))
                    {
                        Undo.RecordObject(track, "Run Test Track Reverse Forward Order");
                        track.PlayTrackReverse(_testTrackIndex, UITweenTrack.ReversePlayMode.ForwardOrderReverse);
                    }

                    if (GUILayout.Button("⏪ 快速退场", GUILayout.Height(24)))
                    {
                        Undo.RecordObject(track, "Run Test Track Quick Exit");
                        track.PlayTrackReverse(_testTrackIndex, UITweenTrack.ReversePlayMode.QuickExit);
                    }
                }
                
                // 停止按钮
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("⏹ 停止该轨道", GUILayout.Height(22)))
                    {
                        track.StopTrack(_testTrackIndex);
                    }

                    if (GUILayout.Button("⏹ 全部停止", GUILayout.Height(22)))
                    {
                        track.StopAllTracks();
                    }
                }
            }
        }
    }

    void DrawTracks()
    {
        EditorGUILayout.LabelField("轨道设置", EditorStyles.boldLabel);
        if (_tracksProp == null)
        {
            EditorGUILayout.HelpBox("未找到轨道数据。", MessageType.Warning);
            return;
        }

        for (int i = 0; i < _tracksProp.arraySize; i++)
        {
            var trackProp = _tracksProp.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(trackProp.FindPropertyRelative("trackName"), new GUIContent("轨道名称"));

                    // 便捷测试按钮（Play 模式单轨）
                    using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                    {
                        if (GUILayout.Button("▶ 测这个", GUILayout.Width(70)))
                        {
                            var track = (UITweenTrack)target;
                            _testTrackIndex = i;
                            track.PlayTrack(i);
                        }
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(50f)))
                    {
                        _tracksProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                var intervalProp = trackProp.FindPropertyRelative("uniformInterval");
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(intervalProp, new GUIContent("统一间隔"));
                    if (GUILayout.Button("应用到轨道", GUILayout.Width(100f)))
                    {
                        ApplyUniformInterval(i, intervalProp.floatValue);
                    }
                }

                DrawTrackItems(trackProp, i);
            }
        }

        if (GUILayout.Button("添加新轨道"))
        {
            _tracksProp.arraySize++;
        }
    }

    void DrawTrackItems(SerializedProperty trackProp, int trackIndex)
    {
        var itemsProp = trackProp.FindPropertyRelative("items");
        if (itemsProp == null)
        {
            EditorGUILayout.HelpBox("轨道元素数据缺失。", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("轨道元素", EditorStyles.boldLabel);

        for (int j = 0; j < itemsProp.arraySize; j++)
        {
            var itemProp = itemsProp.GetArrayElementAtIndex(j);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"元素 {j + 1}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(j <= 0))
                    {
                        if (GUILayout.Button("↑", GUILayout.Width(24f)))
                        {
                            itemsProp.MoveArrayElement(j, j - 1);
                            break;
                        }
                    }

                    using (new EditorGUI.DisabledScope(j >= itemsProp.arraySize - 1))
                    {
                        if (GUILayout.Button("↓", GUILayout.Width(24f)))
                        {
                            itemsProp.MoveArrayElement(j, j + 1);
                            break;
                        }
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(50f)))
                    {
                        itemsProp.DeleteArrayElementAtIndex(j);
                        break;
                    }
                }

                var playerProp = itemProp.FindPropertyRelative("player");
                EditorGUILayout.PropertyField(playerProp, new GUIContent("UI Tween Player"));

                var player = playerProp.objectReferenceValue as UITweenPlayer;
                IReadOnlyList<UITweenPresetOption> options = UITweenEditorUtility.GetPresetOptions(player);

                DrawPresetNameSelector(itemProp.FindPropertyRelative("presetName"), options);
                EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("delayAfterPlay"), new GUIContent("播放间隔"));
            }
        }

        if (GUILayout.Button("添加元素"))
        {
            itemsProp.arraySize++;
        }
    }

    void DrawPresetNameSelector(SerializedProperty presetProp, IReadOnlyList<UITweenPresetOption> options)
    {
        if (presetProp == null) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(presetProp, new GUIContent("播放名称"));
            using (new EditorGUI.DisabledScope(options == null || options.Count == 0))
            {
                if (GUILayout.Button("库中选择", GUILayout.Width(80f)))
                {
                    ShowPresetNameMenu(presetProp, options);
                }
            }
        }
    }

    void ShowPresetNameMenu(SerializedProperty presetProp, IReadOnlyList<UITweenPresetOption> options)
    {
        var menu = new GenericMenu();
        if (options == null || options.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("无可用预设"));
        }
        else
        {
            var so = serializedObject;
            var propertyPath = presetProp.propertyPath;
            foreach (var option in options)
            {
                var capturedName = option.Name;
                menu.AddItem(new GUIContent(capturedName), string.Equals(capturedName, presetProp.stringValue, StringComparison.Ordinal), () =>
                {
                    so.Update();
                    var targetProp = so.FindProperty(propertyPath);
                    if (targetProp != null)
                    {
                        targetProp.stringValue = capturedName;
                        so.ApplyModifiedProperties();
                    }
                });
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("清空名称"), string.IsNullOrEmpty(presetProp.stringValue), () =>
            {
                so.Update();
                var targetProp = so.FindProperty(propertyPath);
                if (targetProp != null)
                {
                    targetProp.stringValue = string.Empty;
                    so.ApplyModifiedProperties();
                }
            });
        }

        menu.ShowAsContext();
    }

    void ApplyUniformInterval(int trackIndex, float interval)
    {
        foreach (var obj in targets)
        {
            if (obj is UITweenTrack track)
            {
                Undo.RecordObject(track, "Apply Uniform Interval");
                track.ApplyUniformInterval(trackIndex, interval);
                EditorUtility.SetDirty(track);
            }
        }
    }

    void RefreshTrackNameOptions()
    {
        var track = target as UITweenTrack;
        if (track == null || track.tracks == null || track.tracks.Count == 0)
        {
            _trackNameOptions = new[] { new GUIContent("(无轨道)") };
            _testTrackIndex = 0;
            return;
        }

        var list = new List<GUIContent>();
        for (int i = 0; i < track.tracks.Count; i++)
        {
            var t = track.tracks[i];
            var name = string.IsNullOrEmpty(t?.trackName) ? $"Track {i}" : t.trackName;
            list.Add(new GUIContent($"{i}. {name}"));
        }
        _trackNameOptions = list.ToArray();
        _testTrackIndex = Mathf.Clamp(_testTrackIndex, 0, _trackNameOptions.Length - 1);
    }
}
#endif
