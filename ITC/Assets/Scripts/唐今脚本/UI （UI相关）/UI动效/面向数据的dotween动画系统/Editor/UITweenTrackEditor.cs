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

    void OnEnable()
    {
        _tracksProp = serializedObject.FindProperty("tracks");
        _useUnscaledProp = serializedObject.FindProperty("useUnscaledIntervals");
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

        DrawTracks();

        serializedObject.ApplyModifiedProperties();
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
        if (presetProp == null)
        {
            return;
        }

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
}
#endif
