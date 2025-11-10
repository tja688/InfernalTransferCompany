using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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

    [Serializable]
    private class ButtonRuntime
    {
        public RectTransform Rect;
        public float LogicalIndex;
        public float CrossAxisConstant;
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

    [Header("调试 & 可视化")]
    [SerializeField] private bool enableDebugDraw = false;
    [SerializeField] private Color slotGizmoColor = new Color(0f, 0.8f, 1f, 0.75f);
    [SerializeField] private Color buttonGizmoColor = new Color(1f, 0.6f, 0f, 0.75f);
    [SerializeField] private float gizmoRadius = 6f;

    [Header("事件回调")]
    public IndexEvent onSnappedToIndex = new IndexEvent();
    public UnityEvent onEntranceCompleted;
    public UnityEvent onExitCompleted;

    public int CurrentIndex => Mathf.RoundToInt(_scrollPosition);
    public float CurrentVelocity => _velocity;

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
    }

    private void OnEnable()
    {
        EnsureInputAction();
        EnableInputAction();
        BuildRuntimeState();
        RestoreSnapshotInternal();
    }

    private void OnDisable()
    {
        DisableInputAction();
    }

    private void Update()
    {
        if (_slotCount == 0 || _runtimeButtons.Count == 0)
        {
            return;
        }

        HandleInput();
        SimulateMotion();
        ManageRecycling();
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
        _velocity = 0f;
        if (instant)
        {
            _scrollPosition = index;
            _isSnapping = false;
            _snapTarget = index;
            ApplyPositions();
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

        if (restoreSnapshotBeforeEntrance)
        {
            RestoreSnapshotInternal();
        }

        _entrancePlaying = true;
        _exitPlaying = false;

        // 从上方（或左侧）起始，使第一个元素最终落在最后一个槽位。
        float desiredTravel = Mathf.Max(1f, _slotCount - 1f);
        float multiplier = impulseMultiplierOverride > 0f ? impulseMultiplierOverride : entranceImpulseMultiplier;
        float impulse = desiredTravel * Mathf.Max(0.5f, multiplier);
        AddImpulseInternal(impulse);
    }

    /// <summary>
    /// 播放退场：按默认反向滚出界面。
    /// </summary>
    public void PlayExit(float impulseMultiplierOverride = 1f)
    {
        if (_slotCount == 0)
        {
            return;
        }

        _exitPlaying = true;
        _entrancePlaying = false;

        float impulse = Mathf.Max(exitImpulseSlots, _slotCount);
        impulse *= Mathf.Max(0.1f, impulseMultiplierOverride);
        AddImpulseInternal(-impulse);
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
                CrossAxisConstant = GetCrossAxisValue(rect)
            };
            _runtimeButtons.Add(runtime);
        }

        _runtimeButtons.Sort((a, b) => a.LogicalIndex.CompareTo(b.LogicalIndex));

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
            ApplyPositions();
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
    }

    #endregion

    #region 输入与模拟

    private void HandleInput()
    {
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
                if (_exitPlaying)
                {
                    _exitPlaying = false;
                    onExitCompleted?.Invoke();
                }
            }
            return;
        }

        if (Mathf.Abs(_velocity) > Mathf.Epsilon)
        {
            _scrollPosition += _velocity * dt;
            float newVelocity = Mathf.MoveTowards(_velocity, 0f, friction * dt);
            _velocity = Mathf.Clamp(newVelocity, -maxVelocity, maxVelocity);

            if (Mathf.Abs(_velocity) <= minVelocityForSnap)
            {
                _velocity = 0f;
                StartSnap(Mathf.Round(_scrollPosition));
            }
        }
        else
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
                if (_exitPlaying)
                {
                    _exitPlaying = false;
                    onExitCompleted?.Invoke();
                }
            }
        }
    }

    private void ManageRecycling()
    {
        if (_runtimeButtons.Count == 0)
        {
            return;
        }

        float extra = Mathf.Max(0f, _runtimeButtons.Count - _slotCount);
        extra += extraRecycleRange;
        float forwardThreshold = -1f - recyclePadding - extra;
        float backwardThreshold = (_slotCount - 1f) + recyclePadding + extra;
        float wrapSpan = _runtimeButtons.Count > 0 ? _runtimeButtons.Count : _slotCount;

        foreach (var button in _runtimeButtons)
        {
            float relative = button.LogicalIndex - _scrollPosition;
            if (relative < forwardThreshold)
            {
                button.LogicalIndex += wrapSpan;
            }
            else if (relative > backwardThreshold)
            {
                button.LogicalIndex -= wrapSpan;
            }
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
            _lastDispatchedIndex = index;
            onSnappedToIndex?.Invoke(index);
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

