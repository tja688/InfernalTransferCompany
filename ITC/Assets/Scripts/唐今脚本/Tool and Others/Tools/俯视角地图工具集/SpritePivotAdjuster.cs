using UnityEngine;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// SpriteRenderer 轴心可视化与调整工具：在场景视图中直接拖动轴心，
/// 自动保持对象在场景中的视觉位置不变，避免重新导入 Sprite 时的位置偏移。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpritePivotAdjuster : MonoBehaviour
{
    private const float MinHandleScale = 0.02f;
    private static readonly Vector2 InvalidPivot = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

    [Header("Axis 设置 (0~1)")]
    [SerializeField]
    [Tooltip("当前轴心（0~1），会在运行时/编辑器生成临时 Sprite 实例来应用。")]
    private Vector2 pivotNormalized = new Vector2(0.5f, 0.5f);

    [SerializeField]
    [Tooltip("是否限制轴心拖拽在原始 Sprite 的矩形范围内。")]
    private bool clampPivotToRect = true;

    [SerializeField]
    [Tooltip("自动监控 SpriteRenderer.sprite 的变更，并将其作为新的源 Sprite。")]
    private bool autoCaptureSpriteChanges = true;

    [SerializeField]
    [Tooltip("应用新轴心时保持 Sprite 包围盒中心不动，防止对象在场景中跳动。")]
    private bool lockBoundsCenter = true;

    [Header("可视化")]
    [SerializeField]
    [Tooltip("是否在 Scene 视图绘制包围盒和轴心。")]
    private bool drawGuides = true;

    [SerializeField]
    private Color boundsColor = new Color(0f, 0.75f, 1f, 0.35f);

    [SerializeField]
    private Color pivotColor = new Color(1f, 0.55f, 0.1f, 0.9f);

    [SerializeField, Min(MinHandleScale)]
    private float handleSize = 0.12f;

    [SerializeField]
    [Tooltip("在 Scene 视图显示轴心坐标标签。")]
    private bool showPivotLabel = true;

    [SerializeField, HideInInspector]
    private Sprite sourceSprite;

    SpriteRenderer spriteRenderer;
    Sprite runtimeSprite;
    Vector2 lastAppliedPivot = InvalidPivot;

    /// <summary>当前归一化轴心。</summary>
    public Vector2 PivotNormalized => pivotNormalized;
    /// <summary>当前是否有有效源 Sprite。</summary>
    public bool HasSourceSprite => sourceSprite != null;
    /// <summary>Scene 可视化开关。</summary>
    public bool DrawGuides => drawGuides;
    /// <summary>Scene 可视化颜色与尺寸。</summary>
    public Color BoundsColor => boundsColor;
    public Color PivotColor => pivotColor;
    public float HandleSize => handleSize;
    public bool ShowPivotLabel => showPivotLabel;
    /// <summary>挂载的 SpriteRenderer。</summary>
    public SpriteRenderer SpriteRenderer => spriteRenderer ??= GetComponent<SpriteRenderer>();

    void Reset()
    {
        CacheRenderer();
        CaptureSpriteFromRenderer(true);
        ApplyPivot(true);
    }

    void Awake() => CacheRenderer();

    void OnEnable()
    {
        CacheRenderer();
        TryEnsureSourceSprite();
        ApplyPivot(true);
    }

    void OnDisable()
    {
        RestoreSourceSprite();
        CleanupRuntimeSprite();
    }

    void OnDestroy()
    {
        RestoreSourceSprite();
        CleanupRuntimeSprite();
    }

    void OnValidate()
    {
        CacheRenderer();
        pivotNormalized = ClampPivot(pivotNormalized);

        if (autoCaptureSpriteChanges)
        {
            CaptureSpriteFromRenderer(false);
        }

        ApplyPivot();
    }

    /// <summary>
    /// 将轴心设为新值并立即应用。
    /// </summary>
    public void SetPivotNormalized(Vector2 normalized, bool forceApply = true)
    {
        normalized = ClampPivot(normalized);
        if (pivotNormalized == normalized && !forceApply)
            return;

        pivotNormalized = normalized;
        if (forceApply)
        {
            ApplyPivot(true);
#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }
    }

    /// <summary>
    /// 将世界坐标转换成归一化轴心并应用。
    /// </summary>
    public void SetPivotFromWorldPosition(Vector3 worldPosition)
    {
        var reference = GetReferenceSprite();
        if (reference == null)
            return;

        Vector2 normalized = WorldToNormalized(worldPosition, reference);
        SetPivotNormalized(normalized);
    }

    /// <summary>
    /// 重新同步源 Sprite 的默认轴心。
    /// </summary>
    public void SyncPivotFromSourceSprite()
    {
        var reference = GetReferenceSprite();
        if (reference == null)
            return;

        pivotNormalized = GetNormalizedPivot(reference);
        ApplyPivot(true);
    }

    /// <summary>
    /// 捕获当前 SpriteRenderer.sprite 作为新的源 Sprite。
    /// </summary>
    public void CaptureSpriteFromRenderer(bool syncPivot)
    {
        if (SpriteRenderer == null)
            return;

        var current = SpriteRenderer.sprite;
        if (current == null || current == runtimeSprite)
            return;

        if (sourceSprite == current && !syncPivot)
            return;

        sourceSprite = current;
        if (syncPivot)
        {
            pivotNormalized = GetNormalizedPivot(sourceSprite);
        }
        lastAppliedPivot = InvalidPivot;
    }

    /// <summary>
    /// 还原源 Sprite 及其轴心。
    /// </summary>
    public void RevertToSourceSprite()
    {
        RestoreSourceSprite();
        CleanupRuntimeSprite();
        if (sourceSprite != null)
        {
            pivotNormalized = GetNormalizedPivot(sourceSprite);
        }
        lastAppliedPivot = InvalidPivot;
    }

    /// <summary>
    /// 获取 Scene 视图中轴心的世界坐标。
    /// </summary>
    public Vector3 GetPivotWorldPosition()
    {
        var reference = GetReferenceSprite();
        if (reference == null)
            return transform.position;

        return NormalizedToWorld(pivotNormalized, reference);
    }

    /// <summary>
    /// 计算 Sprite 的四个世界坐标角点（按左下、右下、右上、左上）。
    /// </summary>
    public bool TryGetWorldCorners(Vector3[] corners)
    {
        if (corners == null || corners.Length < 4)
            return false;

        var reference = GetReferenceSprite();
        if (reference == null)
            return false;

        float ppu = Mathf.Max(1e-5f, reference.pixelsPerUnit);
        Vector2 rectSize = reference.rect.size;
        Vector2 pivotPixels = new Vector2(rectSize.x * pivotNormalized.x, rectSize.y * pivotNormalized.y);
        Vector2 bottomLeftPixels = -pivotPixels;
        Vector2 bottomRightPixels = bottomLeftPixels + new Vector2(rectSize.x, 0f);
        Vector2 topRightPixels = bottomRightPixels + new Vector2(0f, rectSize.y);
        Vector2 topLeftPixels = bottomLeftPixels + new Vector2(0f, rectSize.y);

        corners[0] = PixelToWorld(bottomLeftPixels, ppu);
        corners[1] = PixelToWorld(bottomRightPixels, ppu);
        corners[2] = PixelToWorld(topRightPixels, ppu);
        corners[3] = PixelToWorld(topLeftPixels, ppu);
        return true;
    }

    /// <summary>
    /// 是否允许捕获当前 SpriteRenderer 上的新 Sprite。
    /// </summary>
    public bool CanCaptureSprite =>
        SpriteRenderer != null &&
        SpriteRenderer.sprite != null &&
        SpriteRenderer.sprite != runtimeSprite;

    void CacheRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void TryEnsureSourceSprite()
    {
        if (sourceSprite == null)
        {
            CaptureSpriteFromRenderer(true);
        }
    }

    Vector2 ClampPivot(Vector2 pivot)
    {
        if (!clampPivotToRect)
            return pivot;

        pivot.x = Mathf.Clamp01(pivot.x);
        pivot.y = Mathf.Clamp01(pivot.y);
        return pivot;
    }

    Sprite GetReferenceSprite()
    {
        if (sourceSprite != null)
            return sourceSprite;
        return SpriteRenderer != null ? SpriteRenderer.sprite : null;
    }

    static Vector2 GetNormalizedPivot(Sprite sprite)
    {
        if (sprite == null)
            return new Vector2(0.5f, 0.5f);

        Vector2 rectSize = sprite.rect.size;
        if (Mathf.Approximately(rectSize.x, 0f) || Mathf.Approximately(rectSize.y, 0f))
            return new Vector2(0.5f, 0.5f);

        return new Vector2(sprite.pivot.x / rectSize.x, sprite.pivot.y / rectSize.y);
    }

    void ApplyPivot(bool force = false)
    {
        if (!isActiveAndEnabled || SpriteRenderer == null)
            return;

        var reference = GetReferenceSprite();
        if (reference == null || reference.texture == null)
            return;

        Vector2 targetPivot = clampPivotToRect ? ClampPivot(pivotNormalized) : pivotNormalized;
        if (!force && lastAppliedPivot == targetPivot)
            return;

        Vector3 lockedPoint = lockBoundsCenter && SpriteRenderer.sprite != null
            ? SpriteRenderer.bounds.center
            : transform.position;

        var previousRuntime = runtimeSprite;
        SpriteMeshType meshType = GetSpriteMeshType(reference);
        runtimeSprite = Sprite.Create(reference.texture, reference.rect, targetPivot,
                                      reference.pixelsPerUnit, 0, meshType, reference.border);
        runtimeSprite.name = $"{reference.name}_pivot_{targetPivot.x:0.000}_{targetPivot.y:0.000}";
        runtimeSprite.hideFlags = HideFlags.HideAndDontSave;

        SpriteRenderer.sprite = runtimeSprite;
        if (previousRuntime != null && previousRuntime != sourceSprite)
        {
            DestroySpriteAsset(previousRuntime);
        }

        if (lockBoundsCenter && SpriteRenderer.sprite != null)
        {
            Vector3 newCenter = SpriteRenderer.bounds.center;
            transform.position += lockedPoint - newCenter;
        }

        lastAppliedPivot = targetPivot;
    }

    void RestoreSourceSprite()
    {
        if (SpriteRenderer == null || sourceSprite == null)
            return;

        Vector3 lockedPoint = lockBoundsCenter && SpriteRenderer.sprite != null
            ? SpriteRenderer.bounds.center
            : transform.position;

        SpriteRenderer.sprite = sourceSprite;

        if (lockBoundsCenter && SpriteRenderer.sprite != null)
        {
            Vector3 newCenter = SpriteRenderer.bounds.center;
            transform.position += lockedPoint - newCenter;
        }
    }

    void CleanupRuntimeSprite()
    {
        if (runtimeSprite == null)
            return;

        DestroySpriteAsset(runtimeSprite);
        runtimeSprite = null;
    }

    static void DestroySpriteAsset(Sprite sprite)
    {
        if (sprite == null)
            return;
#if UNITY_EDITOR
        if (Application.isPlaying)
            Destroy(sprite);
        else
            DestroyImmediate(sprite);
#else
        Destroy(sprite);
#endif
    }

    Vector3 PixelToWorld(Vector2 pixelOffset, float pixelsPerUnit)
    {
        Vector3 local = new Vector3(pixelOffset.x / pixelsPerUnit, pixelOffset.y / pixelsPerUnit, 0f);
        return transform.TransformPoint(local);
    }

    Vector3 NormalizedToWorld(Vector2 normalized, Sprite reference)
    {
        float ppu = Mathf.Max(1e-5f, reference.pixelsPerUnit);
        Vector2 rectSize = reference.rect.size;
        Vector2 currentPivotPixels = new Vector2(rectSize.x * pivotNormalized.x, rectSize.y * pivotNormalized.y);
        Vector2 targetPixels = new Vector2(rectSize.x * normalized.x, rectSize.y * normalized.y);
        Vector2 deltaPixels = targetPixels - currentPivotPixels;
        Vector3 local = new Vector3(deltaPixels.x / ppu, deltaPixels.y / ppu, 0f);
        return transform.TransformPoint(local);
    }

    Vector2 WorldToNormalized(Vector3 worldPosition, Sprite reference)
    {
        float ppu = Mathf.Max(1e-5f, reference.pixelsPerUnit);
        Vector2 rectSize = reference.rect.size;
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        Vector2 currentPivotPixels = new Vector2(rectSize.x * pivotNormalized.x, rectSize.y * pivotNormalized.y);
        Vector2 deltaPixels = new Vector2(local.x * ppu, local.y * ppu);
        Vector2 targetPixels = currentPivotPixels + deltaPixels;
        return new Vector2(targetPixels.x / rectSize.x, targetPixels.y / rectSize.y);
    }

    /// <summary>
    /// 通过反射获取 Sprite 的 meshType，如果无法获取则返回默认值 SpriteMeshType.Tight。
    /// </summary>
    static SpriteMeshType GetSpriteMeshType(Sprite sprite)
    {
        if (sprite == null)
            return SpriteMeshType.Tight;

        // 尝试通过反射获取 meshType 属性
        PropertyInfo meshTypeProperty = typeof(Sprite).GetProperty("meshType", BindingFlags.Public | BindingFlags.Instance);
        if (meshTypeProperty != null)
        {
            object meshTypeValue = meshTypeProperty.GetValue(sprite);
            if (meshTypeValue is SpriteMeshType)
                return (SpriteMeshType)meshTypeValue;
        }

        // 如果反射失败，返回默认值
        return SpriteMeshType.Tight;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpritePivotAdjuster))]
