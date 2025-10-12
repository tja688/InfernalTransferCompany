#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIStateMachine))]
public class UIStateMachineEditor : Editor
{
    SerializedProperty _stateAnimationsProp;
    SerializedProperty _startingStateProp;
    SerializedProperty _defaultProfileIdProp;
    SerializedProperty _additionalProfilesProp;
    SerializedProperty _layerProfilesProp;

    void OnEnable()
    {
        _stateAnimationsProp = serializedObject.FindProperty("stateAnimations");
        _startingStateProp = serializedObject.FindProperty("startingState");
        _defaultProfileIdProp = serializedObject.FindProperty("defaultProfileId");
        _additionalProfilesProp = serializedObject.FindProperty("additionalProfiles");
        _layerProfilesProp = serializedObject.FindProperty("layerProfiles");
    }

    public override void OnInspectorGUI()
    {
        if (serializedObject.isEditingMultipleObjects)
        {
            EditorGUILayout.HelpBox("暂不支持多对象同时编辑 UIStateMachine。", MessageType.Info);
            return;
        }

        serializedObject.Update();

        EditorGUILayout.PropertyField(_defaultProfileIdProp, new GUIContent("默认 Profile Id"));
        EditorGUILayout.PropertyField(_startingStateProp, new GUIContent("默认 Profile 初始状态"));
        EditorGUILayout.Space();

        DrawStateBindings();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_additionalProfilesProp, new GUIContent("额外 Profiles"), true);
        EditorGUILayout.PropertyField(_layerProfilesProp, new GUIContent("层级 Profile 映射"), true);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawStateBindings()
    {
        var machine = (UIStateMachine)target;
        var player = machine != null ? machine.GetComponent<UITweenPlayer>() : null;

        IReadOnlyList<UITweenPresetOption> options = UITweenEditorUtility.GetPresetOptions(player);
        if (player == null)
        {
            EditorGUILayout.HelpBox("未找到同物体上的 UITweenPlayer，库中预设下拉功能将不可用。", MessageType.Info);
        }
        else if (options.Count == 0)
        {
            EditorGUILayout.HelpBox("当前 UITweenPlayer 未关联任何预设或库，暂无可选项。", MessageType.Info);
        }

        EditorGUILayout.LabelField("默认 Profile 状态动画绑定", EditorStyles.boldLabel);

        if (_stateAnimationsProp == null)
        {
            EditorGUILayout.HelpBox("未找到状态动画数据。", MessageType.Warning);
            return;
        }

        for (int i = 0; i < _stateAnimationsProp.arraySize; i++)
        {
            var bindingProp = _stateAnimationsProp.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(bindingProp.FindPropertyRelative("state"), new GUIContent("状态"));
                    if (GUILayout.Button("删除", GUILayout.Width(50f)))
                    {
                        _stateAnimationsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                DrawPresetReferenceField(bindingProp.FindPropertyRelative("onEnterPreset"), new GUIContent("进入动画"), options);
                DrawPresetReferenceField(bindingProp.FindPropertyRelative("onExitPreset"), new GUIContent("退出动画"), options);
                EditorGUILayout.PropertyField(bindingProp.FindPropertyRelative("reverseOnExit"), new GUIContent("退出时反播"));
            }
        }

        if (GUILayout.Button("添加状态绑定"))
        {
            _stateAnimationsProp.arraySize++;
        }
    }

    void DrawPresetReferenceField(SerializedProperty presetProp, GUIContent label, IReadOnlyList<UITweenPresetOption> options)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(presetProp, label);
            using (new EditorGUI.DisabledScope(options == null || options.Count == 0))
            {
                if (GUILayout.Button("库中选择", GUILayout.Width(80f)))
                {
                    ShowPresetReferenceMenu(presetProp, options);
                }
            }
        }

        if (presetProp.objectReferenceValue is UITweenPreset preset)
        {
            string presetName = string.IsNullOrEmpty(preset.presetName) ? "<未命名>" : preset.presetName;
            EditorGUILayout.LabelField("Preset 名称", presetName);
        }
    }

    void ShowPresetReferenceMenu(SerializedProperty presetProp, IReadOnlyList<UITweenPresetOption> options)
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
                var capturedOption = option;
                bool isCurrent = presetProp.objectReferenceValue == capturedOption.Preset;
                menu.AddItem(new GUIContent(capturedOption.Name), isCurrent, () =>
                {
                    so.Update();
                    var targetProp = so.FindProperty(propertyPath);
                    if (targetProp != null)
                    {
                        targetProp.objectReferenceValue = capturedOption.Preset;
                        so.ApplyModifiedProperties();
                    }
                });
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("清除引用"), presetProp.objectReferenceValue == null, () =>
            {
                so.Update();
                var targetProp = so.FindProperty(propertyPath);
                if (targetProp != null)
                {
                    targetProp.objectReferenceValue = null;
                    so.ApplyModifiedProperties();
                }
            });
        }

        menu.ShowAsContext();
    }
}
#endif
