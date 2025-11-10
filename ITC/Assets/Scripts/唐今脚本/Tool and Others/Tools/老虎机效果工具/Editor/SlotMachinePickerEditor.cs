using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SlotMachinePicker))]
public class SlotMachinePickerEditor : Editor
{
    private SerializedProperty viewportProp;
    private SerializedProperty slotsRootProp;
    private SerializedProperty buttonsRootProp;
    private SerializedProperty slotAnchorsProp;
    private SerializedProperty buttonItemsProp;
    private SerializedProperty enableDebugDrawProp;

    private void OnEnable()
    {
        viewportProp = serializedObject.FindProperty("viewport");
        slotsRootProp = serializedObject.FindProperty("slotsRoot");
        buttonsRootProp = serializedObject.FindProperty("buttonsRoot");
        slotAnchorsProp = serializedObject.FindProperty("slotAnchors");
        buttonItemsProp = serializedObject.FindProperty("buttonItems");
        enableDebugDrawProp = serializedObject.FindProperty("enableDebugDraw");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        EditorGUILayout.PropertyField(viewportProp);
        EditorGUILayout.PropertyField(slotsRootProp);
        EditorGUILayout.PropertyField(buttonsRootProp);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("slotAnchors"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonItems"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoCollectSlots"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoCollectButtons"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("axis"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultScrollDirection"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("slotSpacingOverride"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useUnscaledDeltaTime"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("输入控制", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableMouseScroll"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("invertMouseWheel"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelImpulseMultiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("flingThreshold"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("stepCooldown"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scrollActionReference"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("动力学", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxVelocity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("friction"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minVelocityForSnap"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("snapSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("snapThreshold"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("snapEase"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("复用与边界", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("recyclePadding"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("extraRecycleRange"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("入场 / 退场", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("entranceImpulseMultiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("exitImpulseSlots"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("restoreSnapshotBeforeEntrance"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(enableDebugDrawProp);
        using (new EditorGUI.DisabledScope(!enableDebugDrawProp.boolValue))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("slotGizmoColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonGizmoColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoRadius"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onSnappedToIndex"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onSnappedWithDirection")); // <--- 【新增】
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onEntranceCompleted"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onExitCompleted"));

        EditorGUILayout.Space();
        DrawRuntimeSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRuntimeSection()
    {
        var picker = (SlotMachinePicker)target;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("运行时状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前索引", picker.CurrentIndex.ToString());
            EditorGUILayout.LabelField("当前速度", picker.CurrentVelocity.ToString("F3"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("重新采集引用"))
                {
                    Undo.RecordObject(picker, "Rebuild SlotMachinePicker references");
                    picker.RebuildReferences();
                    EditorUtility.SetDirty(picker);
                }

                if (GUILayout.Button("保存快照"))
                {
                    picker.SaveSnapshot();
                }

                if (GUILayout.Button("恢复快照"))
                {
                    picker.RestoreSnapshot();
                    EditorUtility.SetDirty(picker);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("吸附至当前索引"))
                {
                    picker.SnapToIndex(picker.CurrentIndex, true);
                    EditorUtility.SetDirty(picker);
                }

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("入场（默认）"))
                    {
                        picker.PlayEntrance();
                    }

                    if (GUILayout.Button("退场（默认）"))
                    {
                        picker.PlayExit();
                    }
                }
            }
        }
    }
}

