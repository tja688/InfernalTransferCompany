
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GamePanelStateMachine))]
public class GamePanelStateMachineEditor : Editor
{
    private SerializedProperty _panelLibraryProp;
    private SerializedProperty _startingPanelProp;
    private SerializedProperty _debugModeProp; // 新增
    private SerializedProperty _transitionsProp;
    private string[] _panelOptions = new string[0];

    private void OnEnable()
    {
        _panelLibraryProp = serializedObject.FindProperty("panelLibrary");
        _startingPanelProp = serializedObject.FindProperty("startingPanel");
        _debugModeProp = serializedObject.FindProperty("debugMode"); // 新增
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
        EditorGUILayout.PropertyField(_debugModeProp, new GUIContent("调试模式")); // 新增

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
            var trackNameProp = transitionProp.FindPropertyRelative("trackNameToPlay"); // 新增

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

                // 新增：播放模式选择
                var playModeProp = transitionProp.FindPropertyRelative("playMode");
                EditorGUILayout.PropertyField(playModeProp, new GUIContent("播放模式"));

                // 新增：仅在反向模式下显示反向类型选择
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

        serializedObject.ApplyModifiedProperties();
        
        // 在播放模式下强制重绘以更新当前状态
        if (Application.isPlaying)
        {
            Repaint();
        }
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
            _panelOptions = library.panelNames.ToArray();
        }
        else
        {
            _panelOptions = new string[] { "None" };
        }
    }
}
#endif
