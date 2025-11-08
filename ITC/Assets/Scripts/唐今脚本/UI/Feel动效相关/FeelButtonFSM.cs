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
    MMEventListener<MMStateChangeEvent<FeelButtonFSM.ButtonStates>>,
    MMEventListener<PanelChangedEvent>
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
    [Tooltip("是否托管射线检测，在按钮或面板转场期间禁止射线。")]
    [SerializeField]
    private bool _manageRaycasts = true;

    [Tooltip("按钮自身状态切换时是否阻止射线。")]
    [SerializeField]
    private bool _blockRaycastsDuringLocalTransition = true;

    [Tooltip("收到 PanelChangedEvent 时是否阻止射线。")]
    [SerializeField]
    private bool _blockRaycastsOnPanelChanged = true;

    [Tooltip("PanelChangedEvent 发生后阻止射线的持续时间（秒，实时计时）。")]
    [SerializeField]
    private float _panelChangedRaycastBlockDuration = 0.35f;

    [Header("反馈管理")]
    [Tooltip("记录并管理所有子级 MMF_Player，在转场时将强制停止它们。")]
    [SerializeField]
    private bool _autoManageFeedbackPlayers = true;

    private MMStateMachine<ButtonStates> _stateMachine;
    private bool _isTransitioning = false; // 关键的“锁”，防止动画播放时再次触发
    private bool _localTransitionLockedRaycast = false;
    private Coroutine _stateTransitionCoroutine;
    private Coroutine _panelRaycastCoroutine;

    private readonly List<MMF_Player> _managedPlayers = new List<MMF_Player>();

    private CanvasGroup _canvasGroup;
    private readonly List<Graphic> _raycastGraphics = new List<Graphic>();
    private readonly Dictionary<Graphic, bool> _raycastRestoreStates = new Dictionary<Graphic, bool>();
    private bool _canvasGroupRestoreState = true;
    private int _raycastLockCount = 0;

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
    }

    #region 状态机事件监听
    
    // 当脚本启用时，开始监听状态变化事件
    void OnEnable()
    {
        this.MMEventStartListening<MMStateChangeEvent<ButtonStates>>();
        this.MMEventStartListening<PanelChangedEvent>();

        if (_manageRaycasts)
        {
            CacheRaycastTargets();
        }

        if (_autoManageFeedbackPlayers && _managedPlayers.Count == 0)
        {
            RefreshManagedPlayers();
        }
    }

    // 当脚本禁用时，停止监听，防止内存泄漏
    void OnDisable()
    {
        this.MMEventStopListening<MMStateChangeEvent<ButtonStates>>();
        this.MMEventStopListening<PanelChangedEvent>();

        if (_panelRaycastCoroutine != null)
        {
            StopCoroutine(_panelRaycastCoroutine);
            _panelRaycastCoroutine = null;
        }

        StopActiveTransition();
        ResetRaycastLocks();
    }

    /// <summary>
    /// 这是 Feel 状态机的核心：当状态发生变化时，这个函数会被自动调用
    /// </summary>
    public void OnMMEvent(MMStateChangeEvent<ButtonStates> stateChangeEvent)
    {
        // 仅在状态转换时播放（这符合你的要求）
        if (stateChangeEvent.NewState == stateChangeEvent.PreviousState)
        {
            return;
        }

        switch (stateChangeEvent.NewState)
        {
            case ButtonStates.Hover:
                // 播放动效A (Hover)
                StartStateTransition(hoverFeedback);
                break;
                
            case ButtonStates.Idle:
                // 播放动效B (Idle)
                StartStateTransition(idleFeedback);
                break;
        }
    }

    #endregion

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

        // 如果正在播放转场动画，则忽略本次输入
        if (_isTransitioning)
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

        // 如果正在播放转场动画，则忽略本次输入
        if (_isTransitioning)
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
    /// 接收到 PanelChangedEvent 时的处理：阻止射线并终止所有反馈。
    /// </summary>
    public void OnMMEvent(PanelChangedEvent panelChangedEvent)
    {
        if (_blockRaycastsOnPanelChanged)
        {
            if (_panelRaycastCoroutine != null)
            {
                StopCoroutine(_panelRaycastCoroutine);
                _panelRaycastCoroutine = null;
                UnlockRaycast();
            }

            _panelRaycastCoroutine = StartCoroutine(PanelRaycastBlockRoutine());
        }

        StopActiveTransition();
        StopAllManagedFeedbacks();
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

        if (!force)
        {
            if (_isTransitioning || _stateMachine.CurrentState == targetState)
            {
                return false;
            }
        }
        else
        {
            StopActiveTransition();
        }

        _stateMachine.ChangeState(targetState);
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

    private IEnumerator PanelRaycastBlockRoutine()
    {
        LockRaycast();

        float duration = Mathf.Max(0f, _panelChangedRaycastBlockDuration);
        if (duration > 0f)
        {
            yield return new WaitForSecondsRealtime(duration);
        }
        else
        {
            yield return null;
        }

        UnlockRaycast();
        _panelRaycastCoroutine = null;
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

        if (_panelChangedRaycastBlockDuration < 0f)
        {
            _panelChangedRaycastBlockDuration = 0f;
        }
    }
#endif
}