#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UITweenController))]
public class UITweenControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认的Inspector面板
        DrawDefaultInspector();

        // 获取我们正在编辑的脚本实例
        UITweenController controller = (UITweenController)target;

        // 添加自定义按钮
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Workflow: Move object to its final state in the Scene, then click 'Record'.", MessageType.Info);

        if (GUILayout.Button("Record Target State"))
        {
            // 标记对象，以便可以撤销操作
            Undo.RecordObject(controller, "Record Target State");
            controller.RecordStates();
        }

        if (GUILayout.Button("Revert to Start State"))
        {
            Undo.RecordObject(controller.transform, "Revert to Start State");
            controller.RevertToStart();
        }

        if (GUILayout.Button("Preview Target State"))
        {
            // 这个按钮可以让你预览最终效果
            controller.transform.position = controller.targetPosition;
            controller.transform.localScale = controller.targetScale;
        }
    }
}
#endif