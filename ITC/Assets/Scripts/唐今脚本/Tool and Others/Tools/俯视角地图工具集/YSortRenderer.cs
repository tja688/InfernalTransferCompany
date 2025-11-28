using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 通用 2D Y 轴排序器：根据参考点的世界 Y 值实时调整 SortingOrder，
/// 让俯视角角色/物体在遮挡关系上更自然。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class YSortRenderer : MonoBehaviour
{
    public enum ReferenceMode
    {
        TransformY,
        CustomPivot,
        RendererBoundsBottom
    }

    public enum UpdateMode
    {
        Always,
        PlayModeOnly,
        Manual
    }

    [Header("采样设置")]
    [Tooltip("用于计算排序值的参考方式。推荐 RendererBoundsBottom 以脚底为基准。")]
    public ReferenceMode referenceMode = ReferenceMode.RendererBoundsBottom;

    [Tooltip("当参考方式为 CustomPivot 时使用的 Transform。")]
    public Transform customPivot;

    [Tooltip("计算包围盒时是否包含子层级。")]
    public bool includeChildren = true;

    [Tooltip("遍历子层级时是否包含未激活的对象。")]
    public bool includeInactiveChildren = false;

    [Tooltip("当没有 SortingGroup 时，是否包含自身的 SpriteRenderer。")]
    public bool includeSelfRenderer = true;

    [Header("排序参数")]
    [Tooltip("可选：强制覆盖 Sorting Layer 名称，为空则沿用原设置。")]
    public string overrideSortingLayer = string.Empty;

    [Tooltip("基础排序偏移。可用于不同楼层、区域的整体前后关系控制。")]
    public int baseOrder = 0;

    [Tooltip("每 1 世界单位对应的排序层级数量。数值越大，切换越平滑。")]
    public int orderPerUnit = 100;

    [Tooltip("勾选后改为 Y 越大排序越靠前。默认关闭（Y 越小越靠前）。")]
    public bool invertSorting = false;

    [Tooltip("为同节点下的多个 SpriteRenderer 叠加递增偏移，避免完全同序导致抖动。")]
    public bool addRendererIndexOffset = true;

    [Header("更新策略")]
    [Tooltip("Always：编辑器 & 运行期都刷新；PlayModeOnly：仅运行时；Manual：仅手动调用刷新。")]
    public UpdateMode updateMode = UpdateMode.Always;

    [Tooltip("Y 值变化小于该阈值将不会触发刷新，避免轻微抖动。")]
    public float minDelta = 0.001f;

    [Tooltip("编辑器下是否在 Transform 层级变化时自动 ReCache。")]
    public bool recacheOnChildrenChanged = true;

    readonly List<SpriteRenderer> cachedRenderers = new List<SpriteRenderer>();
    SortingGroup sortingGroup;
    float lastSampledY = float.NaN;

    void Reset()
    {
        CacheRenderers();
        RefreshOrder(true);
    }

    void Awake() => CacheRenderers();

    void OnEnable()
    {
        CacheRenderers();
        RefreshOrder(true);
    }

    void OnTransformChildrenChanged()
    {
        if (recacheOnChildrenChanged)
            CacheRenderers();
    }

    void OnValidate()
    {
        orderPerUnit = Mathf.Max(1, orderPerUnit);
        minDelta = Mathf.Max(0f, minDelta);
        CacheRenderers();
        RefreshOrder(true);
    }

    void Update()
    {
        if (!ShouldUpdate())
            return;
        RefreshOrder();
    }

    /// <summary>
    /// 立即刷新一次排序（在 Manual 模式下可调用此方法）。
    /// </summary>
    [ContextMenu("Refresh Order Now")]
    public void RefreshOrderContextMenu()
    {
        RefreshOrder(true);
    }

    /// <summary>
    /// 强制重新缓存涉及到的 SpriteRenderer。
    /// </summary>
    [ContextMenu("Rebuild Renderer Cache")]
    public void RebuildRendererCache()
    {
        CacheRenderers();
        RefreshOrder(true);
    }

    public void RefreshOrder(bool force = false)
    {
        if (!isActiveAndEnabled)
            return;

        float currentY = SampleReferenceY();
        if (!force && !float.IsNaN(lastSampledY) && Mathf.Abs(currentY - lastSampledY) < minDelta)
            return;

        lastSampledY = currentY;
        int order = baseOrder + (invertSorting
            ? Mathf.RoundToInt(currentY * orderPerUnit)
            : -Mathf.RoundToInt(currentY * orderPerUnit));

        ApplyOrder(order);
    }

    bool ShouldUpdate()
    {
        switch (updateMode)
        {
            case UpdateMode.Manual:
                return false;
            case UpdateMode.PlayModeOnly:
                return Application.isPlaying;
            default:
                return true;
        }
    }

    float SampleReferenceY()
    {
        switch (referenceMode)
        {
            case ReferenceMode.CustomPivot:
                if (customPivot != null)
                    return customPivot.position.y;
                break;
            case ReferenceMode.RendererBoundsBottom:
                if (TryGetRendererBounds(out var bounds))
                    return bounds.min.y;
                break;
        }

        var target = customPivot != null && referenceMode == ReferenceMode.CustomPivot
            ? customPivot
            : transform;
        return target.position.y;
    }

    bool TryGetRendererBounds(out Bounds result)
    {
        result = default;
        bool initialized = false;

        EnsureRendererCache();
        for (int i = cachedRenderers.Count - 1; i >= 0; i--)
        {
            var sr = cachedRenderers[i];
            if (sr == null)
            {
                cachedRenderers.RemoveAt(i);
                continue;
            }

            if (!initialized)
            {
                result = sr.bounds;
                initialized = true;
            }
            else
            {
                result.Encapsulate(sr.bounds);
            }
        }

        return initialized;
    }

    void ApplyOrder(int order)
    {
        if (sortingGroup != null)
        {
            if (!string.IsNullOrEmpty(overrideSortingLayer))
                sortingGroup.sortingLayerName = overrideSortingLayer;
            sortingGroup.sortingOrder = order;
            return;
        }

        EnsureRendererCache();
        if (cachedRenderers.Count == 0)
            return;

        for (int i = 0; i < cachedRenderers.Count; i++)
        {
            var sr = cachedRenderers[i];
            if (sr == null)
                continue;

            if (!string.IsNullOrEmpty(overrideSortingLayer))
                sr.sortingLayerName = overrideSortingLayer;

            sr.sortingOrder = addRendererIndexOffset ? order + i : order;
        }
    }

    void CacheRenderers()
    {
        sortingGroup = GetComponent<SortingGroup>();
        cachedRenderers.Clear();

        if (sortingGroup != null)
        {
            AppendRenderersFromChildren();
            return;
        }

        if (includeSelfRenderer)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                cachedRenderers.Add(sr);
        }

        AppendRenderersFromChildren();
    }

    void AppendRenderersFromChildren()
    {
        if (!includeChildren)
            return;

        var children = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
        for (int i = 0; i < children.Length; i++)
        {
            var sr = children[i];
            if (sr == null)
                continue;
            if (!includeSelfRenderer && sr.gameObject == gameObject)
                continue;
            if (!cachedRenderers.Contains(sr))
                cachedRenderers.Add(sr);
        }
    }

    void EnsureRendererCache()
    {
        if (cachedRenderers.Count == 0)
            CacheRenderers();
    }
}










