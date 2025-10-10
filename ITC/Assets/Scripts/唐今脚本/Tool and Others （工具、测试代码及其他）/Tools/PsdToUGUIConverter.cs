using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class PsdToUGUIConverter : MonoBehaviour
{
    [Header("源：PSD Importer 生成的预制体资源（Prefab Asset，不是场景实例）")]
    public GameObject psdPrefabAsset;

    [Header("画布/像素设置（与项目统一）")]
    [Tooltip("与 CanvasScaler.referencePixelsPerUnit、Sprite PPU 保持一致，通常 100。")]
    public int referencePixelsPerUnit = 100;

    [Tooltip("UI 左上为原点时将 anchoredPosition.y 取反，通常勾选。")]
    public bool flipYForTopLeftLayout = true;

    [Header("锚点策略（初版统一；可后续扩展为按命名/位置智能）")]
    public AnchorPreset anchorPreset = AnchorPreset.TopLeft;

    [Header("类型设置")]
    [Tooltip("Sprite.border 有效时自动设为 Sliced，否则 Simple。")]
    public bool autoUseSlicedIfHasBorder = true;

    [Header("合成选项")]
    [Tooltip("继承 PSD 根节点的缩放（一般保持关闭）。")]
    public bool inheritRootScale = false;

    [Tooltip("保留无 Sprite 的分组为空容器（仅维持层级）。")]
    public bool keepEmptyGroups = true;

    [Tooltip("转换完成后删除临时 PSD 实例。")]
    public bool destroyTempPsdInstance = true;

    // —— 公开方法：也可以通过组件右键菜单调用 ——
#if UNITY_EDITOR
    public void ConvertNow()
    {
        if (psdPrefabAsset == null)
        {
            Debug.LogError("[PsdToUGUIConverter] 请先指定 PSD 预制体资源（Prefab Asset）！");
            return;
        }

        var myRT = GetComponent<RectTransform>();
        if (myRT == null)
        {
            Debug.LogError("[PsdToUGUIConverter] 本组件必须挂在 UI 节点上（需要 RectTransform）。");
            return;
        }

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[PsdToUGUIConverter] 未找到上级 Canvas，请把此对象放在 Canvas 下。");
            return;
        }

        // 实例化 PSD 预制体为临时对象（放到 Canvas 下）
        var psdInstance = UnityEditor.PrefabUtility.InstantiatePrefab(psdPrefabAsset, myRT) as GameObject;
        if (!psdInstance)
        {
            Debug.LogError("[PsdToUGUIConverter] 实例化 PSD 预制体失败。");
            return;
        }
        psdInstance.transform.SetParent(canvas.transform, worldPositionStays: true);

        // 解包 Prefab 实例，避免 Transform/组件类型限制
        var outer = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(psdInstance);
        if (outer != null)
        {
            UnityEditor.PrefabUtility.UnpackPrefabInstance(outer, UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.AutomatedAction);
            psdInstance = outer;
        }

        // 记录根缩放（可选）
        Vector3 rootScale = psdInstance.transform.lossyScale;

        // 校正 CanvasScaler 的参考像素（不强制，仅提示）
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null && Mathf.Abs(scaler.referencePixelsPerUnit - referencePixelsPerUnit) > 0.01f)
        {
            Debug.LogWarning($"[PsdToUGUIConverter] 提示：CanvasScaler.referencePixelsPerUnit = {scaler.referencePixelsPerUnit}，与本工具设置 {referencePixelsPerUnit} 不一致，可能导致尺寸不符。");
        }

        // 构建目标根容器（放在当前组件节点下）
        var targetRoot = CreateContainerUnder(myRT, psdInstance.name + "_UI");

        // 坐标换算需用到的相机
        var cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        // 递归转换
        int created = 0, sliced = 0;
        ConvertRecursive(psdInstance.transform, targetRoot, canvas, cam, rootScale, ref created, ref sliced);

        // 删除临时 PSD
        if (destroyTempPsdInstance)
        {
            DestroyImmediate(psdInstance);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(myRT.gameObject.scene);
        Debug.Log($"[PsdToUGUIConverter] 转换完成：创建 {created} 个 UI 节点，Sliced {sliced} 个。目标根：{targetRoot.name}");
    }
