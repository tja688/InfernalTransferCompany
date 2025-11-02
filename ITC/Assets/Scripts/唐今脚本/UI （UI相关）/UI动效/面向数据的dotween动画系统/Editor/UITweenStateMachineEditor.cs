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
    private Dictionary<UITweenPlayer, IReadOnlyList<UITweenPresetOption>> _externalPlayerPresetOptions = new Dictionary<UITweenPlayer, IReadOnlyList<UITweenPresetOption>>();

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

        var stateMachine = FindFirstObjectByType<GamePanelStateMachine>();
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

        // 缓存所有外部Player的预设选项
        RefreshExternalPlayerPresetOptions();
    }

    private void RefreshExternalPlayerPresetOptions()
    {
        _externalPlayerPresetOptions.Clear();
        if (_targetMachine == null) return;

        // 遍历所有配置以查找外部Player
        foreach (var panelConfig in _targetMachine.panelConfigurations)
        {
            foreach (var binding in panelConfig.stateBindings)
            {
                foreach (var configItem in binding.configItems)
                {
                    if (configItem.itemType == UITweenStateMachine.ConfigItemType.ExternalPlayer && 
                        configItem.externalPlayerConfig != null && 
                        configItem.externalPlayerConfig.externalPlayer != null)
                    {
                        var externalPlayer = configItem.externalPlayerConfig.externalPlayer;
                        if (!_externalPlayerPresetOptions.ContainsKey(externalPlayer))
                        {
                            _externalPlayerPresetOptions[externalPlayer] = UITweenEditorUtility.GetPresetOptions(externalPlayer);
                        }
                    }
                }
            }
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

        DrawHeaderSection();

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

    private void DrawHeaderSection()
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

    private void DrawStateBinding(SerializedProperty bindingProp, int index, string panelName)
    {
        var stateProp = bindingProp.FindPropertyRelative("state");
        var configItemsProp = bindingProp.FindPropertyRelative("configItems");
        
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
            
            // 绘制配置项列表
            EditorGUILayout.LabelField("配置项", EditorStyles.boldLabel);
            for (int i = 0; i < configItemsProp.arraySize; i++)
            {
                DrawConfigItem(configItemsProp.GetArrayElementAtIndex(i), i, bindingProp);
            }

            // 添加配置项按钮
            DrawAddConfigItemButton(configItemsProp);
        }
    }

    private void DrawConfigItem(SerializedProperty configItemProp, int index, SerializedProperty bindingProp)
    {
        var itemTypeProp = configItemProp.FindPropertyRelative("itemType");
        var itemType = (UITweenStateMachine.ConfigItemType)itemTypeProp.enumValueIndex;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(GetConfigItemTypeLabel(itemType), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    configItemProp.DeleteCommand();
                    return;
                }
            }

            EditorGUILayout.Space(2);

            switch (itemType)
            {
                case UITweenStateMachine.ConfigItemType.Preset:
                    DrawPresetConfigItem(configItemProp);
                    break;
                case UITweenStateMachine.ConfigItemType.Track:
                    DrawTrackConfigItem(configItemProp);
                    break;
                case UITweenStateMachine.ConfigItemType.ExternalPlayer:
                    DrawExternalPlayerConfigItem(configItemProp);
                    break;
                case UITweenStateMachine.ConfigItemType.UnityEvent:
                    DrawUnityEventConfigItem(configItemProp);
                    break;
            }
        }
    }

    private void DrawPresetConfigItem(SerializedProperty configItemProp)
    {
        var presetConfigProp = configItemProp.FindPropertyRelative("presetConfig");
        if (presetConfigProp == null) return;

        DrawPresetSelector(presetConfigProp.FindPropertyRelative("onEnterPresetName"), new GUIContent("进入动画"));
        
        var reverseOnExitProp = presetConfigProp.FindPropertyRelative("reverseOnExit");
        EditorGUILayout.PropertyField(reverseOnExitProp, new GUIContent("退出时反向播放"));
        
        using (new EditorGUI.DisabledScope(reverseOnExitProp.boolValue))
        {
            DrawPresetSelector(presetConfigProp.FindPropertyRelative("onExitPresetName"), new GUIContent("退出动画"));
        }

        EditorGUILayout.PropertyField(presetConfigProp.FindPropertyRelative("onEnterBaselineMode"), new GUIContent("进入基线模式"));
        EditorGUILayout.PropertyField(presetConfigProp.FindPropertyRelative("onExitBaselineMode"), new GUIContent("退出基线模式"));
    }

    private void DrawTrackConfigItem(SerializedProperty configItemProp)
    {
        var trackConfigProp = configItemProp.FindPropertyRelative("trackConfig");
        if (trackConfigProp == null) return;

        EditorGUILayout.PropertyField(trackConfigProp.FindPropertyRelative("onEnterTrack"), new GUIContent("进入轨道"));
        EditorGUILayout.PropertyField(trackConfigProp.FindPropertyRelative("onEnterTrackName"), new GUIContent("轨道名称"));
        
        var reverseTrackProp = trackConfigProp.FindPropertyRelative("reverseTrackOnExit");
        EditorGUILayout.PropertyField(reverseTrackProp, new GUIContent("退出时反向播放轨道"));

        if (reverseTrackProp.boolValue)
        {
            EditorGUILayout.PropertyField(trackConfigProp.FindPropertyRelative("onExitTrackReverseMode"), new GUIContent("反向模式"));
        }
    }

    private void DrawExternalPlayerConfigItem(SerializedProperty configItemProp)
    {
        var externalPlayerConfigProp = configItemProp.FindPropertyRelative("externalPlayerConfig");
        if (externalPlayerConfigProp == null) return;

        var playerProp = externalPlayerConfigProp.FindPropertyRelative("externalPlayer");
        var oldPlayer = playerProp.objectReferenceValue as UITweenPlayer;
        EditorGUILayout.PropertyField(playerProp, new GUIContent("外部Player"));

        var externalPlayer = playerProp.objectReferenceValue as UITweenPlayer;
        
        // 如果Player改变了，更新缓存
        if (externalPlayer != oldPlayer && externalPlayer != null)
        {
            _externalPlayerPresetOptions[externalPlayer] = UITweenEditorUtility.GetPresetOptions(externalPlayer);
        }

        IReadOnlyList<UITweenPresetOption> options = null;
        if (externalPlayer != null)
        {
            if (!_externalPlayerPresetOptions.ContainsKey(externalPlayer))
            {
                _externalPlayerPresetOptions[externalPlayer] = UITweenEditorUtility.GetPresetOptions(externalPlayer);
            }
            options = _externalPlayerPresetOptions[externalPlayer];
        }

        DrawPresetSelectorWithOptions(externalPlayerConfigProp.FindPropertyRelative("onEnterPresetName"), new GUIContent("进入动画"), options);
        
        var reverseOnExitProp = externalPlayerConfigProp.FindPropertyRelative("reverseOnExit");
        EditorGUILayout.PropertyField(reverseOnExitProp, new GUIContent("退出时反向播放"));
        
        using (new EditorGUI.DisabledScope(reverseOnExitProp.boolValue))
        {
            DrawPresetSelectorWithOptions(externalPlayerConfigProp.FindPropertyRelative("onExitPresetName"), new GUIContent("退出动画"), options);
        }
    }

    private void DrawUnityEventConfigItem(SerializedProperty configItemProp)
    {
        var unityEventConfigProp = configItemProp.FindPropertyRelative("unityEventConfig");
        if (unityEventConfigProp == null) return;

        EditorGUILayout.PropertyField(unityEventConfigProp.FindPropertyRelative("onEnterEvent"), new GUIContent("进入事件"));
        EditorGUILayout.PropertyField(unityEventConfigProp.FindPropertyRelative("onExitEvent"), new GUIContent("退出事件"));
    }

    private void DrawAddConfigItemButton(SerializedProperty configItemsProp)
    {
        if (EditorGUILayout.DropdownButton(new GUIContent("  添加配置项..."), FocusType.Keyboard, GUILayout.Height(28)))
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("动画预设"), false, () => {
                AddConfigItem(configItemsProp, UITweenStateMachine.ConfigItemType.Preset);
            });
            menu.AddItem(new GUIContent("动画轨道"), false, () => {
                AddConfigItem(configItemsProp, UITweenStateMachine.ConfigItemType.Track);
            });
            menu.AddItem(new GUIContent("外部Player"), false, () => {
                AddConfigItem(configItemsProp, UITweenStateMachine.ConfigItemType.ExternalPlayer);
            });
            menu.AddItem(new GUIContent("Unity事件"), false, () => {
                AddConfigItem(configItemsProp, UITweenStateMachine.ConfigItemType.UnityEvent);
            });
            menu.ShowAsContext();
        }
    }

    private void AddConfigItem(SerializedProperty configItemsProp, UITweenStateMachine.ConfigItemType itemType)
    {
        // 先应用修改以确保索引正确
        serializedObject.ApplyModifiedProperties();
        
        // 获取对应的运行时绑定对象
        var bindingProp = configItemsProp.serializedObject.targetObject as UITweenStateMachine;
        if (bindingProp == null) return;
        
        // 找到对应的 binding（需要通过父级属性找到）
        // 由于 SerializedProperty 的限制，我们需要直接操作运行时对象
        var stateBinding = GetStateBindingFromProperty(configItemsProp);
        if (stateBinding != null)
        {
            var newConfigItem = new UITweenStateMachine.StateConfigItem(itemType);
            stateBinding.configItems.Add(newConfigItem);
            EditorUtility.SetDirty(bindingProp);
        }
        
        serializedObject.Update();
    }

    private UITweenStateMachine.UIStateBinding GetStateBindingFromProperty(SerializedProperty configItemsProp)
    {
        // 通过序列化路径找到对应的运行时对象
        var target = serializedObject.targetObject as UITweenStateMachine;
        if (target == null) return null;

        // 解析路径以找到对应的 binding
        // 路径格式类似: panelConfigurations.Array.data[0].stateBindings.Array.data[0].configItems
        var pathParts = configItemsProp.propertyPath.Split('.');
        
        // 查找 panelConfigurations 索引
        int panelIndex = -1;
        int bindingIndex = -1;
        
        for (int i = 0; i < pathParts.Length; i++)
        {
            if (pathParts[i] == "panelConfigurations" && i + 1 < pathParts.Length)
            {
                // 下一部分应该是 Array.data[index]
                if (i + 2 < pathParts.Length && pathParts[i + 1].StartsWith("Array"))
                {
                    var indexStr = pathParts[i + 2].Replace("data[", "").Replace("]", "");
                    if (int.TryParse(indexStr, out panelIndex))
                    {
                        // 继续查找 stateBindings
                        for (int j = i + 3; j < pathParts.Length; j++)
                        {
                            if (pathParts[j] == "stateBindings" && j + 1 < pathParts.Length)
                            {
                                if (j + 2 < pathParts.Length && pathParts[j + 1].StartsWith("Array"))
                                {
                                    var bindingIndexStr = pathParts[j + 2].Replace("data[", "").Replace("]", "");
                                    if (int.TryParse(bindingIndexStr, out bindingIndex))
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        if (panelIndex >= 0 && panelIndex < target.panelConfigurations.Count &&
            bindingIndex >= 0 && bindingIndex < target.panelConfigurations[panelIndex].stateBindings.Count)
        {
            return target.panelConfigurations[panelIndex].stateBindings[bindingIndex];
        }

        return null;
    }

    private string GetConfigItemTypeLabel(UITweenStateMachine.ConfigItemType itemType)
    {
        switch (itemType)
        {
            case UITweenStateMachine.ConfigItemType.Preset:
                return "动画预设";
            case UITweenStateMachine.ConfigItemType.Track:
                return "动画轨道";
            case UITweenStateMachine.ConfigItemType.ExternalPlayer:
                return "外部Player";
            case UITweenStateMachine.ConfigItemType.UnityEvent:
                return "Unity事件";
            default:
                return "未知类型";
        }
    }

    private void DrawPresetSelector(SerializedProperty presetProp, GUIContent label)
    {
        DrawPresetSelectorWithOptions(presetProp, label, _presetOptions);
    }

    private void DrawPresetSelectorWithOptions(SerializedProperty presetProp, GUIContent label, IReadOnlyList<UITweenPresetOption> options)
    {
        if (presetProp == null) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(presetProp, label);
            using (new EditorGUI.DisabledScope(options == null || options.Count == 0))
            {
                if (GUILayout.Button("库中选择", GUILayout.Width(80f)))
                {
                    ShowPresetNameMenu(presetProp, options);
                }
            }
        }
    }

    private void ShowPresetNameMenu(SerializedProperty presetProp, IReadOnlyList<UITweenPresetOption> options)
    {
        var menu = new GenericMenu();
        if (options == null || options.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("无可用预设"));
        }
        else
        {
            foreach (var option in options)
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
