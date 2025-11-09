#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PanelTransitionFilter))]
public class PanelTransitionFilterEditor : Editor
{
    private SerializedProperty _panelLibraryProp;
    private SerializedProperty _currentPanelProp;
    private SerializedProperty _targetPanelProp;
    private SerializedProperty _panelChangedEventProp;
    private SerializedProperty _onMatchSuccessProp;
    private string[] _panelOptions = new string[0];

    private void OnEnable()
    {
        _panelLibraryProp = serializedObject.FindProperty("_panelLibrary");
        _currentPanelProp = serializedObject.FindProperty("_currentPanel");
        _targetPanelProp = serializedObject.FindProperty("_targetPanel");
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
        
        // 当前面板下拉选择
        DrawPanelSelector(_currentPanelProp, "当前面板");
        
        // 目标变换面板下拉选择
        DrawPanelSelector(_targetPanelProp, "目标变换面板");

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

    private void DrawPanelSelector(SerializedProperty panelProp, string label)
    {
        int currentIndex = System.Array.IndexOf(_panelOptions, panelProp.stringValue);
        if (currentIndex < 0) currentIndex = 0; // 默认为 "None"

        using (new EditorGUI.DisabledScope(_panelOptions.Length <= 1))
        {
            int newIndex = EditorGUILayout.Popup(label, currentIndex, _panelOptions);
            if (newIndex != currentIndex)
            {
                panelProp.stringValue = _panelOptions[newIndex];
            }
        }
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
        }
        else
        {
            _panelOptions = new string[] { "None" };
        }
    }
}
#endif

