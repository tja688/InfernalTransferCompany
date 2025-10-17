using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class UIPivotParentTool : ScriptableObject
{
    // 将父物体中心固定在子物体几何中心（通过父子关系和位移调整）
    [MenuItem("Tools/UI Tool/ParentAtChildCenter")]
    static void SetParentAtChildCenter()
    {
        GameObject childObj = Selection.activeGameObject;
        string dialogTitle = "Parent At Child Center";

        // 检查选中物体
        if (childObj == null)
        {
            EditorUtility.DisplayDialog(dialogTitle, "请选中一个UI子物体！", "确定");
            return;
        }

        // 检查是否是UI元素（必须有RectTransform）
        RectTransform childRect = childObj.GetComponent<RectTransform>();
        if (childRect == null)
        {
            EditorUtility.DisplayDialog(dialogTitle, "选中的物体不是UI元素（缺少RectTransform）！", "确定");
            return;
        }

        // 计算子物体（包括所有子级）的几何中心（世界空间）
        Bounds worldBounds = CalculateUIBounds(childObj);
        Vector3 childCenter = worldBounds.center; // 子物体的几何中心（世界坐标）

        // 创建父物体
        GameObject parentObj = new GameObject(childObj.name + "_CenterParent");
        RectTransform parentRect = parentObj.AddComponent<RectTransform>();

        // 继承原父级，保持层级关系
        Transform originalParent = childObj.transform.parent;
        if (originalParent != null)
        {
            parentObj.transform.SetParent(originalParent);
        }

        // 关键1：父物体位置 = 子物体几何中心（世界坐标）
        parentObj.transform.position = childCenter;

        // 关键2：子物体设为父物体的子级，并修正相对位置（确保视觉上不动）
        // 计算子物体原来的世界位置相对于父物体的本地位置
        Vector3 localPosInParent = parentObj.transform.InverseTransformPoint(childObj.transform.position);
        childObj.transform.SetParent(parentObj.transform);
        childObj.transform.localPosition = localPosInParent; // 抵消父物体位置，保持视觉不变

        // 选中父物体，方便观察
        Selection.activeGameObject = parentObj;

        EditorUtility.DisplayDialog(dialogTitle, "父物体已创建，其中心已固定在子物体几何中心！", "确定");
    }

    // 计算UI物体（含子级）的世界空间边界
    static Bounds CalculateUIBounds(GameObject uiObj)
    {
        List<RectTransform> allRects = new List<RectTransform>();
        uiObj.GetComponentsInChildren<RectTransform>(true, allRects);

        Bounds bounds = new Bounds();
        bool isFirst = true;

        foreach (var rect in allRects)
        {
            if (!rect.gameObject.activeInHierarchy) continue; // 忽略隐藏物体

            // 获取UI四个角的世界坐标
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            // 计算当前Rect的边界
            Bounds rectBounds = new Bounds(corners[0], Vector3.zero);
            foreach (var corner in corners)
            {
                rectBounds.Encapsulate(corner);
            }

            // 合并到总边界
            if (isFirst)
            {
                bounds = rectBounds;
                isFirst = false;
            }
            else
            {
                bounds.Encapsulate(rectBounds);
            }
        }

        return bounds;
    }
}