#endif

    // 递归：源 Transform → 目标 RectTransform（保持层级与大致位置）
    private void ConvertRecursive(Transform src, RectTransform dstParent,
                                  Canvas canvas, Camera cam, Vector3 rootScale,
                                  ref int created, ref int sliced)
    {
        for (int i = 0; i < src.childCount; i++)
        {
            var child = src.GetChild(i);
            var sr = child.GetComponent<SpriteRenderer>();
            RectTransform newDst = null;

            if (sr != null && sr.sprite != null)
            {
                // 有 SpriteRenderer → Image
                var go = new GameObject(child.name, typeof(RectTransform), typeof(Image));
                newDst = go.GetComponent<RectTransform>();
                newDst.SetParent(dstParent, false);
                ApplyAnchorPreset(newDst, anchorPreset);

                var img = go.GetComponent<Image>();
                var sp = sr.sprite;
                img.sprite = sp;
                img.raycastTarget = true;

                // 尺寸用像素（前提：PPU 与 CanvasRefPPU 对齐）
                var size = sp.rect.size;
                if (inheritRootScale && rootScale != Vector3.one)
                    size *= rootScale.x; // 简化假设等比缩放
                newDst.sizeDelta = size;

                // pivot 用 sprite 的 pivot（0~1）
                newDst.pivot = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height);

                // 位置：把源世界坐标映射到目标父 RT 的局部 UI 坐标
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dstParent,
                    RectTransformUtility.WorldToScreenPoint(cam, child.position),
                    cam,
                    out localPoint
                );
                if (flipYForTopLeftLayout) localPoint.y = -localPoint.y;
                newDst.anchoredPosition = localPoint;

                // 类型：自动 Sliced
                if (autoUseSlicedIfHasBorder && HasValidBorder(sp))
                {
                    img.type = Image.Type.Sliced;
                    sliced++;
                }
                else
                {
                    img.type = Image.Type.Simple;
                    img.preserveAspect = false;
                }

                created++;
            }
            else
            {
                // 纯分组（或无 Sprite）：是否保留为空容器
                if (keepEmptyGroups || child.childCount > 0)
                {
                    newDst = CreateContainerUnder(dstParent, child.name);

                    // 可选：让容器大致落在原位置（便于直观对齐）
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        dstParent,
                        RectTransformUtility.WorldToScreenPoint(cam, child.position),
                        cam,
                        out localPoint
                    );
                    if (flipYForTopLeftLayout) localPoint.y = -localPoint.y;
                    newDst.anchoredPosition = localPoint;
                }
                else
                {
                    // 完全跳过
                    continue;
                }
            }

            // 保持兄弟顺序
            newDst.SetSiblingIndex(child.GetSiblingIndex());

            // 递归
            ConvertRecursive(child, newDst, canvas, cam, rootScale, ref created, ref sliced);
        }
    }

    private static RectTransform CreateContainerUnder(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        // 统一左上锚点（与 TopLeft 预设一致）
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    private static bool HasValidBorder(Sprite sp)
    {
        var b = sp.border;
        return b.x > 0 || b.y > 0 || b.z > 0 || b.w > 0;
    }

    private void ApplyAnchorPreset(RectTransform rt, AnchorPreset preset)
    {
        switch (preset)
        {
            case AnchorPreset.TopLeft:
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                break;
            case AnchorPreset.Center:
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                break;
            case AnchorPreset.StretchTop:
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                break;
            case AnchorPreset.StretchFull:
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 1);
                break;
        }
    }

    public enum AnchorPreset
    {
        TopLeft,
        Center,
        StretchTop,
        StretchFull
    }
}

#if UNITY_EDITOR
// —— 同一文件内的自定义 Inspector（单脚本完成） ——

[CustomEditor(typeof(PsdToUGUIConverter))]
public class PsdToUGUIConverterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var converter = (PsdToUGUIConverter)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox("把 PSD Importer 生成的 Prefab 解析→转换为 UGUI，结果作为子节点创建在本对象下。", MessageType.Info);

        GUI.enabled = converter != null && converter.psdPrefabAsset != null;
        if (GUILayout.Button("▶ 一键转换（PSD 预制体 → UGUI）", GUILayout.Height(32)))
        {
            Undo.RegisterFullObjectHierarchyUndo(converter.gameObject, "Convert PSD to UGUI");
            converter.ConvertNow();
        }
        GUI.enabled = true;

        if (converter.psdPrefabAsset == null)
        {
            EditorGUILayout.HelpBox("请先在上方关联 PSD 预制体资源（Prefab Asset）。", MessageType.Warning);
        }
    }
}
#endif
