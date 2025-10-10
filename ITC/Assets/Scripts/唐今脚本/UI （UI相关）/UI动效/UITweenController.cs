using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// UI 动效控制器（Bezier/直线两用）
/// - 可记录“初始/目标状态”，支持预览、路径可视化；
/// - 修复：记录终点时不再意外覆盖“起点”；
/// - 可选将 Anchor/Pivot 也纳入补间；
/// - 颜色支持 CanvasGroup Alpha / Graphic Color / Both；
/// - 路径：二次 Bézier（单控制点），Scene 视图可视化与拖动；
/// - 可选择关闭曲线，按直线补间；
/// </summary>
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("UI/Tween Controller (Advanced V3)")]
public class UITweenController : MonoBehaviour
{
    // ---------- 基本动画设置 ----------
    [Header("Animation Settings")]
    [Tooltip("动画总时长（秒）。建议 0.15~0.6 做 UI 微交互，页级大进出 0.6~1.2。")]
    public float duration = 1f;

    [Tooltip("缓动类型（曲线的快慢感）。可配合下方自定义曲线开关使用。")]
    public Ease easeType = Ease.OutQuad;

    [Tooltip("启用后优先使用自定义 AnimationCurve（覆盖上面的 Ease）。")]
    public bool useCustomCurve = false;

