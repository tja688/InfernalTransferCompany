#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UITweenStateMachine))]
public class UITweenStateMachineEditor : Editor
{
    private SerializedProperty _stateAnimationsProp;
    private UITweenStateMachine _targetMachine;
    private UITweenPlayer _linkedPlayer;
    private IReadOnlyList<UITweenPresetOption> _presetOptions;

    void OnEnable()
    {
        _targetMachine = (UITweenStateMachine)target;
        _linkedPlayer = _targetMachine.GetComponent<UITweenPlayer>();
        _stateAnimationsProp = serializedObject.FindProperty("stateAnimations");
        _presetOptions = UITweenEditorUtility.GetPresetOptions(_linkedPlayer);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (_linkedPlayer == null)
        {
            EditorGUILayout.HelpBox("錯誤：未在同對象上找到 UITweenPlayer 組件。", MessageType.Error);
            return;
        }

        if (_presetOptions.Count == 0)
        {
            EditorGUILayout.HelpBox("提示：關聯的 UITweenPlayer 中沒有可用的動畫預設。", MessageType.Info);
        }

        EditorGUILayout.LabelField("狀態動畫綁定", EditorStyles.boldLabel);

        for (int i = 0; i < _stateAnimationsProp.arraySize; i++)
        {
            var bindingProp = _stateAnimationsProp.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(bindingProp.FindPropertyRelative("state"), new GUIContent("狀態"));
                    if (GUILayout.Button("刪除", GUILayout.Width(50f)))
                    {
                        _stateAnimationsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
                
                // --- 修改後的繪製邏輯 ---
                
                DrawPresetNameSelector(bindingProp.FindPropertyRelative("onEnterPresetName"), new GUIContent("進入動畫名稱"));

                var reverseOnExitProp = bindingProp.FindPropertyRelative("reverseOnExit");
                EditorGUILayout.PropertyField(reverseOnExitProp, new GUIContent("退出時倒播進入動畫"));

                // 當 reverseOnExit 被勾選時，禁用 onExitPresetName 字段
                using (new EditorGUI.DisabledScope(reverseOnExitProp.boolValue))
                {
                    DrawPresetNameSelector(bindingProp.FindPropertyRelative("onExitPresetName"), new GUIContent("退出動畫名稱"));
                }
            }
        }

        if (GUILayout.Button("添加狀態綁定", GUILayout.Height(25)))
        {
            _stateAnimationsProp.arraySize++;
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawPresetNameSelector(SerializedProperty presetProp, GUIContent label)
    {
        if (presetProp == null) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(presetProp, label);
            using (new EditorGUI.DisabledScope(_presetOptions == null || _presetOptions.Count == 0))
            {
                if (GUILayout.Button("庫中選擇", GUILayout.Width(80f)))
                {
                    ShowPresetNameMenu(presetProp);
                }
            }
        }
    }

    void ShowPresetNameMenu(SerializedProperty presetProp)
    {
        var menu = new GenericMenu();
        if (_presetOptions == null || _presetOptions.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("無可用預設"));
        }
        else
        {
            var so = serializedObject;
            var propertyPath = presetProp.propertyPath;
            foreach (var option in _presetOptions)
            {
                var capturedName = option.Name;
                bool isCurrent = string.Equals(capturedName, presetProp.stringValue, StringComparison.Ordinal);
                menu.AddItem(new GUIContent(capturedName), isCurrent, () =>
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
            menu.AddItem(new GUIContent("清空名稱"), string.IsNullOrEmpty(presetProp.stringValue), () =>
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
}
#endif