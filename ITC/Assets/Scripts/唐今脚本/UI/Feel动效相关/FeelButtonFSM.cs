using UnityEngine;
using UnityEngine.EventSystems; // 导入UI事件系统
using MoreMountains.Tools;
using MoreMountains.Feedbacks;
using System.Collections;

/// <summary>
/// 使用 Feel 状态机和 MMFeedbacks 制作的一个防连点（等待动画播放完毕）的按钮。
/// 需要挂载在有 EventTrigger 或实现了 IPointer... 接口的 UI 对象上。
/// </summary>
public class FeelButtonFSM : MonoBehaviour, 
    IPointerEnterHandler, 
    IPointerExitHandler,  
    MMEventListener<MMStateChangeEvent<FeelButtonFSM.ButtonStates>> // <--- 改成这个
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

    private MMStateMachine<ButtonStates> _stateMachine;
    private bool _isTransitioning = false; // 关键的“锁”，防止动画播放时再次触发

    /// <summary>
    /// 初始化状态机
    /// </summary>
    void Awake()
    {
        // 初始化状态机，'this.gameObject' 指明宿主，'true' 允许它广播事件
        _stateMachine = new MMStateMachine<ButtonStates>(this.gameObject, true);
        
        // 设定初始状态为空闲
        _stateMachine.ChangeState(ButtonStates.Idle);
    }

    #region 状态机事件监听
    
    // 当脚本启用时，开始监听状态变化事件
    void OnEnable()
    {
        this.MMEventStartListening<MMStateChangeEvent<ButtonStates>>();
    }

    // 当脚本禁用时，停止监听，防止内存泄漏
    void OnDisable()
    {
        this.MMEventStopListening<MMStateChangeEvent<ButtonStates>>();
    }

    /// <summary>
    /// 这是 Feel 状态机的核心：当状态发生变化时，这个函数会被自动调用
    /// </summary>
    public void OnMMEvent(MMStateChangeEvent<ButtonStates> stateChangeEvent)
    {
        // 仅在状态转换时播放（这符合你的要求）
        switch (stateChangeEvent.NewState)
        {
            case ButtonStates.Hover:
                // 播放动效A (Hover)
                StartCoroutine(PlayAnimationCoroutine(hoverFeedback));
                break;
                
            case ButtonStates.Idle:
                // 播放动效B (Idle)
                StartCoroutine(PlayAnimationCoroutine(idleFeedback));
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
        // 如果正在播放转场动画，则忽略本次输入
        if (_isTransitioning)
        {
            return; 
        }

        // 如果当前是 Idle 状态，则请求切换到 Hover 状态
        if (_stateMachine.CurrentState == ButtonStates.Idle)
        {
            _stateMachine.ChangeState(ButtonStates.Hover);
        }
    }

    /// <summary>
    /// 当鼠标离开 UI 区域时调用
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // 如果正在播放转场动画，则忽略本次输入
        if (_isTransitioning)
        {
            return; 
        }

        // 如果当前是 Hover 状态，则请求切换到 Idle 状态
        if (_stateMachine.CurrentState == ButtonStates.Hover)
        {
            _stateMachine.ChangeState(ButtonStates.Idle);
        }
    }

    #endregion

    /// <summary>
    /// 播放 Feedback 动画并设置“锁”的协程
    /// </summary>
    private IEnumerator PlayAnimationCoroutine(MMFeedbacks feedbackToPlay)
    {
        if (feedbackToPlay == null)
        {
            yield break; // 如果 Feedback 未指定，直接退出
        }

        // 1. 上锁：禁止新的状态转换
        _isTransitioning = true; 

        // 2. 播放动效，并等待它播放完成
        // PlayFeedbacksCoroutine 会自动在 Feedback 播放完毕后才继续执行
        yield return feedbackToPlay.PlayFeedbacksCoroutine(this.transform.position);

        // 3. 解锁：允许下一次状态转换
        _isTransitioning = false; 
    }
}