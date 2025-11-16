#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PanelTransitionFilter))]
public class PanelTransitionFilterEditor : Editor
{
    private const int kMaxMaskOptionCount = 31;

    private SerializedProperty _panelLibraryProp;
    private SerializedProperty _currentPanelSelectionsProp;
    private SerializedProperty _targetPanelSelectionsProp;
    private SerializedProperty _legacyCurrentPanelProp;
    private SerializedProperty _legacyTargetPanelProp;
    private SerializedProperty _panelChangedEventProp;
    private SerializedProperty _onMatchSuccessProp;
    private string[] _panelOptions = new string[0];
    private string[] _panelMaskOptions = new string[0];

    private void OnEnable()
    {
        _panelLibraryProp = serializedObject.FindProperty("_panelLibrary");
        _currentPanelSelectionsProp = serializedObject.FindProperty("_currentPanelSelections");
        _targetPanelSelectionsProp = serializedObject.FindProperty("_targetPanelSelections");
        _legacyCurrentPanelProp = serializedObject.FindProperty("_legacyCurrentPanel");
        _legacyTargetPanelProp = serializedObject.FindProperty("_legacyTargetPanel");
        _panelChangedEventProp = serializedObject.FindProperty("_panelChangedEvent");
        _onMatchSuccessProp = serializedObject.FindProperty("_onMatchSuccess");
        RefreshPanelOptions();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("面板切换过滤器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 显示面板库字段
        EditorGUILayout.PropertyField(_panelLibraryProp);

        // 当库发生变化时，刷新选项
        if (serializedObject.ApplyModifiedProperties())
        {
            RefreshPanelOptions();
        }
        
        serializedObject.Update();

        // 显示警告（如果没有面板库）
        if (_panelOptions.Length <= 1)
        {
            EditorGUILayout.HelpBox("请先关联一个有效的 GamePanelLibrarySO，并在其中添加面板名称。", MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("过滤配置", EditorStyles.boldLabel);
        
        DrawPanelMaskSelector(_currentPanelSelectionsProp, _legacyCurrentPanelProp, "允许的来源面板");
        DrawPanelMaskSelector(_targetPanelSelectionsProp, _legacyTargetPanelProp, "允许的目标面板");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("事件配置", EditorStyles.boldLabel);
        
        // 面板切换事件
        EditorGUILayout.PropertyField(_panelChangedEventProp, new GUIContent("面板切换事件"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("输出事件", EditorStyles.boldLabel);
        
        // UnityEvent
        EditorGUILayout.PropertyField(_onMatchSuccessProp, new GUIContent("匹配成功时触发"));

        // 运行时信息显示
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                var filter = (PanelTransitionFilter)target;
                var panelManager = PanelManager.Instance;
                if (panelManager != null)
                {
                    EditorGUILayout.TextField("实际当前面板", panelManager.CurrentPanel);
                }
                else
                {
                    EditorGUILayout.TextField("实际当前面板", "PanelManager 未找到");
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPanelMaskSelector(SerializedProperty selectionProp, SerializedProperty legacyProp, string label)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        if (selectionProp == null)
        {
            EditorGUILayout.HelpBox("未找到序列化的面板集合字段。", MessageType.Error);
            EditorGUILayout.EndVertical();
            return;
        }

        if (legacyProp == null)
        {
            EditorGUILayout.HelpBox("未找到旧版本面板字段。", MessageType.Error);
            EditorGUILayout.EndVertical();
            return;
        }

        MigrateLegacySelection(selectionProp, legacyProp);

        if (_panelMaskOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("当前没有可用的面板选项，无法配置过滤。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        using (new EditorGUI.DisabledScope(_panelMaskOptions.Length <= 0))
        {
            if (_panelMaskOptions.Length <= kMaxMaskOptionCount)
            {
                DrawMaskField(selectionProp);
            }
            else
            {
                EditorGUILayout.HelpBox("面板数量超过 31，将使用逐条勾选模式。", MessageType.Warning);
                DrawToggleList(selectionProp);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选"))
            {
                SelectAll(selectionProp);
            }
            if (GUILayout.Button("清空（通配）"))
            {
                ClearSelection(selectionProp);
            }
            EditorGUILayout.EndHorizontal();

            DrawSelectionSummary(selectionProp);
            if (selectionProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("当前未选择任何面板，表示接受任意面板。", MessageType.None);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawMaskField(SerializedProperty selectionProp)
    {
        int currentMask = BuildMaskFromSelection(selectionProp);
        EditorGUI.BeginChangeCheck();
        int newMask = EditorGUILayout.MaskField("面板集合", currentMask, _panelMaskOptions);
        if (EditorGUI.EndChangeCheck())
        {
            ApplyMaskToSelection(selectionProp, newMask);
        }
    }

    private void DrawToggleList(SerializedProperty selectionProp)
    {
        var selected = BuildSelectedSet(selectionProp);
        for (int i = 0; i < _panelMaskOptions.Length; i++)
        {
            string option = _panelMaskOptions[i];
            bool isSelected = selected.Contains(option);
            bool newValue = EditorGUILayout.ToggleLeft(option, isSelected);
            if (newValue != isSelected)
            {
                if (newValue)
                {
                    AddPanel(selectionProp, option);
                }
                else
                {
                    RemovePanel(selectionProp, option);
                }
            }
        }
    }

    private void DrawSelectionSummary(SerializedProperty selectionProp)
    {
        if (selectionProp.arraySize == 0)
        {
            return;
        }

        var summary = new System.Text.StringBuilder();
        for (int i = 0; i < selectionProp.arraySize; i++)
        {
            if (i > 0) summary.Append("，");
            summary.Append(selectionProp.GetArrayElementAtIndex(i).stringValue);
        }

        EditorGUILayout.HelpBox($"已选择：{summary}", MessageType.None);
    }

    private int BuildMaskFromSelection(SerializedProperty selectionProp)
    {
        int mask = 0;
        for (int i = 0; i < selectionProp.arraySize; i++)
        {
            string panelName = selectionProp.GetArrayElementAtIndex(i).stringValue;
            int optionIndex = System.Array.IndexOf(_panelMaskOptions, panelName);
            if (optionIndex >= 0)
            {
                mask |= 1 << optionIndex;
            }
        }
        return mask;
    }

    private void ApplyMaskToSelection(SerializedProperty selectionProp, int mask)
    {
        ClearSelection(selectionProp);

        for (int i = 0; i < _panelMaskOptions.Length; i++)
        {
            bool isSet = (mask & (1 << i)) != 0;
            if (isSet)
            {
                AddPanel(selectionProp, _panelMaskOptions[i]);
            }
        }
    }

    private void SelectAll(SerializedProperty selectionProp)
    {
        ClearSelection(selectionProp);
        for (int i = 0; i < _panelMaskOptions.Length; i++)
        {
            AddPanel(selectionProp, _panelMaskOptions[i]);
        }
    }

    private void ClearSelection(SerializedProperty selectionProp)
    {
        for (int i = selectionProp.arraySize - 1; i >= 0; i--)
        {
            selectionProp.DeleteArrayElementAtIndex(i);
        }
    }

    private void AddPanel(SerializedProperty selectionProp, string panelName)
    {
        if (string.IsNullOrEmpty(panelName) || panelName == "None" || IsPanelSelected(selectionProp, panelName))
        {
            return;
        }

        int newIndex = selectionProp.arraySize;
        selectionProp.arraySize++;
        selectionProp.GetArrayElementAtIndex(newIndex).stringValue = panelName;
    }

    private void RemovePanel(SerializedProperty selectionProp, string panelName)
    {
        for (int i = selectionProp.arraySize - 1; i >= 0; i--)
        {
            var element = selectionProp.GetArrayElementAtIndex(i);
            if (element.stringValue == panelName)
            {
                selectionProp.DeleteArrayElementAtIndex(i);
            }
        }
    }

    private bool IsPanelSelected(SerializedProperty selectionProp, string panelName)
    {
        for (int i = 0; i < selectionProp.arraySize; i++)
        {
            if (selectionProp.GetArrayElementAtIndex(i).stringValue == panelName)
            {
                return true;
            }
        }
        return false;
    }

    private HashSet<string> BuildSelectedSet(SerializedProperty selectionProp)
    {
        var set = new HashSet<string>(System.StringComparer.Ordinal);
        for (int i = 0; i < selectionProp.arraySize; i++)
        {
            var value = selectionProp.GetArrayElementAtIndex(i).stringValue;
            if (!string.IsNullOrEmpty(value) && value != "None")
            {
                set.Add(value);
            }
        }
        return set;
    }

    private void MigrateLegacySelection(SerializedProperty selectionProp, SerializedProperty legacyProp)
    {
        if (selectionProp.arraySize > 0)
        {
            return;
        }

        var legacyValue = legacyProp.stringValue;
        if (string.IsNullOrEmpty(legacyValue) || legacyValue == "None")
        {
            return;
        }

        AddPanel(selectionProp, legacyValue);
        legacyProp.stringValue = "None";
    }

    private void RefreshPanelOptions()
    {
        var filter = (PanelTransitionFilter)target;
        
        // 尝试从序列化属性获取面板库
        GamePanelLibrarySO library = null;
        if (_panelLibraryProp != null && _panelLibraryProp.objectReferenceValue != null)
        {
            library = _panelLibraryProp.objectReferenceValue as GamePanelLibrarySO;
        }
        
        // 如果序列化属性中没有，尝试从 PanelManager 获取
        if (library == null && PanelManager.Instance != null)
        {
            var panelManagerType = typeof(PanelManager);
            var fieldInfo = panelManagerType.GetField("_panelLibrary", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fieldInfo != null)
            {
                library = fieldInfo.GetValue(PanelManager.Instance) as GamePanelLibrarySO;
            }
        }

        if (library != null && library.panelNames != null)
        {
            // 确保 "None" 总是第一个选项
            var optionsList = new System.Collections.Generic.List<string>();
            optionsList.Add("None");
            foreach (var panelName in library.panelNames)
            {
                if (!string.IsNullOrEmpty(panelName) && panelName != "None")
                {
                    optionsList.Add(panelName);
                }
            }
            _panelOptions = optionsList.ToArray();
            _panelMaskOptions = optionsList.FindAll(option => option != "None").ToArray();
        }
        else
        {
            _panelOptions = new string[] { "None" };
            _panelMaskOptions = System.Array.Empty<string>();
        }
    }
}
#endif

