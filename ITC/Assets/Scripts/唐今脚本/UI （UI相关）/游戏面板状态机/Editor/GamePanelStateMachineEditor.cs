#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GamePanelStateMachine))]
public class GamePanelStateMachineEditor : Editor
{
    private SerializedProperty _panelLibraryProp;
    private SerializedProperty _startingPanelProp;
    private SerializedProperty _debugModeProp;
    private SerializedProperty _transitionsProp;
    private string[] _panelOptions = new string[0];

    // --- 新增 ---
    // 用于存储运行时测试工具中下拉菜单的选中项
    private int _testTargetPanelIndex = 0; 
    // --- 新增结束 ---

    private void OnEnable()
    {
        _panelLibraryProp = serializedObject.FindProperty("panelLibrary");
        _startingPanelProp = serializedObject.FindProperty("startingPanel");
        _debugModeProp = serializedObject.FindProperty("debugMode");
        _transitionsProp = serializedObject.FindProperty("transitions");
        RefreshPanelOptions();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 显示当前状态 (只读)
        using (new EditorGUI.DisabledScope(true))
        {
            var stateMachine = (GamePanelStateMachine)target;
            EditorGUILayout.TextField("当前状态", Application.isPlaying ? stateMachine.CurrentPanel : "(Not in Play Mode)");
        }

        EditorGUILayout.PropertyField(_panelLibraryProp);

        // 当库发生变化时，刷新选项
        if (serializedObject.ApplyModifiedProperties())
        {
            RefreshPanelOptions();
        }
        
        serializedObject.Update();

        // 初始面板选择
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("基础设置", EditorStyles.boldLabel);
        DrawPanelSelector(_startingPanelProp, "初始面板");
        EditorGUILayout.PropertyField(_debugModeProp, new GUIContent("调试模式"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("面板过渡设置", EditorStyles.boldLabel);

        if (_panelOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("请先关联一个有效的 GamePanelLibrarySO，并在其中添加面板名称。", MessageType.Warning);
        }

        for (int i = 0; i < _transitionsProp.arraySize; i++)
        {
            var transitionProp = _transitionsProp.GetArrayElementAtIndex(i);
            var fromPanelProp = transitionProp.FindPropertyRelative("fromPanel");
            var toPanelProp = transitionProp.FindPropertyRelative("toPanel");
            var trackProp = transitionProp.FindPropertyRelative("transitionTrack");
            var trackNameProp = transitionProp.FindPropertyRelative("trackNameToPlay");

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"过渡 #{i + 1}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    _transitionsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                DrawPanelSelector(fromPanelProp, "从");
                DrawPanelSelector(toPanelProp, "到");
                EditorGUILayout.PropertyField(trackProp, new GUIContent("动画轨道 (Track)"));
                EditorGUILayout.PropertyField(trackNameProp, new GUIContent("轨道名称 (Name)"));
                
                var playModeProp = transitionProp.FindPropertyRelative("playMode");
                EditorGUILayout.PropertyField(playModeProp, new GUIContent("播放模式"));
                
                if (playModeProp.enumValueIndex == (int)GamePanelStateMachine.TransitionPlayMode.Reverse)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(transitionProp.FindPropertyRelative("reverseMode"), new GUIContent("反向模式"));
                    }
                }
            }
        }

        if (GUILayout.Button("添加新过渡", GUILayout.Height(25)))
        {
            _transitionsProp.arraySize++;
        }
        
        // --- 新增的运行时测试工具 ---
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("运行时测试 (Runtime Testing)", EditorStyles.boldLabel);
            
        // 只在播放模式下显示此工具
        if (Application.isPlaying)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                // 确保索引不会越界 (以防 panelLibrary 在运行时被更改)
                if (_testTargetPanelIndex >= _panelOptions.Length)
                {
                    _testTargetPanelIndex = 0;
                }

                // 禁用下拉菜单（如果没有选项）
                using (new EditorGUI.DisabledScope(_panelOptions.Length <= 1))
                {
                    _testTargetPanelIndex = EditorGUILayout.Popup("目标状态", _testTargetPanelIndex, _panelOptions);
                }

                // 禁用按钮（如果没有选项或选中的是 "None"）
                bool canChange = _panelOptions.Length > 0 && 
                                 _testTargetPanelIndex < _panelOptions.Length && 
                                 _panelOptions[_testTargetPanelIndex] != "None";

                using (new EditorGUI.DisabledScope(!canChange))
                {
                    if (GUILayout.Button("强制切换状态", GUILayout.Height(25)))
                    {
                        var stateMachine = (GamePanelStateMachine)target;
                        string targetPanel = _panelOptions[_testTargetPanelIndex];
                            
                        // 调用 public 方法
                        stateMachine.RequestStateChange(targetPanel);
                    }
                }
            }
                
            // 在播放模式下强制重绘以更新当前状态
            Repaint();
        }
        else
        {
            // 如果不在播放模式，重置测试索引并显示提示
            _testTargetPanelIndex = 0;
            EditorGUILayout.HelpBox("进入播放模式 (Play Mode) 以使用此工具。", MessageType.Info);
        }
        // --- 新增结束 ---

        serializedObject.ApplyModifiedProperties();
        
        // 原始的 Repaint() 调用已被移到上面的 if(Application.isPlaying) 块中
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
        var stateMachine = (GamePanelStateMachine)target;
        var library = stateMachine.PanelLibrary;
        if (library != null && library.panelNames != null)
        {
            // --- 修改 --- 确保 "None" 总是第一个选项
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
        
        // 重置测试索引，以防列表变化
        _testTargetPanelIndex = 0;
    }
}
#endif