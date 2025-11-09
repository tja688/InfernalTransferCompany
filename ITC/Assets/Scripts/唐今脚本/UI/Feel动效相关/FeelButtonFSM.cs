using System;
using UnityEngine;
using UnityEngine.EventSystems; // 导入UI事件系统
using UnityEngine.UI;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 使用 Feel 状态机和 MMFeedbacks 制作的一个防连点（等待动画播放完毕）的按钮。
/// 需要挂载在有 EventTrigger 或实现了 IPointer... 接口的 UI 对象上。
/// </summary>
public class FeelButtonFSM : MonoBehaviour, 
    IPointerEnterHandler, 
    IPointerExitHandler,
    IGameEventListener<bool>,
    IGameEventListener<PanelChangedPayload>
{
/// <summary>
    /// 按钮的两种状态：空闲（鼠标离开）和悬停（鼠标进入）
    /// </summary>
    public enum ButtonStates 
    { 
        Idle, 
        Hover 
    }

    [Header("Feedbacks")]
    [Tooltip("动效A：当鼠标进入时播放")]
    public MMFeedbacks hoverFeedback;
    
    [Tooltip("动效B：当鼠标离开时播放")]
    public MMFeedbacks idleFeedback;

    [Header("输入选项")]
    [Tooltip("是否使用指针事件驱动状态切换。关闭后请通过外部脚本调用 RequestState 来驱动。")]
    [SerializeField]
    private bool _usePointerEvents = true;

    [Header("射线检测控制")]
    [Tooltip("是否托管射线检测，在按钮转场期间禁止射线。")]
    [SerializeField]
    private bool _manageRaycasts = true;

    [Tooltip("按钮自身状态切换时是否阻止射线。")]
    [SerializeField]
    private bool _blockRaycastsDuringLocalTransition = true;

    [Header("反馈管理")]
    [Tooltip("记录并管理所有子级 MMF_Player，在转场时将强制停止它们。")]
    [SerializeField]
    private bool _autoManageFeedbackPlayers = true;

    [Header("事件响应")]
    [Tooltip("布尔类型事件：用于外部控制按钮射线检测的启用/禁用。true=禁用射线检测，false=恢复射线检测。")]
    [SerializeField]
    private BoolGameEvent _raycastControlEvent;

    [Tooltip("来自面板系统的切换事件，用于根据当前面板切换动效预设。")]
    [SerializeField]
    private PanelChangedGameEvent _panelChangedEvent;

    [Header("面板专属动效预设")]
    [Tooltip("开启后可针对不同面板配置独立的鼠标进入/离开动效。")]
    [SerializeField]
    private bool _usePanelSpecificPresets = false;

    [Tooltip("面板名称库，用于在 Inspector 中提供下拉选择（自动从 PanelManager 获取）。")]
    [SerializeField]
    private GamePanelLibrarySO _panelLibrary;

    [Tooltip("面板 -> 动效 预设列表。未匹配到时将使用通用动效。")]
    [SerializeField]
    private List<PanelFeedbackPreset> _panelPresets = new List<PanelFeedbackPreset>();

    private MMStateMachine<ButtonStates> _stateMachine;
    private bool _isTransitioning = false; // 关键的"锁"，防止动画播放时再次触发
    private bool _localTransitionLockedRaycast = false;
    private Coroutine _stateTransitionCoroutine;

    private readonly List<MMF_Player> _managedPlayers = new List<MMF_Player>();

    private CanvasGroup _canvasGroup;
    private readonly List<Graphic> _raycastGraphics = new List<Graphic>();
    private readonly Dictionary<Graphic, bool> _raycastRestoreStates = new Dictionary<Graphic, bool>();
    private bool _canvasGroupRestoreState = true;
    private int _raycastLockCount = 0;

    private bool _externalRaycastLocked = false; // 标记是否被外部事件锁定

    private MMFeedbacks _activeHoverFeedback;
    private MMFeedbacks _activeIdleFeedback;
    private string _activePanelName = string.Empty;

    [Serializable]
    private class PanelFeedbackPreset
    {
        [Tooltip("目标面板名称（需与 GamePanelLibrarySO 中的名称一致）。")]
        public string panelName;

        [Tooltip("当鼠标进入该面板的按钮时使用的动效覆盖。为空时沿用通用动效。")]
        public MMFeedbacks hoverFeedback;

        [Tooltip("当鼠标离开该面板的按钮时使用的动效覆盖。为空时沿用通用动效。")]
        public MMFeedbacks idleFeedback;
    }

    /// <summary>
    /// 初始化状态机
    /// </summary>
    void Awake()
    {
        // 初始化状态机，'this.gameObject' 指明宿主，'true' 允许它广播事件
        _stateMachine = new MMStateMachine<ButtonStates>(this.gameObject, true);
        
        // 设定初始状态为空闲
        _stateMachine.ChangeState(ButtonStates.Idle);

        CacheRaycastTargets();
        RefreshManagedPlayers();
        AutoFindPanelLibrary();
        ResetActiveFeedbacks();
    }

    void OnEnable()
    {
        // 注册射线检测控制事件
        if (_raycastControlEvent != null)
        {
            _raycastControlEvent.RegisterListener(this);
        }
        else if (_manageRaycasts)
        {
            Debug.LogWarning($"{name}: 未配置 RaycastControlEvent，将无法响应外部射线检测控制事件。", this);
        }

        // 注册面板切换事件
        if (_panelChangedEvent != null)
        {
            _panelChangedEvent.RegisterListener(this);
        }
        else if (_usePanelSpecificPresets)
        {
            Debug.LogWarning($"{name}: 未配置 PanelChangedGameEvent，将无法响应面板切换事件。", this);
        }

        // 自动查找面板库（运行时）
        if (_panelLibrary == null)
        {
            AutoFindPanelLibrary();
        }

        if (_manageRaycasts)
        {
            CacheRaycastTargets();
        }

        if (_autoManageFeedbackPlayers && _managedPlayers.Count == 0)
        {
            RefreshManagedPlayers();
        }

        // 应用当前面板的预设
        if (PanelManager.Instance != null)
        {
            _activePanelName = PanelManager.Instance.CurrentPanel;
            ApplyPanelPreset(_activePanelName);
        }
    }

    void OnDisable()
    {
        if (_raycastControlEvent != null)
        {
            _raycastControlEvent.UnregisterListener(this);
        }

        if (_panelChangedEvent != null)
        {
            _panelChangedEvent.UnregisterListener(this);
        }

        StopActiveTransition();
        ResetRaycastLocks();
        _externalRaycastLocked = false;
    }
    #region UI 输入事件

    /// <summary>
    /// 当鼠标进入 UI 区域时调用
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_usePointerEvents)
        {
            return;
        }

        // 如果正在播放转场动画，或外部已锁定射线检测，则忽略本次输入
        if (_isTransitioning || _externalRaycastLocked)
        {
            return; 
        }

        RequestState(ButtonStates.Hover);
    }

    /// <summary>
    /// 当鼠标离开 UI 区域时调用
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_usePointerEvents)
        {
            return;
        }

        // 如果正在播放转场动画，或外部已锁定射线检测，则忽略本次输入
        if (_isTransitioning || _externalRaycastLocked)
        {
            return; 
        }

        RequestState(ButtonStates.Idle);
    }

    #endregion

    /// <summary>
    /// 播放 Feedback 动画并设置“锁”的协程
    /// </summary>
    private IEnumerator PlayAnimationCoroutine(MMFeedbacks feedbackToPlay)
    {
        if (feedbackToPlay == null)
        {
            EndLocalTransition();
            _stateTransitionCoroutine = null;
            yield break; // 如果 Feedback 未指定，直接退出
        }

        BeginLocalTransition();

        // 2. 播放动效，并等待它播放完成
        // PlayFeedbacksCoroutine 会自动在 Feedback 播放完毕后才继续执行
        yield return feedbackToPlay.PlayFeedbacksCoroutine(this.transform.position);

        EndLocalTransition();
        _stateTransitionCoroutine = null;
    }

    /// <summary>
    /// 接收到布尔事件时的处理：根据布尔值控制射线检测的启用/禁用。
    /// true = 禁用射线检测，false = 恢复射线检测。
    /// </summary>
    void IGameEventListener<bool>.OnEventRaised(bool shouldDisableRaycast)
    {
        HandleRaycastControlEvent(shouldDisableRaycast);
    }

    /// <summary>
    /// 接收到面板切换事件时的处理：更新动效预设。
    /// </summary>
    void IGameEventListener<PanelChangedPayload>.OnEventRaised(PanelChangedPayload payload)
    {
        HandlePanelChanged(payload);
    }

    /// <summary>
    /// 手动请求状态切换，可供外部脚本调用。
    /// </summary>
    /// <param name="targetState">目标状态</param>
    /// <param name="force">是否强制打断当前转场</param>
    public bool RequestState(ButtonStates targetState, bool force = false)
    {
        if (_stateMachine == null)
        {
            return false;
        }

        ButtonStates previousState = _stateMachine.CurrentState;

        if (!force)
        {
            if (_isTransitioning || previousState == targetState)
            {
                return false;
            }
        }
        else
        {
            StopActiveTransition();
            previousState = _stateMachine.CurrentState;
        }

        _stateMachine.ChangeState(targetState);
        HandleStateChanged(previousState, targetState);
        return true;
    }

    /// <summary>
    /// 当前状态
    /// </summary>
    public ButtonStates CurrentState => _stateMachine != null ? _stateMachine.CurrentState : ButtonStates.Idle;

    /// <summary>
    /// 是否处于转场中
    /// </summary>
    public bool IsTransitioning => _isTransitioning;

    private void HandleStateChanged(ButtonStates previousState, ButtonStates newState)
    {
        if (previousState == newState)
        {
            return;
        }

        // 如果外部已锁定射线检测（面板切换动画期间），则不播放按钮动画
        if (_externalRaycastLocked)
        {
            return;
        }

        switch (newState)
        {
            case ButtonStates.Hover:
                StartStateTransition(GetActiveHoverFeedback());
                break;

            case ButtonStates.Idle:
                StartStateTransition(GetActiveIdleFeedback());
                break;
        }
    }

    private void StartStateTransition(MMFeedbacks feedbacks)
    {
        if (feedbacks == null)
        {
            StopAllManagedFeedbacks();
            return;
        }

        if (_stateTransitionCoroutine != null)
        {
            StopCoroutine(_stateTransitionCoroutine);
            _stateTransitionCoroutine = null;
        }

        _stateTransitionCoroutine = StartCoroutine(PlayAnimationCoroutine(feedbacks));
    }

    private void BeginLocalTransition()
    {
        if (_isTransitioning)
        {
            return;
        }

        _isTransitioning = true;

        StopAllManagedFeedbacks();

        if (_manageRaycasts && _blockRaycastsDuringLocalTransition)
        {
            LockRaycast();
            _localTransitionLockedRaycast = true;
        }
        else
        {
            _localTransitionLockedRaycast = false;
        }
    }

    private void EndLocalTransition()
    {
        if (!_isTransitioning)
        {
            return;
        }

        _isTransitioning = false;

        if (_localTransitionLockedRaycast)
        {
            UnlockRaycast();
            _localTransitionLockedRaycast = false;
        }
    }

    private void StopActiveTransition()
    {
        if (_stateTransitionCoroutine != null)
        {
            StopCoroutine(_stateTransitionCoroutine);
            _stateTransitionCoroutine = null;
        }

        if (_isTransitioning)
        {
            EndLocalTransition();
        }
    }

    private void RefreshManagedPlayers()
    {
        if (!_autoManageFeedbackPlayers)
        {
            return;
        }

        _managedPlayers.Clear();
        _managedPlayers.AddRange(GetComponentsInChildren<MMF_Player>(true));
    }

    private void StopAllManagedFeedbacks()
    {
        if (!_autoManageFeedbackPlayers)
        {
            return;
        }

        for (int i = _managedPlayers.Count - 1; i >= 0; i--)
        {
            MMF_Player player = _managedPlayers[i];
            if (player == null)
            {
                _managedPlayers.RemoveAt(i);
                continue;
            }

            player.StopFeedbacks();
        }
    }

    /// <summary>
    /// 处理外部射线检测控制事件。
    /// </summary>
    /// <param name="shouldDisableRaycast">true=禁用射线检测，false=恢复射线检测</param>
    private void HandleRaycastControlEvent(bool shouldDisableRaycast)
    {
        if (!_manageRaycasts)
        {
            return;
        }

        if (shouldDisableRaycast)
        {
            // 外部请求禁用射线检测
            if (!_externalRaycastLocked)
            {
                LockRaycast();
                _externalRaycastLocked = true;
            }

            // 停止当前转场和所有反馈，避免在面板切换动画期间播放按钮动画
            StopActiveTransition();
            StopAllManagedFeedbacks();
        }
        else
        {
            // 外部请求恢复射线检测
            if (_externalRaycastLocked)
            {
                UnlockRaycast();
                _externalRaycastLocked = false;
            }
        }
    }

    /// <summary>
    /// 处理面板切换事件：更新动效预设。
    /// </summary>
    private void HandlePanelChanged(PanelChangedPayload payload)
    {
        _activePanelName = payload.NewPanelName;
        ApplyPanelPreset(_activePanelName);
    }

    /// <summary>
    /// 应用指定面板的动效预设。
    /// </summary>
    private void ApplyPanelPreset(string panelName)
    {
        if (!_usePanelSpecificPresets)
        {
            ResetActiveFeedbacks();
            return;
        }

        PanelFeedbackPreset preset = FindPreset(panelName);
        if (preset != null)
        {
            _activeHoverFeedback = preset.hoverFeedback != null ? preset.hoverFeedback : hoverFeedback;
            _activeIdleFeedback = preset.idleFeedback != null ? preset.idleFeedback : idleFeedback;
        }
        else
        {
            ResetActiveFeedbacks();
        }
    }

    /// <summary>
    /// 查找指定面板名称的预设。
    /// </summary>
    private PanelFeedbackPreset FindPreset(string panelName)
    {
        if (string.IsNullOrEmpty(panelName))
        {
            return null;
        }

        for (int i = 0; i < _panelPresets.Count; i++)
        {
            PanelFeedbackPreset preset = _panelPresets[i];
            if (preset == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(preset.panelName) &&
                string.Equals(preset.panelName, panelName, StringComparison.Ordinal))
            {
                return preset;
            }
        }

        return null;
    }

    /// <summary>
    /// 重置为通用动效。
    /// </summary>
    private void ResetActiveFeedbacks()
    {
        _activeHoverFeedback = hoverFeedback;
        _activeIdleFeedback = idleFeedback;
    }

    /// <summary>
    /// 自动查找场景中的 PanelManager 并获取其面板库。
    /// </summary>
    private void AutoFindPanelLibrary()
    {
        if (_panelLibrary != null)
        {
            return; // 已手动配置，不自动查找
        }

        if (PanelManager.Instance != null)
        {
            // 通过反射获取 PanelManager 的 _panelLibrary 字段
            var panelManagerType = typeof(PanelManager);
            var fieldInfo = panelManagerType.GetField("_panelLibrary", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fieldInfo != null)
            {
                var library = fieldInfo.GetValue(PanelManager.Instance) as GamePanelLibrarySO;
                if (library != null)
                {
                    _panelLibrary = library;
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                    #endif
                }
            }
        }
    }

    private MMFeedbacks GetActiveHoverFeedback()
    {
        return _activeHoverFeedback != null ? _activeHoverFeedback : hoverFeedback;
    }

    private MMFeedbacks GetActiveIdleFeedback()
    {
        return _activeIdleFeedback != null ? _activeIdleFeedback : idleFeedback;
    }

    private void CacheRaycastTargets()
    {
        if (!_manageRaycasts)
        {
            return;
        }

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _raycastGraphics.Clear();
            _raycastGraphics.AddRange(GetComponentsInChildren<Graphic>(true));
        }
    }

    private void CleanRaycastGraphics()
    {
        for (int i = _raycastGraphics.Count - 1; i >= 0; i--)
        {
            if (_raycastGraphics[i] == null)
            {
                _raycastGraphics.RemoveAt(i);
            }
        }
    }

    private void CaptureRaycastState()
    {
        if (!_manageRaycasts)
        {
            return;
        }

        _raycastRestoreStates.Clear();

        if (_canvasGroup != null)
        {
            _canvasGroupRestoreState = _canvasGroup.blocksRaycasts;
            return;
        }

        CleanRaycastGraphics();
        foreach (Graphic graphic in _raycastGraphics)
        {
            if (graphic == null)
            {
                continue;
            }

            _raycastRestoreStates[graphic] = graphic.raycastTarget;
        }
    }

    private void DisableRaycastTargets()
    {
        if (!_manageRaycasts)
        {
            return;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
            return;
        }

        CleanRaycastGraphics();
        foreach (Graphic graphic in _raycastGraphics)
        {
            if (graphic == null)
            {
                continue;
            }

            graphic.raycastTarget = false;
        }
    }

    private void RestoreRaycastTargets()
    {
        if (!_manageRaycasts)
        {
            return;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = _canvasGroupRestoreState;
            return;
        }

        foreach (KeyValuePair<Graphic, bool> entry in _raycastRestoreStates)
        {
            if (entry.Key == null)
            {
                continue;
            }

            entry.Key.raycastTarget = entry.Value;
        }
    }

    private void LockRaycast()
    {
        if (!_manageRaycasts)
        {
            return;
        }

        if (_raycastLockCount == 0)
        {
            CaptureRaycastState();
            DisableRaycastTargets();
        }

        _raycastLockCount++;
    }

    private void UnlockRaycast()
    {
        if (!_manageRaycasts || _raycastLockCount == 0)
        {
            return;
        }

        _raycastLockCount--;
        if (_raycastLockCount == 0)
        {
            RestoreRaycastTargets();
        }
    }

    private void ResetRaycastLocks()
    {
        if (!_manageRaycasts)
        {
            return;
        }

        if (_raycastLockCount > 0)
        {
            _raycastLockCount = 0;
            RestoreRaycastTargets();
        }
    }

    private void OnTransformChildrenChanged()
    {
        RefreshManagedPlayers();
        CacheRaycastTargets();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            CacheRaycastTargets();
            RefreshManagedPlayers();
        }
    }
#endif
}