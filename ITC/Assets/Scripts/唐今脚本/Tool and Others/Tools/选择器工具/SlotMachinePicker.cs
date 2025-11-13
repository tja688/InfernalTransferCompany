using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// 【新增】定义吸附方向
public enum SnapDirection
{
    Neutral, // 中立（例如：通过 SnapToIndex 强制指定，或没有移动）
    Forward, // 正向（与 Default Scroll Direction 一致）
    Backward // 反向（与 Default Scroll Direction 相反）
}

/// <summary>
/// 老虎机/滚轮式选择器驱动器。
/// 支持惯性滚动、步进吸附、按钮复用、外部冲量注入与入场/退场控制。
/// 该组件专为纯脚本驱动的 UI 按钮列而设计，不依赖 ScrollRect 或布局组件。
/// </summary>
[DisallowMultipleComponent]
public class SlotMachinePicker : MonoBehaviour
{
    public enum SlotAxis
    {
        Vertical,
        Horizontal
    }

    public enum ScrollDirection
    {
        Up,
        Down
    }

    [Serializable]
    public class IndexEvent : UnityEvent<int> { }

    // 【新增】定义一个可以传递"索引"和"方向"的新事件类型
    [Serializable]
    public class SnapEvent : UnityEvent<int, SnapDirection> { }

    [Serializable]
    private class ButtonRuntime
    {
        public RectTransform Rect;
        public float LogicalIndex;
        public float CrossAxisConstant;
        public Vector3 BaseScale;
        public Tween ActiveTween;
        public bool IsFocused;
        public bool TweeningToFocus;
        public Vector3 LastTweenTargetScale;
    }

    [Serializable]
    private struct Snapshot
    {
        public float ScrollPosition;
        public float Velocity;
        public float[] LogicalIndices;
    }

    [Header("基础引用")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private Transform slotsRoot;
    [SerializeField] private Transform buttonsRoot;
    [SerializeField] private List<RectTransform> slotAnchors = new List<RectTransform>();
    [SerializeField] private List<RectTransform> buttonItems = new List<RectTransform>();
    [Tooltip("启用后自动读取 slotsRoot 下的子 RectTransform 作为槽位锚点（仅在手动调用“重新采集”或 OnValidate 时生效）。")]
    [SerializeField] private bool autoCollectSlots = true;
    [Tooltip("启用后自动读取 buttonsRoot 下的子 RectTransform 作为按钮元素（仅在手动调用“重新采集”或 OnValidate 时生效）。")]
    [SerializeField] private bool autoCollectButtons = true;

    [Header("布局设置")]
    [SerializeField] private SlotAxis axis = SlotAxis.Vertical;
    [SerializeField] private ScrollDirection defaultScrollDirection = ScrollDirection.Down;
    [Tooltip("为 0 表示自动根据槽位锚点计算。非 0 时强制使用该数值（单位：UI 坐标单位）。")]
    [SerializeField] private float slotSpacingOverride = 0f;
    [Tooltip("为 true 时使用 Time.unscaledDeltaTime 进行驱动。")]
    [SerializeField] private bool useUnscaledDeltaTime = false;

    [Header("输入控制")]
    [Tooltip("是否启用内置鼠标滚轮响应。")]
    [SerializeField] private bool enableMouseScroll = true;
    [Tooltip("默认 Unity 鼠标向上滚动的值为正。勾选后会取反，以“向下滚动 = 正向”对齐默认方向。")]
    [SerializeField] private bool invertMouseWheel = true;
    [Tooltip("滚轮值转换为冲量的倍率，数值越大滚动越灵敏。")]
    [SerializeField] private float wheelImpulseMultiplier = 0.12f;
    [Tooltip("滚轮输入的绝对值超过该阈值时判定为“惯性滚动（大力）”，否则进入步进模式。")]
    [SerializeField] private float flingThreshold = 0.6f;
    [Tooltip("两次步进之间的最小间隔（秒）。")]
    [SerializeField] private float stepCooldown = 0.08f;
    [Tooltip("可选：绑定自定义滚轮 Input Action。为空时自动创建 <Mouse>/scroll 动作。")]
    [SerializeField] private InputActionReference scrollActionReference;

    [Header("动力学")]
    [Tooltip("速度（slot/秒）最大值限制。")]
    [SerializeField] private float maxVelocity = 18f;
    [Tooltip("滚动衰减，数值越大减速越快。单位：slot/秒^2。")]
    [SerializeField] private float friction = 12f;
    [Tooltip("触发吸附动画的速度阈值。速度低于该值时开始吸附。")]
    [SerializeField] private float minVelocityForSnap = 0.2f;
    [Tooltip("吸附动画的最大速度（slot/秒）。")]
    [SerializeField] private float snapSpeed = 16f;
    [Tooltip("当与目标槽位的差距低于该值时视为吸附完成。")]
    [SerializeField] private float snapThreshold = 0.01f;
    [Tooltip("吸附动画使用的缓动曲线。")]
    [SerializeField] private AnimationCurve snapEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("复用与边界")]
    [Tooltip("额外的复用缓冲。数值越大，按钮离开视图后会停留更久再复用。")]
    [SerializeField] private float recyclePadding = 0.75f;
    [Tooltip("按钮离开视图多远算“出画”。默认按槽位数自动换算，此处可额外补偿。单位：slot。")]
    [SerializeField] private float extraRecycleRange = 0f;

