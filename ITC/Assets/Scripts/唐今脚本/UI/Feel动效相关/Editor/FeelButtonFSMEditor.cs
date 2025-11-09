#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(FeelButtonFSM))]
public class FeelButtonFSMEditor : Editor
{
    private SerializedProperty _raycastControlEventProp;
    private SerializedProperty _panelChangedEventProp;
    private SerializedProperty _usePanelSpecificPresetsProp;
    private SerializedProperty _panelLibraryProp;
    private SerializedProperty _panelPresetsProp;

    private ReorderableList _presetList;

    private void OnEnable()
    {
        _raycastControlEventProp = serializedObject.FindProperty("_raycastControlEvent");
        _panelChangedEventProp = serializedObject.FindProperty("_panelChangedEvent");
        _usePanelSpecificPresetsProp = serializedObject.FindProperty("_usePanelSpecificPresets");
        _panelLibraryProp = serializedObject.FindProperty("_panelLibrary");
        _panelPresetsProp = serializedObject.FindProperty("_panelPresets");

        // 自动查找面板库
        AutoFindPanelLibrary();

        _presetList = new ReorderableList(serializedObject, _panelPresetsProp, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "面板专属动效映射（未匹配时无动效）"),
            drawElementCallback = DrawPresetElement,
            elementHeightCallback = index => GetPresetElementHeight()
        };

        _presetList.onAddCallback = list =>
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.InsertArrayElementAtIndex(index);
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("panelName").stringValue = string.Empty;
            element.FindPropertyRelative("hoverFeedback").objectReferenceValue = null;
            element.FindPropertyRelative("idleFeedback").objectReferenceValue = null;
        };
    }

    /// <summary>
    /// 自动查找场景中的 PanelManager 并获取其面板库。
    /// </summary>
    private void AutoFindPanelLibrary()
    {
        if (_panelLibraryProp.objectReferenceValue != null)
        {
            return; // 已手动配置，不自动查找
        }

        // 在编辑器中查找 PanelManager
        PanelManager panelManager = FindObjectOfType<PanelManager>();
        if (panelManager != null)
        {
            // 通过反射获取 PanelManager 的 _panelLibrary 字段
            var panelManagerType = typeof(PanelManager);
            var fieldInfo = panelManagerType.GetField("_panelLibrary", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fieldInfo != null)
            {
                var library = fieldInfo.GetValue(panelManager) as GamePanelLibrarySO;
                if (library != null)
                {
                    _panelLibraryProp.objectReferenceValue = library;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 获取通用动效字段属性（用于显示警告）
        SerializedProperty hoverFeedbackProp = serializedObject.FindProperty("hoverFeedback");
        SerializedProperty idleFeedbackProp = serializedObject.FindProperty("idleFeedback");

        // 绘制除特定字段外的所有属性
        DrawPropertiesExcluding(serializedObject, "hoverFeedback", "idleFeedback", "_raycastControlEvent", "_panelChangedEvent", "_usePanelSpecificPresets", "_panelLibrary", "_panelPresets");

        // 显示已弃用的通用动效字段（灰色显示，表示不再使用）
        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.HelpBox("通用动效字段（hoverFeedback / idleFeedback）已弃用，现在不再使用。请通过面板专属动效预设配置动效。", MessageType.Info);
        EditorGUILayout.PropertyField(hoverFeedbackProp);
        EditorGUILayout.PropertyField(idleFeedbackProp);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_raycastControlEventProp);
        EditorGUILayout.PropertyField(_panelChangedEventProp);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_usePanelSpecificPresetsProp);
        
        if (_usePanelSpecificPresetsProp.boolValue)
        {
            EditorGUI.indentLevel++;
            
            // 显示面板库字段，并提供自动查找按钮
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_panelLibraryProp);
            if (_panelLibraryProp.objectReferenceValue == null)
            {
                if (GUILayout.Button("自动查找", GUILayout.Width(70)))
                {
                    AutoFindPanelLibrary();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (_panelLibraryProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("未指定面板库，将通过手动输入设置面板名称。", MessageType.Info);
            }

            if (_panelPresetsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("⚠️ 未创建任何预设！按钮将无任何鼠标响应动效。请添加至少一个面板预设并配置动效。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("✓ 已创建预设。按钮将根据当前面板匹配预设中的动效。未匹配到预设时无动效反馈。", MessageType.Info);
            }

            _presetList.DoLayoutList();
            EditorGUI.indentLevel--;
        }
        else
        {
            if (_panelPresetsProp.arraySize > 0)
            {
                EditorGUILayout.HelpBox("面板专属动效预设已禁用，列表内容当前不会生效。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ 未启用面板专属动效预设，且未创建任何预设。按钮将无任何鼠标响应动效。", MessageType.Warning);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPresetElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = _panelPresetsProp.GetArrayElementAtIndex(index);
        SerializedProperty panelNameProp = element.FindPropertyRelative("panelName");
        SerializedProperty hoverProp = element.FindPropertyRelative("hoverFeedback");
        SerializedProperty idleProp = element.FindPropertyRelative("idleFeedback");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect panelRect = new Rect(rect.x, rect.y + 2f, rect.width, lineHeight);

        string[] panelOptions = GetPanelOptions(out int selectedIndex, panelNameProp.stringValue);
        if (panelOptions != null && panelOptions.Length > 0)
        {
            int newIndex = EditorGUI.Popup(panelRect, "面板", selectedIndex, panelOptions);
            if (newIndex >= 0 && newIndex < panelOptions.Length)
            {
                panelNameProp.stringValue = panelOptions[newIndex];
            }
        }
        else
        {
            panelNameProp.stringValue = EditorGUI.TextField(panelRect, "面板", panelNameProp.stringValue);
        }

        Rect hoverRect = new Rect(rect.x, panelRect.y + lineHeight + spacing, rect.width, lineHeight);
        EditorGUI.PropertyField(hoverRect, hoverProp, new GUIContent("Hover 动效"));

        Rect idleRect = new Rect(rect.x, hoverRect.y + lineHeight + spacing, rect.width, lineHeight);
        EditorGUI.PropertyField(idleRect, idleProp, new GUIContent("Idle 动效"));
    }

    private float GetPresetElementHeight()
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        return lineHeight * 3f + spacing * 3f + 6f;
    }

    private string[] GetPanelOptions(out int selectedIndex, string currentValue)
    {
        selectedIndex = -1;

        GamePanelLibrarySO library = _panelLibraryProp.objectReferenceValue as GamePanelLibrarySO;
        if (library == null || library.panelNames == null || library.panelNames.Count == 0)
        {
            return null;
        }

        int count = library.panelNames.Count;
        string[] options = new string[count];
        for (int i = 0; i < count; i++)
        {
            string panelName = library.panelNames[i];
            options[i] = panelName;

            if (string.Equals(panelName, currentValue, StringComparison.Ordinal))
            {
                selectedIndex = i;
            }
        }

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        return options;
    }
}
#endif