public class SpritePivotAdjusterEditor : Editor
{
    static readonly Vector3[] Corners = new Vector3[4];

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "sourceSprite", "m_Script");

        var tool = (SpritePivotAdjuster)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("工具操作", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(!tool.CanCaptureSprite))
        {
            if (GUILayout.Button("捕获当前 Sprite 作为源"))
            {
                Undo.RecordObject(tool, "Capture Source Sprite");
                tool.CaptureSpriteFromRenderer(true);
                EditorUtility.SetDirty(tool);
            }
        }

        using (new EditorGUI.DisabledScope(!tool.HasSourceSprite))
        {
            if (GUILayout.Button("还原到源 Sprite / 轴心"))
            {
                Undo.RecordObject(tool, "Revert Sprite Pivot");
                tool.RevertToSourceSprite();
                EditorUtility.SetDirty(tool);
            }

            if (GUILayout.Button("同步源 Sprite 默认轴心"))
            {
                Undo.RecordObject(tool, "Sync Source Pivot");
                tool.SyncPivotFromSourceSprite();
                EditorUtility.SetDirty(tool);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        var tool = (SpritePivotAdjuster)target;
        if (!tool.DrawGuides || tool.SpriteRenderer == null || !tool.HasSourceSprite)
            return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        if (tool.TryGetWorldCorners(Corners))
        {
            Handles.color = tool.BoundsColor;
            Handles.DrawAAPolyLine(3f, Corners[0], Corners[1], Corners[2], Corners[3], Corners[0]);
        }

        Vector3 pivotPos = tool.GetPivotWorldPosition();
        Handles.color = tool.PivotColor;
        float size = HandleUtility.GetHandleSize(pivotPos) * tool.HandleSize;
        EditorGUI.BeginChangeCheck();
        var fmh_459_59_638996655091821770 = Quaternion.identity; Vector3 newPos = Handles.FreeMoveHandle(pivotPos, size, Vector3.zero, Handles.SphereHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(tool, "Move Sprite Pivot");
            tool.SetPivotFromWorldPosition(newPos);
            EditorUtility.SetDirty(tool);
        }

        if (tool.ShowPivotLabel)
        {
            var pivot = tool.PivotNormalized;
            Handles.Label(pivotPos + Vector3.up * size * 0.5f, $"Pivot ({pivot.x:F2}, {pivot.y:F2})");
        }
    }
}
#endif