    [Header("入场 / 退场")]
    [Tooltip("执行 PlayEntrance 时使用的基础冲量倍率。实际冲量 = max(slotCount-1, EntranceMinImpulse) * multiplier。")]
    [SerializeField] private float entranceImpulseMultiplier = 1.0f;
    [Tooltip("执行 PlayExit 时使用的基础冲量（会根据方向自动取符号）。")]
    [SerializeField] private float exitImpulseSlots = 2f;
    [Tooltip("启用后，PlayEntrance 会从快照恢复到初始状态。")]
    [SerializeField] private bool restoreSnapshotBeforeEntrance = true;

    [Header("聚焦缩放效果")]
    [Tooltip("启用后，根据指定槽位对按钮执行聚焦缩放动画。")]
    [SerializeField] private bool enableFocusScaleEffect = false;
    [Tooltip("聚焦槽位序号。-1 表示自动使用居中槽位（偶数数量时向上取整）。")]
    [SerializeField] private int focusSlotIndexOverride = -1;
    [Tooltip("聚焦时相对于初始缩放的倍率。")]
    [SerializeField] private Vector3 focusScaleMultiplier = new Vector3(1.15f, 1.15f, 1f);
    [Tooltip("按钮进入聚焦槽位时的缩放持续时间。")]
    [SerializeField] private float focusScaleDuration = 0.2f;
    [Tooltip("按钮进入聚焦槽位时的缓动。")]
    [SerializeField] private Ease focusScaleEase = Ease.OutQuad;
    [Tooltip("按钮离开聚焦槽位时的缩放持续时间。")]
    [SerializeField] private float focusRecoverDuration = 0.15f;
    [Tooltip("按钮离开聚焦槽位时的缓动。")]
    [SerializeField] private Ease focusRecoverEase = Ease.OutQuad;
    [Tooltip("触发聚焦缩放所需的距离阈值（单位：槽位）。")]
    [SerializeField, Range(0.01f, 0.5f)] private float focusActivationThreshold = 0.2f;
    [Tooltip("在循环复用或退场隐藏之前是否强制重置按钮缩放。")]
    [SerializeField] private bool forceScaleResetOnRecycle = true;

    [Header("调试 & 可视化")]
    [SerializeField] private bool enableDebugDraw = false;
    [SerializeField] private Color slotGizmoColor = new Color(0f, 0.8f, 1f, 0.75f);
    [SerializeField] private Color buttonGizmoColor = new Color(1f, 0.6f, 0f, 0.75f);
    [SerializeField] private float gizmoRadius = 6f;

    [Header("事件回调")]
    public IndexEvent onSnappedToIndex = new IndexEvent();
    public SnapEvent onSnappedWithDirection = new SnapEvent(); // <--- 【新增】
    public UnityEvent onEntranceCompleted;
    public UnityEvent onExitCompleted;

    public int CurrentIndex => Mathf.RoundToInt(_scrollPosition);
    public float CurrentVelocity => _velocity;
    public bool IsExiting => _exitPlaying; // 【新增】暴露退场状态，供外部组件检查

    public bool MouseInputEnabled
    {
        get => enableMouseScroll;
        set => enableMouseScroll = value;
    }

    // --- runtime state ---
    private readonly List<ButtonRuntime> _runtimeButtons = new List<ButtonRuntime>(32);
    private readonly List<float> _slotAxisValues = new List<float>(16);

    private InputAction _runtimeScrollAction;
    private float _scrollPosition;
    private float _velocity;
    private bool _isSnapping;
    private float _snapStartTime;
    private float _snapDuration;
    private float _snapOrigin;
    private float _snapTarget;
    private int _lastDispatchedIndex;
    private float _lastStepTime = float.MinValue;
    private bool _entrancePlaying;
    private bool _exitPlaying;

    private Snapshot _initialSnapshot;
    private bool _snapshotValid;
    private float _slotStep;
    private float _firstSlotAxisValue;
    private int _slotCount;
    private bool _isHidden = true; // <-- 【新增】状态：控制按钮是否已隐藏
    private bool _wasFocusEffectEnabled;

    private float DeltaTime => useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime;
    private float TimeNow => useUnscaledDeltaTime ? Time.unscaledTime : Time.time;
    private int DirectionSign => defaultScrollDirection == ScrollDirection.Down ? 1 : -1;

