using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class CanvasAssignMainCameraAndSorting : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("若为空则自动用 Camera.main")]
    public Camera targetCamera;

    [Header("Sorting Layer (渲染排序层)")]
    [Tooltip("Canvas 使用的 Sorting Layer 名称")]
    public string sortingLayerName = "UI";

    [Tooltip("Canvas 的 Order in Layer")]
    public int sortingOrder = 0;

    [Tooltip("是否强制启用 Override Sorting（World/Screen-Camera 模式生效）")]
    public bool forceOverrideSorting = true;

    [Header("（可选）物理层/Layer")]
    [Tooltip("是否把 GameObject 的 Layer 也设置为 UI（物理与可见层常分离，可按需开启）")]
    public bool alsoSetGameObjectLayerToUI = false;

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        // 在编辑器里改参数时也即时应用
        if (isActiveAndEnabled) Apply();
    }

    void Apply()
    {
        var c = GetComponent<Canvas>();
        if (c == null) return;

        // 1) 赋相机（仅 WorldSpace 或 ScreenSpace-Camera 需要）
        if (c.renderMode == RenderMode.WorldSpace || c.renderMode == RenderMode.ScreenSpaceCamera)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (c.worldCamera == null && targetCamera != null)
                c.worldCamera = targetCamera;
        }

        // 2) 设置 Sorting Layer / Order
        // 注意：只有 WorldSpace / ScreenSpace-Camera 且 overrideSorting=true 时，sortingLayer 才起效
        if (c.renderMode == RenderMode.WorldSpace || c.renderMode == RenderMode.ScreenSpaceCamera)
        {
            if (forceOverrideSorting && !c.overrideSorting)
                c.overrideSorting = true;

            // 校验 Sorting Layer 是否存在
            if (SortingLayer.NameToID(sortingLayerName) == 0)
            {
                Debug.LogWarning($"[CanvasAssign] Sorting Layer \"{sortingLayerName}\" 不存在，请在 Project Settings > Tags and Layers 中创建。已跳过设置。", this);
            }
            else
            {
                c.sortingLayerName = sortingLayerName;
                c.sortingOrder = sortingOrder;
            }
        }
        else
        {
            // Screen Space - Overlay 模式不会使用 Sorting Layer
            // 如确实需要分层排序，请改为 ScreenSpace-Camera 或 WorldSpace
        }

        // 3) （可选）把 GameObject 的 Layer 也设为 UI
        if (alsoSetGameObjectLayerToUI)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer == -1)
            {
                Debug.LogWarning("[CanvasAssign] 物理层 \"UI\" 不存在，请在 Tags and Layers 中新增。");
            }
            else
            {
                gameObject.layer = uiLayer;
            }
        }
    }
}
