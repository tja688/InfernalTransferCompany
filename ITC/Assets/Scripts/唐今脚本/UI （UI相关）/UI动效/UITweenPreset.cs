// MIT License
// ScriptableObject preset for Goal-Driven UI Tween
// - 保留旧字段名：presetName / delay / loops / loopType / unscaledTime / useCustomCurve / customCurve / easeType / targetPivot
// - 新增打断策略枚举与字段（相对/贝塞尔）
// - 统一提供 ApplyEaseTo(Tween) 供 Player/Controller 复用

using UnityEngine;
using DG.Tweening;

public enum RelativeBaselineMode
{
    KeepBaseline = 0,       // 终点 = 初始基线 + delta（推荐，避免漂移）
    RebaseAtInterrupt = 1   // 终点 = (打断时刻当前值) + delta（以当前为新基线）
}

public enum BezierInterruptPolicy
{
    RecomputeCurve = 0,     // 打断时以当前点重解一条二次贝塞尔
    ReattachToCurve = 1     // 将当前点投影回既定曲线并续播
}

[CreateAssetMenu(fileName = "NewUITweenPreset", menuName = "UI Tween/Goal-Driven Preset", order = 1000)]
public class UITweenPreset : ScriptableObject
{
    [Header("Identity")]
    public string presetName = "MyTween";

    [Header("Mode")]
    [Tooltip("勾选：相对模式（位置/尺寸/旋转使用增量）；不勾选：绝对模式（使用目标终值）。")]
    public bool useRelativeMode = false;

    [Tooltip("相对模式打断策略：保持初始基线或在打断时重设基线。")]
    public RelativeBaselineMode relativeBaselineMode = RelativeBaselineMode.KeepBaseline;

    [Header("Bezier (only works in ABSOLUTE position mode)")]
    [Tooltip("仅在【绝对位置】模式下生效：使用二次贝塞尔路径。")]
    public bool useBezierPath = false;

    [Tooltip("二次贝塞尔：要求在 t* 处经过的必经点（世界/Anchored 坐标与 RectTransform 对齐）。")]
    public Vector2 passThroughPointC = Vector2.zero;

    [Range(0.05f, 0.95f)]
    [Tooltip("二次贝塞尔参数 t*，控制“必经点”在路径上的位置。")]
    public float passTStar = 0.5f;

    [Tooltip("贝塞尔打断策略：重解曲线 or 回到既定轨道。")]
    public BezierInterruptPolicy bezierInterruptPolicy = BezierInterruptPolicy.RecomputeCurve;

    [Header("Position")]
    public bool animatePosition = true;
    [Tooltip("绝对模式：最终 anchoredPosition；相对模式：位移 delta")]
    public Vector2 targetAnchoredPosition = Vector2.zero;

    [Header("Size")]
    public bool animateSize = false;
    [Tooltip("绝对模式：最终 sizeDelta；相对模式：sizeDelta 增量")]
    public Vector2 targetSizeDelta = Vector2.zero;

    [Header("Rotation")]
    public bool animateRotationZ = false;
    [Tooltip("绝对模式：最终 Z 欧拉角；相对模式：Z 增量")]
    public float targetEulerZ = 0f;

    [Header("Pivot (optional, absolute only)")]
    [Tooltip("可选：目标 Pivot（仅建议在绝对模式下使用）。")]
    public Vector2 targetPivot = new Vector2(0.5f, 0.5f);
    public bool animatePivot = false;

    [Header("Alpha")]
    public bool animateAlpha = false;
    [Tooltip("绝对：最终 alpha；相对：alpha 增量（如不需相对，可保持绝对用法）")]
    public float targetAlpha = 1f;

    [Header("Color")]
    public bool animateColor = false;
    public Color targetColor = Color.white;

    [Header("Timing & Ease")]
    public float duration = 0.25f;
    public float delay = 0f;
    public int loops = 0;
    public LoopType loopType = LoopType.Restart;
    public bool unscaledTime = false;

    [Tooltip("使用自定义 AnimationCurve（优先级高于 Ease Type）。")]
    public bool useCustomCurve = false;
    public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("Dotween 的 Ease 类型（当未使用自定义曲线时生效）。")]
    public Ease easeType = Ease.OutQuad;

    [Header("Runtime Options")]
    [Tooltip("距离越近是否按比例缩短时长（可由调用方覆盖时长）。")]
    public bool scaleDurationByDistance = false;

    // 统一将 preset 的缓动/循环/延迟/时间缩放等应用到 Tween
    public void ApplyEaseTo(Tween t)
    {
        if (useCustomCurve && customCurve != null) t.SetEase(customCurve);
        else t.SetEase(easeType);

        t.SetUpdate(unscaledTime);
        if (loops != 0) t.SetLoops(loops, loopType);
        if (delay > 0f) t.SetDelay(delay);
    }
}