    #region Unity 生命周期

    private void OnValidate()
    {
        TryAutoCollect();
        ClampInspectorValues();
    }

    private void Awake()
    {
        BuildRuntimeState();
        HideAllButtons(); // <-- 【修改】启动时立即隐藏
    }

    private void OnEnable()
    {
        EnsureInputAction();
        EnableInputAction();
        // 【修改】如果仍处于隐藏状态，确保再次隐藏（以防在编辑器中 OnDisable）
        if (_isHidden)
        {
            HideAllButtons();
        }
    }

    private void OnDisable()
    {
        DisableInputAction();
        ResetFocusEffectState(true);
    }

    private void Update()
    {
        if (_wasFocusEffectEnabled && !enableFocusScaleEffect)
        {
            ResetFocusEffectState(true);
        }
        _wasFocusEffectEnabled = enableFocusScaleEffect;

        // 【修改】如果隐藏了，就不执行任何操作
        if (_slotCount == 0 || _runtimeButtons.Count == 0 || _isHidden)
        {
            return;
        }

        HandleInput();
        SimulateMotion();
        ManageRecycling();
        UpdateFocusEffect();
        ApplyPositions();
        DispatchEventsIfNeeded();
    }

    private void OnDrawGizmosSelected()
    {
        if (!enableDebugDraw)
        {
            return;
        }

#if UNITY_EDITOR
        DrawDebugGizmos();
#endif
    }

    #endregion

    #region 聚焦缩放效果

    private void UpdateFocusEffect(bool instant = false)
    {
        if (!enableFocusScaleEffect || _runtimeButtons.Count == 0 || _slotCount == 0)
        {
            return;
        }

        int focusSlotIndex = GetEffectiveFocusSlotIndex();
        if (focusSlotIndex < 0)
        {
            ResetFocusEffectState(true);
            return;
        }

        ButtonRuntime focusedButton = null;
        float bestDistance = float.MaxValue;

        foreach (var button in _runtimeButtons)
        {
            if (button.Rect == null)
            {
                continue;
            }

            if (!button.Rect.gameObject.activeInHierarchy)
            {
                AnimateDefocus(button, true);
                continue;
            }

            float distance = Mathf.Abs((button.LogicalIndex - _scrollPosition) - focusSlotIndex);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                focusedButton = button;
            }
        }

        bool hasValidFocus = focusedButton != null && bestDistance <= focusActivationThreshold;

