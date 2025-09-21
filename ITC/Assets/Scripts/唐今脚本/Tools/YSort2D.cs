using UnityEngine;
using UnityEngine.Rendering; // SortingGroup

/// <summary>
/// 简易Y排序：根据物体在世界坐标的Y值，动态设置渲染顺序，实现2D俯视图的前后遮挡。
/// 用法：挂到角色/道具根节点（推荐根节点加一个 SortingGroup 管理子Sprite）
/// </summary>
[ExecuteAlways]
public class YSort2D : MonoBehaviour
{
    [Header("排序基准")]
    [Tooltip("若指定，则用该点的Y作为排序基准；为空则用本物体transform.position.y")]
    public Transform pivot;

    [Tooltip("是否用渲染包围盒的底部Y（脚底）作为排序基准；仅当存在SpriteRenderer或SortingGroup时有效")]
    public bool useBoundsBottom = false;

    [Header("排序参数")]
    [Tooltip("基础排序层级（常用做全局偏移），越大越靠前")]
    public int baseOrder = 0;

    [Tooltip("每世界单位对应多少排序层级。常用100~1000之间，值越大前后切换更细腻。")]
    public int orderPerUnit = 100;

    [Tooltip("反转排序方向。默认：Y越小越靠前；勾上后相反。")]
    public bool invert = false;

    [Header("更新策略")]
    [Tooltip("是否每帧更新。若物体只在移动时改变，可关闭并按需手动调用 RefreshOrder()。")]
    public bool updateEveryFrame = true;

    [Tooltip("最小Y变化阈值（世界单位），小于该值不刷新，减少抖动和开销。")]
    public float minDeltaY = 0.001f;

    SortingGroup sortingGroup;
    SpriteRenderer[] spriteRenderers;
    float lastY;

    void Awake() => Cache();

    void OnEnable()
    {
        Cache();
        RefreshOrder(true);
    }

    void OnValidate()
    {
        if (orderPerUnit < 1) orderPerUnit = 1;
        Cache();
        RefreshOrder(true);
    }

    void Cache()
    {
        if (sortingGroup == null) sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup == null) spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    void Update()
    {
        if (!updateEveryFrame && Application.isPlaying) return;
        RefreshOrder();
    }

    /// <summary>
    /// 立刻刷新排序；当你手动改变位置/缩放或批量Spawn时可主动调用。
    /// </summary>
    public void RefreshOrder(bool force = false)
    {
        float y = GetReferenceY();

        if (!force && Mathf.Abs(y - lastY) < minDeltaY) return;
        lastY = y;

        // 计算排序值：Y越小 -> order越大（越靠前）
        int order = baseOrder + (invert ? Mathf.RoundToInt(y * orderPerUnit)
                                        : -Mathf.RoundToInt(y * orderPerUnit));

        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = order;
        }
        else if (spriteRenderers != null)
        {
            // 多子Sprite时，没有SortingGroup就逐个加偏移
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                // 保留每个子Sprite原有相对次序（i作为微小偏移，避免同层抖动）
                spriteRenderers[i].sortingOrder = order + i;
            }
        }
    }

    float GetReferenceY()
    {
        // 1) 若要求用bounds底部
        if (useBoundsBottom)
        {
            if (spriteRenderers != null && spriteRenderers.Length > 0)
            {
                var b = spriteRenderers[0].bounds;
                for (int i = 1; i < spriteRenderers.Length; i++)
                    b.Encapsulate(spriteRenderers[i].bounds);
                return b.min.y;
            }
        }

        // 2) 否则用自定义pivot或自身位置
        var t = pivot ? pivot : transform;
        return t.position.y;
    }
}