    [Tooltip("自定义 AnimationCurve（横轴 0..1 时间，纵轴 0..1 进度）。")]
    public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("是否沿二次 Bézier 曲线运动（A→C→B）。关闭则按直线 DOAnchorPos 到目标点。")]
    public bool useBezierPath = true;

    [Tooltip("记录/预览时，在 Scene 视图显示路径与控制点。")]
    public bool showPathGizmos = true;

    // ---------- 颜色/透明度 ----------
    public enum ColorMode { None, AlphaOnly, GraphicColor, Both }

    [Header("Color / Opacity")]
    [Tooltip("None 不做颜色；AlphaOnly 仅改 CanvasGroup.alpha；GraphicColor 改 UI Graphic 颜色；Both 两者都改。")]
    public ColorMode colorMode = ColorMode.None;

    // ---------- Anchor / Pivot 可选补间 ----------
    [Header("Anchor & Pivot (Optional)")]
    [Tooltip("启用后，将 AnchorMin/AnchorMax/Pivot 也纳入补间（不建议频繁改，除非明确需求）。")]
    public bool tweenAnchorAndPivot = false;

    // ---------- 状态数据（由编辑器按钮管理） ----------
    [Header("State Data (Managed by Editor Buttons)")]
    // RectTransform 主属性
    [SerializeField] private Vector2 startAnchoredPosition;
    [SerializeField] private Vector2 startSizeDelta;
    [SerializeField] private Vector2 startAnchorMin;
    [SerializeField] private Vector2 startAnchorMax;
    [SerializeField] private Vector2 startPivot;
    [SerializeField] private Vector3 startRotation;
    [SerializeField] private Color  startColor = Color.white;

    [SerializeField] private Vector2 targetAnchoredPosition;
    [SerializeField] private Vector2 targetSizeDelta;
    [SerializeField] private Vector2 targetAnchorMin;
    [SerializeField] private Vector2 targetAnchorMax;
    [SerializeField] private Vector2 targetPivot;
    [SerializeField] private Vector3 targetRotation;
    [SerializeField] private Color  targetColor = Color.white;

    // Bézier 控制点（父本地坐标）
    [Header("Bezier Path")]
    [Tooltip("二次 Bézier 的单一控制点（父本地坐标）。可在 Scene 视图拖动。")]
    public Vector2 pathControlPoint;

    // ---------- 组件缓存 ----------
    private RectTransform _rt;
    private Graphic _graphic;
    private CanvasGroup _canvasGroup;

    public RectTransform RectTransform => _rt ??= GetComponent<RectTransform>();

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _graphic = GetComponent<Graphic>();
    }

    // ----------------- 公共方法（编辑器按钮会调用） -----------------

    /// <summary>记录当前为“初始状态”。</summary>
    public void RecordInitialState()
    {
        EnsureReferences();

        startAnchoredPosition = RectTransform.anchoredPosition;
        startSizeDelta        = RectTransform.sizeDelta;
        startAnchorMin        = RectTransform.anchorMin;
        startAnchorMax        = RectTransform.anchorMax;
        startPivot            = RectTransform.pivot;
        startRotation         = RectTransform.eulerAngles;

        if (_canvasGroup != null) startColor = new Color(1, 1, 1, _canvasGroup.alpha);
        else if (_graphic != null) startColor = _graphic.color;

        // 仅在记录“起点”时允许用当前刷新起点，再重算控制点为 AB 中点
        ResetControlPoint(refreshStartFromCurrent: true);
    }

    /// <summary>记录当前为“目标状态”。</summary>
    public void RecordTargetState()
    {
        EnsureReferences();

        targetAnchoredPosition = RectTransform.anchoredPosition;
        targetSizeDelta        = RectTransform.sizeDelta;
        targetAnchorMin        = RectTransform.anchorMin;
        targetAnchorMax        = RectTransform.anchorMax;
        targetPivot            = RectTransform.pivot;
        targetRotation         = RectTransform.eulerAngles;

        if (_canvasGroup != null) targetColor = new Color(1, 1, 1, _canvasGroup.alpha);
        else if (_graphic != null) targetColor = _graphic.color;

        // 只重算控制点，不触碰 start（避免“起点被覆盖”的老问题）
        ResetControlPoint(refreshStartFromCurrent: false);
    }

    /// <summary>回到“初始状态”。</summary>
    public void RevertToInitialState()
    {
        RectTransform.anchorMin       = startAnchorMin;
        RectTransform.anchorMax       = startAnchorMax;
        RectTransform.pivot           = startPivot;
        RectTransform.sizeDelta       = startSizeDelta;
        RectTransform.anchoredPosition= startAnchoredPosition;
        RectTransform.eulerAngles     = startRotation;

        if (colorMode == ColorMode.AlphaOnly || colorMode == ColorMode.Both)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = startColor.a;
        }
        if (colorMode == ColorMode.GraphicColor || colorMode == ColorMode.Both)
        {
            if (_graphic != null) _graphic.color = new Color(startColor.r, startColor.g, startColor.b,
                                                              (colorMode == ColorMode.GraphicColor && _canvasGroup == null) ? startColor.a : _graphic.color.a);
        }
    }

    /// <summary>
    /// 重置 Bézier 控制点到 AB 中点。可选：是否用当前物体位置“刷新起点”。
    /// </summary>
    public void ResetControlPoint(bool refreshStartFromCurrent = false)
    {
        if (refreshStartFromCurrent)
            startAnchoredPosition = RectTransform.anchoredPosition; // 只在“记录起点”时允许

        pathControlPoint = (startAnchoredPosition + targetAnchoredPosition) * 0.5f;
    }

    // ----------------- 动画播放 -----------------

    /// <summary>创建补间序列（暂停状态，等待 Play）。</summary>
    public Sequence CreateAnimationSequence()
    {
        EnsureReferences();

        var seq = DOTween.Sequence();

        // 位置：曲线或直线
        if (useBezierPath)
        {
            // 用参数化推进，统一走 easing/自定义曲线
            seq.Join(DOTween.To(() => 0f, t =>
            {
                float eased = ApplyEasing(t);
                Vector2 p = QuadBezier(startAnchoredPosition, pathControlPoint, targetAnchoredPosition, eased);
                RectTransform.anchoredPosition = p;
            }, 1f, duration));
        }
        else
        {
            var tween = RectTransform.DOAnchorPos(targetAnchoredPosition, duration);
            if (useCustomCurve) tween.SetEase(customCurve);
            else tween.SetEase(easeType);
            seq.Join(tween);
        }

        // 尺寸
        {
            var tween = RectTransform.DOSizeDelta(targetSizeDelta, duration);
            ApplyEaseTo(tween);
            seq.Join(tween);
        }

        // 旋转
        {
            var tween = RectTransform.DORotate(targetRotation, duration, RotateMode.Fast);
            ApplyEaseTo(tween);
            seq.Join(tween);
        }

        // Anchor/Pivot（可选）
        if (tweenAnchorAndPivot)
        {
            var t1 = RectTransform.DOAnchorMin(targetAnchorMin, duration);
            var t2 = RectTransform.DOAnchorMax(targetAnchorMax, duration);
            var t3 = RectTransform.DOPivot(targetPivot, duration);
            ApplyEaseTo(t1); ApplyEaseTo(t2); ApplyEaseTo(t3);
            seq.Join(t1).Join(t2).Join(t3);
        }

        // 颜色/透明度
        if (colorMode != ColorMode.None)
        {
            if ((colorMode == ColorMode.AlphaOnly || colorMode == ColorMode.Both) && _canvasGroup != null)
            {
                var t = _canvasGroup.DOFade(targetColor.a, duration);
                ApplyEaseTo(t);
                seq.Join(t);
            }
            if ((colorMode == ColorMode.GraphicColor || colorMode == ColorMode.Both) && _graphic != null)
            {
                var t = _graphic.DOColor(targetColor, duration);
                ApplyEaseTo(t);
                seq.Join(t);
            }
        }

        // 序列总体设置
        if (!useCustomCurve) seq.SetEase(easeType);
        // 不在这里 Pause/Play，由外部决定
        seq.SetTarget(this).Pause();
        return seq;
    }

    /// <summary>在运行时直接播放一次动画。</summary>
    public void Play()
    {
        CreateAnimationSequence().Play();
    }

    // ----------------- 工具/数学 -----------------
    private void EnsureReferences()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null && _graphic == null) _graphic = GetComponent<Graphic>();
    }

    /// <summary>二次 Bézier 取点</summary>
    public static Vector2 QuadBezier(in Vector2 A, in Vector2 P, in Vector2 B, float t)
    {
        float u = 1f - t;
        return u * u * A + 2f * u * t * P + t * t * B;
    }

    /// <summary>统一应用自定义/标准缓动到数值 t。</summary>
    private float ApplyEasing(float t)
    {
        if (!useCustomCurve) return DOVirtual.EasedValue(0f, 1f, t, easeType);
        return Mathf.Clamp01(customCurve.Evaluate(Mathf.Clamp01(t)));
    }

    private void ApplyEaseTo(Tween t)
    {
        if (useCustomCurve) t.SetEase(customCurve);
        else t.SetEase(easeType);
    }

    // 供编辑器访问的只读属性（用于 SceneGUI 显示）
    public Vector2 StartPos => startAnchoredPosition;
    public Vector2 EndPos   => targetAnchoredPosition;
}
