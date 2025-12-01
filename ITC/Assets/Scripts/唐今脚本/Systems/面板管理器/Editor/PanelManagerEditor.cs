using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PanelManager))]
public class PanelManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认检视面板
        DrawDefaultInspector();

        // 仅在运行时显示调试控制
        if (Application.isPlaying)
        {
            PanelManager manager = (PanelManager)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Debugging", EditorStyles.boldLabel);

            if (manager.PanelLibrary != null)
            {
                List<string> panels = manager.PanelLibrary.panelNames;
                string currentPanel = manager.CurrentPanel;

                // 找到当前面板的索引
                int currentIndex = panels.IndexOf(currentPanel);
                if (currentIndex == -1)
                {
                    currentIndex = 0; // 默认选中第一个，或者处理未找到的情况
                }

                // 显示下拉菜单
                int newIndex = EditorGUILayout.Popup("Current Panel", currentIndex, panels.ToArray());

                // 如果用户更改了选项
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < panels.Count)
                {
                    string selectedPanel = panels[newIndex];
                    Debug.Log($"[Editor] Manually switching panel to: {selectedPanel}");
                    manager.ChangePanel(selectedPanel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Panel Library is missing!", MessageType.Warning);
            }
        }
    }
}
