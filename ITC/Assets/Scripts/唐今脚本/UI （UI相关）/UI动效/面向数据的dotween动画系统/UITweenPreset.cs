// MIT License
// ScriptableObject preset for Goal-Driven UI Tween
// - 保留旧字段名：presetName / delay / loops / loopType / unscaledTime / useCustomCurve / customCurve / easeType / targetPivot
// - 新增打断策略枚举与字段（相对/贝塞尔）
// - 统一提供 ApplyEaseTo(Tween) 供 Player/Controller 复用
// [MODIFIED] Split ApplyEaseTo into ApplyTweenSettings and ApplySequenceSettings to fix double delay issue.

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum SecondaryTweenType
{
    Rotation,
    Scale,
    AnchoredPosition,
    Alpha,
    Color
}

[System.Serializable]
public class SecondaryTween
{
    [Tooltip("此轨道名称，仅用于在编辑器中识别")]
    public string name = "Secondary Animation";

    [Tooltip("此动画控制的属性")]
    public SecondaryTweenType propertyType = SecondaryTweenType.Rotation;

    [Tooltip("在主时间轴上的开始时间（秒）")]
    public float startTime = 0f;

    [Tooltip("此动画自身的持续时间（秒）")]
    public float duration = 0.5f;

    [Tooltip("动画的目标值。对于旋转是欧拉角Z，对于缩放是(x,y,z)，对于位置是(x,y)")]
    public Vector3 targetValue = Vector3.zero;

    [Tooltip("缓动类型")]
    public Ease easeType = Ease.Linear;

    [Tooltip("是否使用相对值。勾选后，targetValue 将作为在动画开始瞬间的当前值基础上的增量。")]
    public bool isRelative = true;

    [Tooltip("颜色动画的目标颜色（仅在 propertyType = Color 时使用)")]
    public Color targetColor = Color.white;
}

public enum TimelineEventType
{
    CustomCallback,
    PlayAudio,
    ChangeSprite,
    BroadcastMessage
}

[System.Serializable]
public class TimelineEvent
{
    [Tooltip("事件名称，用于识别")]
    public string name = "New Event";

    [Tooltip("事件在主时间轴上的触发时间点")]
    public float fireTime = 0f;

    [Tooltip("事件类型")]
    public TimelineEventType eventType = TimelineEventType.CustomCallback;

    [Tooltip("【PlayAudio】需要播放的音效片段")]
    public AudioClip audioClip;

    [Tooltip("【ChangeSprite】需要更换到的新 Sprite")]
    public Sprite newSprite;

    [Tooltip("【ChangeSprite】需要操作的目标 Image 组件")]
    public Image targetImage;

    [Tooltip("【BroadcastMessage】要广播的消息名称")]
    public string messageName;

    [Tooltip("【BroadcastMessage】消息的参数（可选）")]
    public string messageParameter;

    [Tooltip("【CustomCallback】自定义回调，可在此挂载任何脚本的公开方法")]
    public UnityEvent customCallback = new UnityEvent();
}

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

    [Header("Secondary Animations")]
    [Tooltip("在主动画播放期间叠加的次级动画轨道")]
    public List<SecondaryTween> secondaryTweens = new List<SecondaryTween>();

    [Header("Timeline Events")]
    [Tooltip("在时间轴特定时间点触发的模块化事件")]
    public List<TimelineEvent> timelineEvents = new List<TimelineEvent>();

    // ===== MODIFICATION START =====
    // 原有的 ApplyEaseTo 方法已被拆分为以下两个方法，以避免双重延遲问题

    /// <summary>
    /// 僅將缓動曲線（Ease/AnimationCurve）應用到 Tween。
    /// </summary>
    public void ApplyTweenSettings(Tween t)
    {
        if (useCustomCurve && customCurve != null)
        {
            t.SetEase(customCurve);
        }
        else
        {
            t.SetEase(easeType);
        }
    }

    /// <summary>
    /// 將序列級別的設定（延遲、循環、時間縮放等）應用到 Sequence。
    /// </summary>
    public void ApplySequenceSettings(Sequence seq)
    {
        seq.SetUpdate(unscaledTime);
        if (loops != 0)
        {
            seq.SetLoops(loops, loopType);
        }
        if (delay > 0f)
        {
            seq.SetDelay(delay);
        }
    }
    // ===== MODIFICATION END =====
}