        foreach (var button in _runtimeButtons)
        {
            if (button.Rect == null)
            {
                continue;
            }

            if (!button.Rect.gameObject.activeInHierarchy)
            {
                AnimateDefocus(button, true);
                continue;
            }

            if (hasValidFocus && button == focusedButton)
            {
                AnimateFocus(button, instant);
            }
            else
            {
                AnimateDefocus(button, instant);
            }
        }
    }

    private void AnimateFocus(ButtonRuntime button, bool instant)
    {
        if (button == null || button.Rect == null)
        {
            return;
        }

        Vector3 targetScale = GetFocusTargetScale(button);

        if (instant || focusScaleDuration <= Mathf.Epsilon)
        {
            CompleteTween(button);
            button.Rect.localScale = targetScale;
            button.LastTweenTargetScale = targetScale;
            button.TweeningToFocus = false;
            button.ActiveTween = null;
            button.IsFocused = true;
            return;
        }

        if (!instant &&
            button.ActiveTween != null &&
            button.ActiveTween.IsActive() &&
            button.TweeningToFocus &&
            Approximately(button.LastTweenTargetScale, targetScale))
        {
            return;
        }

        if (button.IsFocused)
        {
            if (Approximately(button.Rect.localScale, targetScale))
            {
                return;
            }
        }

        CompleteTween(button);
        button.ActiveTween = button.Rect.DOScale(targetScale, focusScaleDuration)
            .SetEase(focusScaleEase)
            .SetUpdate(useUnscaledDeltaTime)
            .OnComplete(() =>
            {
                button.ActiveTween = null;
                button.Rect.localScale = targetScale;
                button.LastTweenTargetScale = targetScale;
                button.TweeningToFocus = false;
            });
        button.LastTweenTargetScale = targetScale;
        button.TweeningToFocus = true;
        button.IsFocused = true;
    }

    private void AnimateDefocus(ButtonRuntime button, bool instant)
    {
        if (button == null || button.Rect == null)
        {
            return;
        }

        Vector3 baseScale = button.BaseScale;

        if (instant || focusRecoverDuration <= Mathf.Epsilon)
        {
            CompleteTween(button);
            button.Rect.localScale = baseScale;
            button.LastTweenTargetScale = baseScale;
            button.TweeningToFocus = false;
            button.ActiveTween = null;
            button.IsFocused = false;
            return;
        }

        if (!instant &&
            button.ActiveTween != null &&
            button.ActiveTween.IsActive() &&
            !button.TweeningToFocus &&
            Approximately(button.LastTweenTargetScale, baseScale))
        {
            return;
        }

        if (!button.IsFocused &&
            (button.ActiveTween == null || !button.ActiveTween.IsActive()) &&
            Approximately(button.Rect.localScale, baseScale))
        {
            return;
        }

        CompleteTween(button);
        button.ActiveTween = button.Rect.DOScale(baseScale, focusRecoverDuration)
            .SetEase(focusRecoverEase)
            .SetUpdate(useUnscaledDeltaTime)
            .OnComplete(() =>
            {
                button.ActiveTween = null;
                button.Rect.localScale = baseScale;
                button.LastTweenTargetScale = baseScale;
                button.TweeningToFocus = false;
            });
        button.LastTweenTargetScale = baseScale;
        button.TweeningToFocus = false;
        button.IsFocused = false;
    }

    private void ResetFocusEffectState(bool immediate)
    {
        if (_runtimeButtons == null)
        {
            return;
        }

        foreach (var button in _runtimeButtons)
        {
            AnimateDefocus(button, immediate);
        }
    }

    private Vector3 GetFocusTargetScale(ButtonRuntime button)
    {
        return Vector3.Scale(button.BaseScale, focusScaleMultiplier);
    }

    private int GetEffectiveFocusSlotIndex()
    {
        if (_slotCount <= 0)
        {
            return -1;
        }

        if (focusSlotIndexOverride >= 0)
        {
            return Mathf.Clamp(focusSlotIndexOverride, 0, _slotCount - 1);
        }

        return Mathf.CeilToInt((_slotCount - 1) * 0.5f);
    }

    private void CompleteTween(ButtonRuntime button)
    {
        if (button.ActiveTween != null)
        {
            if (button.ActiveTween.IsActive())
            {
                button.ActiveTween.Kill(false);
            }
            button.ActiveTween = null;
        }
        button.TweeningToFocus = false;
    }

    private static bool Approximately(Vector3 a, Vector3 b, float tolerance = 0.001f)
    {
        return (a - b).sqrMagnitude <= tolerance * tolerance;
    }

    #endregion

    #region Public API

    /// <summary>
    /// 重新采集槽位与按钮引用。
    /// </summary>
    public void RebuildReferences()
    {
        TryAutoCollect(true);
        BuildRuntimeState();
    }

    /// <summary>
    /// 外部注入冲量（单位：slot/秒）。正值沿默认方向滚动。
    /// </summary>
    public void InjectImpulse(float impulse)
    {
        AddImpulseInternal(impulse);
    }

    /// <summary>
    /// 强制吸附到指定索引。
    /// </summary>
    public void SnapToIndex(int index, bool instant = false)
    {
        // 【新增】如果隐藏了，先激活
        if (_isHidden)
        {
            RestoreSnapshotInternal(); // 会自动激活
        }
        
        _velocity = 0f;
        if (instant)
        {
            _scrollPosition = index;
            _isSnapping = false;
            _snapTarget = index;
            ApplyPositions();
            UpdateFocusEffect(true);
            DispatchEventsIfNeeded(true);
        }
        else
        {
            StartSnap(index);
        }
    }

    /// <summary>
    /// 重置当前速度。
    /// </summary>
    public void ClearVelocity()
    {
        _velocity = 0f;
    }

    /// <summary>
    /// 保存当前状态快照（包括按钮逻辑索引和滚动位置）。
    /// </summary>
    public void SaveSnapshot()
    {
        CaptureSnapshot();
    }

    /// <summary>
    /// 将状态恢复到最近一次保存的快照。
    /// </summary>
    public void RestoreSnapshot()
    {
        RestoreSnapshotInternal();
        ApplyPositions();
        UpdateFocusEffect(true);
    }

    /// <summary>
    /// 播放入场：按钮从快照初始状态重新铺满槽位并带有惯性。
    /// </summary>
    public void PlayEntrance(float impulseMultiplierOverride = -1f)
    {
        if (_slotCount == 0)
        {
            return;
        }

        if (_isHidden)
        {
            // 1. 准备：恢复快照或重置索引
            if (restoreSnapshotBeforeEntrance)
            {
                RestoreSnapshotInternal(); // RestoreSnapshotInternal 会自动激活按钮并设置 _isHidden = false
            }
            else
            {
                // 手动重置按钮的逻辑索引
                for (int i = 0; i < _runtimeButtons.Count; i++)
                {
                    _runtimeButtons[i].LogicalIndex = i;
                }

                // 激活所有按钮实体
                foreach (var button in _runtimeButtons)
                {
                    if (button.Rect) button.Rect.gameObject.SetActive(true);
                }
                _isHidden = false;
            }

            // 2. 将所有按钮“预置”到屏幕外（“第-1个槽”之前）
            // 我们将滚动位置设置为一个负值，这样按钮就都在“上方”
            _scrollPosition = -_runtimeButtons.Count - recyclePadding;
            _velocity = 0f;
            _isSnapping = false;

            // 3. 立即应用一次位置，让它们“瞬移”到屏幕外的起始点
            ApplyPositions();
        }
        else if (restoreSnapshotBeforeEntrance)
        {
            // 如果不是隐藏状态（例如连续调用），但设置了 restore，依然恢复
            RestoreSnapshotInternal();
        }

        _entrancePlaying = true;
        _exitPlaying = false;

        // 4. 注入冲量
        float desiredTravel = Mathf.Max(1f, _slotCount - 1f);
        float multiplier = impulseMultiplierOverride > 0f ? impulseMultiplierOverride : entranceImpulseMultiplier;
        float impulse = desiredTravel * Mathf.Max(0.5f, multiplier);

        // 【新增】确保冲量足够大，能让所有预置的按钮都滚进来
        // 这个冲量需要大到足以覆盖从“预置点”(-N)到“槽位”(0~slotCount)的距离
        float entranceBaseImpulse = _runtimeButtons.Count + _slotCount + 10f; // 10f 作为额外缓冲
        impulse = Mathf.Max(entranceBaseImpulse, impulse); // 取你计算的冲量和基础冲量中较大的一个

        AddImpulseInternal(impulse);
    }

    /// <summary>
    /// 播放退场：按默认反向滚出界面。
    /// </summary>
    public void PlayExit(float impulseMultiplierOverride = 1f)
    {
        // 【修改】如果已隐藏，则不执行
        if (_slotCount == 0 || _isHidden)
        {
            return;
        }

        _exitPlaying = true;
        _entrancePlaying = false;

        float impulse = Mathf.Max(exitImpulseSlots, _slotCount);
        impulse *= Mathf.Max(0.1f, impulseMultiplierOverride);
        AddImpulseInternal(-impulse); // 给予负向冲量
    }

    #endregion

    #region 构建与初始化

    private void BuildRuntimeState()
    {
        _runtimeButtons.Clear();
        _slotAxisValues.Clear();
        _slotCount = 0;
        _slotStep = 0f;
        _firstSlotAxisValue = 0f;

        if (slotAnchors == null)
        {
            slotAnchors = new List<RectTransform>();
        }

        if (buttonItems == null)
        {
            buttonItems = new List<RectTransform>();
        }

        for (int i = 0; i < buttonItems.Count; i++)
        {
            var rect = buttonItems[i];
            if (!rect) continue;

            var runtime = new ButtonRuntime
            {
                Rect = rect,
                LogicalIndex = i,
                CrossAxisConstant = GetCrossAxisValue(rect),
                BaseScale = rect.localScale,
                ActiveTween = null,
                IsFocused = false,
                TweeningToFocus = false,
                LastTweenTargetScale = rect.localScale
            };
            _runtimeButtons.Add(runtime);
        }

        _runtimeButtons.Sort((a, b) => a.LogicalIndex.CompareTo(b.LogicalIndex));

        ResetFocusEffectState(true);
        _wasFocusEffectEnabled = enableFocusScaleEffect;

        _slotCount = slotAnchors.Count;
        if (_slotCount == 0)
        {
            return;
        }

        for (int i = 0; i < slotAnchors.Count; i++)
        {
            RectTransform rect = slotAnchors[i];
            if (!rect) continue;
            _slotAxisValues.Add(GetAxisValue(rect));
        }

        if (_slotAxisValues.Count == 0)
        {
            _slotCount = 0;
            return;
        }

        _slotCount = _slotAxisValues.Count;
        _slotAxisValues.Sort();

        _firstSlotAxisValue = _slotAxisValues[0];
        _slotStep = ComputeSlotStep();

        if (_runtimeButtons.Count > 0)
        {
            CaptureSnapshot();
            // ApplyPositions(); // <--- 【修改】不在此时应用位置，等待 HideAllButtons
            DispatchEventsIfNeeded(true);
        }
    }

    private float ComputeSlotStep()
    {
        if (_slotCount <= 1)
        {
            return slotSpacingOverride != 0f ? slotSpacingOverride : 50f;
        }

        if (Mathf.Abs(slotSpacingOverride) > Mathf.Epsilon)
        {
            return slotSpacingOverride;
        }

        float sum = 0f;
        int pairs = 0;
        for (int i = 0; i < _slotAxisValues.Count - 1; i++)
        {
            float delta = _slotAxisValues[i + 1] - _slotAxisValues[i];
            if (Mathf.Approximately(delta, 0f)) continue;
            sum += delta;
            pairs++;
        }

        if (pairs == 0)
        {
            return slotSpacingOverride != 0f ? slotSpacingOverride : 50f;
        }

        return sum / pairs;
    }

    private void CaptureSnapshot()
    {
        _initialSnapshot.ScrollPosition = _scrollPosition;
        _initialSnapshot.Velocity = 0f;
        _initialSnapshot.LogicalIndices = new float[_runtimeButtons.Count];
        for (int i = 0; i < _runtimeButtons.Count; i++)
        {
            _initialSnapshot.LogicalIndices[i] = _runtimeButtons[i].LogicalIndex;
        }
        _snapshotValid = true;
    }

    private void RestoreSnapshotInternal()
    {
        if (!_snapshotValid)
        {
            return;
        }

        _scrollPosition = _initialSnapshot.ScrollPosition;
        _velocity = _initialSnapshot.Velocity;
        _isSnapping = false;

        float[] source = _initialSnapshot.LogicalIndices;
        int count = Mathf.Min(source.Length, _runtimeButtons.Count);
        for (int i = 0; i < count; i++)
        {
            _runtimeButtons[i].LogicalIndex = source[i];
        }

        // 【新增】恢复快照时，要确保按钮是可见的
        if (_runtimeButtons.Count > 0)
        {
            foreach (var button in _runtimeButtons)
            {
                if (button.Rect)
                {
                    AnimateDefocus(button, true);
                    button.Rect.gameObject.SetActive(true);
                }
            }
            _isHidden = false; // 标记为可见
        }
    }

    private void TryAutoCollect(bool force = false)
    {
#if UNITY_EDITOR
        if (autoCollectSlots && (force || !Application.isPlaying))
        {
            slotAnchors.Clear();
            if (slotsRoot)
            {
                var rects = slotsRoot.GetComponentsInChildren<RectTransform>(true);
                foreach (var r in rects)
                {
                    if (r == slotsRoot) continue;
                    slotAnchors.Add(r);
                }
            }
        }

        if (autoCollectButtons && (force || !Application.isPlaying))
        {
            buttonItems.Clear();
            if (buttonsRoot)
            {
                foreach (Transform child in buttonsRoot)
                {
                    if (child is RectTransform rect)
                    {
                        buttonItems.Add(rect);
                    }
                }
            }
        }
#endif
    }

    private void ClampInspectorValues()
    {
        wheelImpulseMultiplier = Mathf.Max(0f, wheelImpulseMultiplier);
        flingThreshold = Mathf.Max(0f, flingThreshold);
        stepCooldown = Mathf.Max(0f, stepCooldown);
        friction = Mathf.Max(0f, friction);
        maxVelocity = Mathf.Max(0.1f, maxVelocity);
        snapSpeed = Mathf.Max(0.1f, snapSpeed);
        snapThreshold = Mathf.Max(0.0001f, snapThreshold);
        recyclePadding = Mathf.Max(0f, recyclePadding);
        focusScaleDuration = Mathf.Max(0f, focusScaleDuration);
        focusRecoverDuration = Mathf.Max(0f, focusRecoverDuration);
        focusActivationThreshold = Mathf.Clamp(focusActivationThreshold, 0.01f, 0.5f);
        focusScaleMultiplier.x = Mathf.Max(0f, focusScaleMultiplier.x);
        focusScaleMultiplier.y = Mathf.Max(0f, focusScaleMultiplier.y);
        focusScaleMultiplier.z = Mathf.Max(0f, focusScaleMultiplier.z);
    }

    #endregion

    #region 输入与模拟

    private void HandleInput()
    {
        // 【修复】退场模式下禁用所有滚轮输入
        if (_exitPlaying)
        {
            return;
        }

        if (!enableMouseScroll || _runtimeScrollAction == null)
        {
            return;
        }

        Vector2 scrollValue = _runtimeScrollAction.ReadValue<Vector2>();
        float raw = scrollValue.y;
        if (Mathf.Approximately(raw, 0f))
        {
            return;
        }

        if (invertMouseWheel)
        {
            raw = -raw;
        }

        float impulse = raw * wheelImpulseMultiplier;
        float abs = Mathf.Abs(impulse);

        if (abs >= flingThreshold)
        {
            AddImpulseInternal(impulse);
            _lastStepTime = TimeNow;
        }
        else
        {
            if (TimeNow - _lastStepTime >= stepCooldown)
            {
                int stepDirection = impulse > 0f ? 1 : -1;
                Step(stepDirection);
                _lastStepTime = TimeNow;
            }
        }
    }

    private void SimulateMotion()
    {
        float dt = DeltaTime;
        if (dt <= 0f)
        {
            return;
        }

        if (_isSnapping)
        {
            float elapsed = TimeNow - _snapStartTime;
            float t = _snapDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / _snapDuration);
            float eased = snapEase != null ? snapEase.Evaluate(t) : t;
            _scrollPosition = Mathf.Lerp(_snapOrigin, _snapTarget, eased);

            if (t >= 1f - Mathf.Epsilon)
            {
                _isSnapping = false;
                _scrollPosition = _snapTarget;
                _velocity = 0f;

                if (_entrancePlaying)
                {
                    _entrancePlaying = false;
                    onEntranceCompleted?.Invoke();
                }
                // 【修改】退场完成的事件回调移至 ManageRecycling
                // if (_exitPlaying)
                // {
                //     _exitPlaying = false;
                //     onExitCompleted?.Invoke();
                // }
            }
            return;
        }

        if (Mathf.Abs(_velocity) > Mathf.Epsilon)
        {
            _scrollPosition += _velocity * dt;
            float newVelocity = Mathf.MoveTowards(_velocity, 0f, friction * dt);
            _velocity = Mathf.Clamp(newVelocity, -maxVelocity, maxVelocity);

            // 【修改】退场时禁止自动吸附
            if (Mathf.Abs(_velocity) <= minVelocityForSnap && !_exitPlaying)
            {
                _velocity = 0f;
                StartSnap(Mathf.Round(_scrollPosition));
            }
        }
        // 【修改】退场时禁止空闲吸附
        else if (!_exitPlaying)
        {
            float nearest = Mathf.Round(_scrollPosition);
            if (Mathf.Abs(nearest - _scrollPosition) > snapThreshold * 0.5f)
            {
                StartSnap(nearest);
            }
            else
            {
                _scrollPosition = nearest;
                if (_entrancePlaying)
                {
                    _entrancePlaying = false;
                    onEntranceCompleted?.Invoke();
                }
                // 【修改】退场完成的事件回调移至 ManageRecycling
                // if (_exitPlaying)
                // {
                //     _exitPlaying = false;
                //     onExitCompleted?.Invoke();
                // }
            }
        }
        // 如果正在退场且速度为0，_exitPlaying 会在 ManageRecycling 中被处理
    }

    private void ManageRecycling()
    {
        if (_runtimeButtons.Count == 0)
        {
            return;
        }

        float extra = Mathf.Max(0f, _runtimeButtons.Count - _slotCount);
        extra += extraRecycleRange;
        float forwardThreshold = -recyclePadding - extra; // <--- 使用我们上次修复的阈值
        float backwardThreshold = (_slotCount - 1f) + recyclePadding + extra;
        float wrapSpan = _runtimeButtons.Count > 0 ? _runtimeButtons.Count : _slotCount;

        bool allHidden = _exitPlaying; // 仅在退场模式下检查是否全部隐藏
        bool focusEffectActive = enableFocusScaleEffect;
        bool forceResetScale = focusEffectActive && forceScaleResetOnRecycle;

        foreach (var button in _runtimeButtons)
        {
            float relative = button.LogicalIndex - _scrollPosition;

            if (_exitPlaying)
            {
                // 【退场逻辑】: 不循环，只隐藏
                if (relative < forwardThreshold || relative > backwardThreshold)
                {
                    if (focusEffectActive)
                    {
                        AnimateDefocus(button, forceResetScale);
                    }
                    if (button.Rect && button.Rect.gameObject.activeSelf)
                    {
                        button.Rect.gameObject.SetActive(false);
                    }
                }
                else if (button.Rect && button.Rect.gameObject.activeSelf)
                {
                    allHidden = false; // 只要有一个还在屏幕内（且激活），就没退完
                }
            }
            else
            {
                // 【正常循环逻辑】
                if (relative < forwardThreshold)
                {
                    if (focusEffectActive)
                    {
                        AnimateDefocus(button, forceResetScale);
                    }
                    button.LogicalIndex += wrapSpan;
                }
                else if (relative > backwardThreshold)
                {
                    if (focusEffectActive)
                    {
                        AnimateDefocus(button, forceResetScale);
                    }
                    button.LogicalIndex -= wrapSpan;
                }
            }
        }

        if (_exitPlaying && allHidden)
        {
            // 【退场完成】
            _exitPlaying = false;
            _isHidden = true; // 标记为已隐藏
            _velocity = 0f;
            onExitCompleted?.Invoke(); // 在这里回调
        }
    }

    private void ApplyPositions()
    {
        foreach (var button in _runtimeButtons)
        {
            float relative = button.LogicalIndex - _scrollPosition;
            float axisValue = _firstSlotAxisValue + relative * _slotStep;
            SetAxisValue(button.Rect, axisValue, button.CrossAxisConstant);
        }
    }

    private void StartSnap(float target)
    {
        _isSnapping = true;
        _snapTarget = target;
        _snapOrigin = _scrollPosition;
        float distance = Mathf.Abs(target - _snapOrigin);
        _snapDuration = distance <= Mathf.Epsilon ? 0f : Mathf.Clamp(distance / snapSpeed, 0.05f, 0.6f);
        _snapStartTime = TimeNow;
    }

    private void DispatchEventsIfNeeded(bool force = false)
    {
        int index = CurrentIndex;
        if (force || index != _lastDispatchedIndex)
        {
            // 【修改】计算方向
            SnapDirection direction = SnapDirection.Neutral;
            int oldIndex = _lastDispatchedIndex; // 存储旧索引
            
            _lastDispatchedIndex = index; // 更新索引

            if (!force)
            {
                if (index > oldIndex)
                {
                    direction = SnapDirection.Forward;
                }
                else if (index < oldIndex)
                {
                    direction = SnapDirection.Backward;
                }
            }
            
            // 触发旧事件（保持兼容性）
            onSnappedToIndex?.Invoke(index);
            
            // 【新增】触发带方向的新事件
            onSnappedWithDirection?.Invoke(index, direction);
        }
    }

    private void AddImpulseInternal(float impulse)
    {
        float signedImpulse = impulse * DirectionSign;
        _velocity += signedImpulse;
        _velocity = Mathf.Clamp(_velocity, -maxVelocity, maxVelocity);
        _isSnapping = false;
    }

    private void Step(int stepDirection)
    {
        float signed = DirectionSign * Mathf.Sign(stepDirection);
        float target = Mathf.Round(_scrollPosition + signed);
        _velocity = 0f;
        StartSnap(target);
    }

    #endregion

    #region 输入系统处理

    private void EnsureInputAction()
    {
        if (scrollActionReference && scrollActionReference.action != null)
        {
            _runtimeScrollAction = scrollActionReference.action;
            return;
        }

        if (_runtimeScrollAction == null)
        {
            _runtimeScrollAction = new InputAction("SlotMachinePickerScroll", InputActionType.Value, "<Mouse>/scroll", expectedControlType: "Vector2");
        }
    }

    private void EnableInputAction()
    {
        if (_runtimeScrollAction != null && !_runtimeScrollAction.enabled)
        {
            _runtimeScrollAction.Enable();
        }
    }

    private void DisableInputAction()
    {
        if (scrollActionReference && scrollActionReference.action != null)
        {
            return;
        }

        if (_runtimeScrollAction != null && _runtimeScrollAction.enabled)
        {
            _runtimeScrollAction.Disable();
        }
    }

    #endregion

    #region 坐标工具

    private float GetAxisValue(RectTransform rect)
    {
        Vector2 pos = rect.anchoredPosition;
        return axis == SlotAxis.Vertical ? pos.y : pos.x;
    }

    private float GetCrossAxisValue(RectTransform rect)
    {
        Vector2 pos = rect.anchoredPosition;
        return axis == SlotAxis.Vertical ? pos.x : pos.y;
    }

    private void SetAxisValue(RectTransform rect, float axisValue, float crossAxis)
    {
        Vector2 pos = rect.anchoredPosition;
        if (axis == SlotAxis.Vertical)
        {
            pos.x = crossAxis;
            pos.y = axisValue;
        }
        else
        {
            pos.x = axisValue;
            pos.y = crossAxis;
        }

        rect.anchoredPosition = pos;
    }
    
    // 【新增】辅助函数，用于隐藏所有按钮并设置状态
    private void HideAllButtons()
    {
        if (_runtimeButtons == null) return;
        foreach (var button in _runtimeButtons)
        {
            AnimateDefocus(button, true);
            if (button.Rect) button.Rect.gameObject.SetActive(false);
        }
        _isHidden = true;
    }

    #endregion

    #region 调试绘制

