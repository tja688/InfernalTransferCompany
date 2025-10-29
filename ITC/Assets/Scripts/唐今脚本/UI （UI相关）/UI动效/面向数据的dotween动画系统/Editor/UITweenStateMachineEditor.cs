#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UITweenStateMachine))]
public class UITweenStateMachineEditor : Editor
{
    private UITweenStateMachine _targetMachine;
    private SerializedProperty _panelConfigsProp;
    
    private GamePanelStateMachine _panelStateMachine;
    private string[] _allPanelOptions = Array.Empty<string>();
    private IReadOnlyList<UITweenPresetOption> _presetOptions;

    void OnEnable()
    {
        _targetMachine = (UITweenStateMachine)target;
        _panelConfigsProp = serializedObject.FindProperty("panelConfigurations");
        
        EditorApplication.update += FindAndCacheDependencies;
    }

    void OnDisable()
    {
        EditorApplication.update -= FindAndCacheDependencies;
    }

    private void FindAndCacheDependencies()
    {
        if (_panelStateMachine != null && Application.isPlaying) return;

        var stateMachine = FindObjectOfType<GamePanelStateMachine>();
        if (stateMachine != _panelStateMachine)
        {
            _panelStateMachine = stateMachine;
            RefreshPanelOptions();
            Repaint();
        }

        var player = _targetMachine.GetComponent<UITweenPlayer>();
        if (player != null)
        {
            _presetOptions = UITweenEditorUtility.GetPresetOptions(player);
        }
    }