#if UNITY_EDITOR
    private void DrawDebugGizmos()
    {
        if (_slotCount > 0)
        {
            UnityEditor.Handles.color = slotGizmoColor;
            foreach (var slot in slotAnchors)
            {
                if (!slot) continue;
                UnityEditor.Handles.DrawSolidDisc(slot.position, Vector3.forward, gizmoRadius);
            }
        }

        UnityEditor.Handles.color = buttonGizmoColor;
        foreach (var btn in buttonItems)
        {
            if (!btn) continue;
            UnityEditor.Handles.DrawWireDisc(btn.position, Vector3.forward, gizmoRadius * 0.85f);
        }

        if (viewport)
        {
            UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.35f);
            Vector3 min = viewport.TransformPoint(viewport.rect.min);
            Vector3 max = viewport.TransformPoint(viewport.rect.max);
            Vector3 p0 = new Vector3(min.x, min.y, min.z);
            Vector3 p1 = new Vector3(min.x, max.y, min.z);
            Vector3 p2 = new Vector3(max.x, max.y, min.z);
            Vector3 p3 = new Vector3(max.x, min.y, min.z);
            UnityEditor.Handles.DrawAAPolyLine(4f, new[] { p0, p1, p2, p3, p0 });
        }
    }
#endif

    #endregion
}