    private void RefreshPanelOptions()
    {
        var library = _panelStateMachine?.PanelLibrary;
        _allPanelOptions = (library?.panelNames ?? new List<string>()).ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader();

        if (_panelStateMachine == null)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        // 绘制已有的配置
        for (int i = 0; i < _panelConfigsProp.arraySize; i++)
        {
            DrawPanelConfiguration(_panelConfigsProp.GetArrayElementAtIndex(i), i);
        }

        // 添加新配置的按钮
        DrawAddPanelConfigurationButton();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("全局面板状态机链接", EditorStyles.boldLabel);
        if (_panelStateMachine != null)
        {
            EditorGUILayout.HelpBox($"已链接到: {_panelStateMachine.name}", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("未在场景中找到 GamePanelStateMachine 实例。正在自动查找中...", MessageType.Warning);
        }
        EditorGUILayout.Space();
    }

    private void DrawPanelConfiguration(SerializedProperty configProp, int index)
    {
        var panelNameProp = configProp.FindPropertyRelative("panelName");
        var bindingsProp = configProp.FindPropertyRelative("stateBindings");

        using (new EditorGUILayout.VerticalScope("box"))
        {
            // --- 面板标题 (只读) ---
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent(panelNameProp.stringValue, "此面板的配置集"), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("删除此面板配置", GUILayout.Width(120)))
                {
                    // 添加删除确认，防止误操作
                    if (EditorUtility.DisplayDialog("确认删除", $"确定要删除对 '{panelNameProp.stringValue}' 面板的所有状态绑定配置吗？", "确定", "取消"))
                    {
                        _panelConfigsProp.DeleteArrayElementAtIndex(index);
                        return;
                    }
                }
            }
            
            EditorGUILayout.LabelField("状态绑定", EditorStyles.miniBoldLabel);
            
            // --- 状态绑定列表 ---
            for (int j = 0; j < bindingsProp.arraySize; j++)
            {
                DrawStateBinding(bindingsProp.GetArrayElementAtIndex(j), j, panelNameProp.stringValue);
            }

            if (GUILayout.Button("添加状态绑定"))
            {
                bindingsProp.arraySize++;
            }
        }
        EditorGUILayout.Space();
    }

    private void DrawAddPanelConfigurationButton()
    {
        var configuredPanels = new HashSet<string>();
        for (int i = 0; i < _panelConfigsProp.arraySize; i++)
        {
            configuredPanels.Add(_panelConfigsProp.GetArrayElementAtIndex(i).FindPropertyRelative("panelName").stringValue);
        }

        var unconfiguredPanels = _allPanelOptions.Where(p => !configuredPanels.Contains(p)).ToList();

        using (new EditorGUI.DisabledScope(unconfiguredPanels.Count == 0))
        {
            if (EditorGUILayout.DropdownButton(new GUIContent("  为面板添加配置..."), FocusType.Keyboard, GUILayout.Height(28)))
            {
                var menu = new GenericMenu();
                foreach (var panelName in unconfiguredPanels)
                {
                    menu.AddItem(new GUIContent(panelName), false, () => {
                        int newIndex = _panelConfigsProp.arraySize;
                        _panelConfigsProp.arraySize++;
                        var newConfigProp = _panelConfigsProp.GetArrayElementAtIndex(newIndex);
                        newConfigProp.FindPropertyRelative("panelName").stringValue = panelName;
                        newConfigProp.FindPropertyRelative("stateBindings").ClearArray();
                        serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }
        }
    }

    // DrawStateBinding, DrawPresetSelector, ShowPresetNameMenu 方法与之前相同，故省略以保持简洁
    // (实际实现中这些方法依然存在且被调用)
    private void DrawStateBinding(SerializedProperty bindingProp, int index, string panelName)
    {
        var stateProp = bindingProp.FindPropertyRelative("state");
        
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(stateProp, new GUIContent($"{panelName} - 状态 #{index + 1}"));
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    bindingProp.DeleteCommand();
                    return;
                }
            }

            EditorGUILayout.Space(2);
            
            EditorGUILayout.LabelField("动画预设", EditorStyles.boldLabel);
            DrawPresetSelector(bindingProp.FindPropertyRelative("onEnterPresetName"), new GUIContent("进入动画"));
            var reverseOnExitProp = bindingProp.FindPropertyRelative("reverseOnExit");
            EditorGUILayout.PropertyField(reverseOnExitProp, new GUIContent("退出时反向播放"));
            using (new EditorGUI.DisabledScope(reverseOnExitProp.boolValue))
            {
                DrawPresetSelector(bindingProp.FindPropertyRelative("onExitPresetName"), new GUIContent("退出动画"));
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("动画轨道 (可选)", EditorStyles.boldLabel);
            var playTrackProp = bindingProp.FindPropertyRelative("playTrackOnEnter");
            EditorGUILayout.PropertyField(playTrackProp, new GUIContent("启用轨道播放"));

            if (playTrackProp.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(bindingProp.FindPropertyRelative("onEnterTrack"), new GUIContent("进入轨道"));
                    EditorGUILayout.PropertyField(bindingProp.FindPropertyRelative("onEnterTrackName"), new GUIContent("轨道名称"));
                    
                    var reverseTrackProp = bindingProp.FindPropertyRelative("reverseTrackOnExit");
                    EditorGUILayout.PropertyField(reverseTrackProp, new GUIContent("退出时反向播放轨道"));

                    if (reverseTrackProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(bindingProp.FindPropertyRelative("onExitTrackReverseMode"), new GUIContent("反向模式"));
                    }
                }
            }
        }
    }

    private void DrawPresetSelector(SerializedProperty presetProp, GUIContent label)
    {
        if (presetProp == null) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(presetProp, label);
            using (new EditorGUI.DisabledScope(_presetOptions == null || _presetOptions.Count == 0))
            {
                if (GUILayout.Button("库中选择", GUILayout.Width(80f)))
                {
                    ShowPresetNameMenu(presetProp);
                }
            }
        }
    }

    private void ShowPresetNameMenu(SerializedProperty presetProp)
    {
        var menu = new GenericMenu();
        if (_presetOptions == null || _presetOptions.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("无可用预设"));
        }
        else
        {
            foreach (var option in _presetOptions)
            {
                var capturedName = option.Name;
                bool isCurrent = string.Equals(capturedName, presetProp.stringValue, StringComparison.Ordinal);
                menu.AddItem(new GUIContent(capturedName), isCurrent, () =>
                {
                    presetProp.stringValue = capturedName;
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("清空"), string.IsNullOrEmpty(presetProp.stringValue), () =>
            {
                presetProp.stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();
            });
        }
        menu.ShowAsContext();
    }
}
#endif
