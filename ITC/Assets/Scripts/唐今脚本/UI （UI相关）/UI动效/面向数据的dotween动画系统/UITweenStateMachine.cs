using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

/// <summary>
/// 响应UI交互事件，并根据全局 GamePanelStateMachine 的当前面板状态，播放对应的动画和轨道。
/// </summary>
[RequireComponent(typeof(UITweenPlayer))]
public class UITweenStateMachine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    #region Data Structures

    /// <summary>
    /// 定义了UI元素可能存在的状态
    /// </summary>
    public enum UIState
    {
        Normal,   // 正常/默认状态
        Hover,    // 鼠标悬停状态
        Pressed,  // 鼠标按下状态
        Selected, // 选中状态（例如 Toggle on）
        Disabled  // 禁用状态
    }

    /// <summary>
    /// 单个UI状态的动画/轨道绑定
    /// </summary>
    [System.Serializable]
    public class UIStateBinding
    {
        public UIState state;

        [Header("动画预设 (Preset)")]
        public string onEnterPresetName;
        public bool reverseOnExit = true;
        public string onExitPresetName;

        [Header("动画轨道 (Track)")]
        public bool playTrackOnEnter = false;
        public UITweenTrack onEnterTrack;
        public string onEnterTrackName;
        public bool reverseTrackOnExit = false;
        public UITweenTrack.ReversePlayMode onExitTrackReverseMode = UITweenTrack.ReversePlayMode.Default;
    }

    /// <summary>
    /// 单个面板所对应的状态配置集合
    /// </summary>
    [System.Serializable]
    public class PanelStateConfiguration
    {
        [Tooltip("此配置对应的面板名称")]
        public string panelName;
        public List<UIStateBinding> stateBindings = new List<UIStateBinding>();
    }

    #endregion

    [Tooltip("包含所有面板状态配置的列表")]
    public List<PanelStateConfiguration> panelConfigurations = new List<PanelStateConfiguration>();

    private UITweenPlayer _player;
    private UIState _currentState = UIState.Normal;
    private bool _isInteractable = true;

    // --- 全局状态机相关 ---
    private IDisposable _panelStateSubscription;
    private string _currentGlobalPanel = "None";

    void Awake()
    {
        _player = GetComponent<UITweenPlayer>();
    }

    void OnEnable()
    {
        // 订阅全局面板状态机
        _panelStateSubscription = GamePanelStateMachine.Instance.Subscribe(OnPanelStateChanged);
    }

    void OnDisable()
    {
        // 取消订阅
        _panelStateSubscription?.Dispose();
    }

    private void OnPanelStateChanged(PanelStateChange change)
    {
        _currentGlobalPanel = change.CurrentPanel;
        // 当全局面板切换时，可能需要将当前UI元素重置到Normal状态
        // 如果当前不是Normal，则触发一次从当前状态到Normal的退出逻辑
        if (_currentState != UIState.Normal)
        {
            TransitionTo(UIState.Normal, true);
        }
    }

    /// <summary>
    /// 核心状态转换逻辑
    /// </summary>
    /// <param name="newState">要转换到的新状态</param>
    /// <param name="isGlobalReset">是否是因全局面板切换而触发的重置</param>
    private void TransitionTo(UIState newState, bool isGlobalReset = false)
    {
        if (!_isInteractable || _currentState == newState) return;

        // 查找当前全局面板对应的配置
        var config = panelConfigurations.Find(c => c.panelName == _currentGlobalPanel);
        if (config == null) return; // 如果当前面板没有任何配置，则不响应

        UIState oldState = _currentState;
        _currentState = newState;

        // --- 执行退出逻辑 ---
        var oldBinding = config.stateBindings.Find(b => b.state == oldState);
        if (oldBinding != null)
        {
            // 播放退出的动画预设
            if (oldBinding.reverseOnExit && !string.IsNullOrEmpty(oldBinding.onEnterPresetName))
            {
                PlayPreset(oldBinding.onEnterPresetName, true, oldState, newState);
            }
            else if (!string.IsNullOrEmpty(oldBinding.onExitPresetName))
            {
                PlayPreset(oldBinding.onExitPresetName, false, oldState, newState);
            }

            // 播放退出的轨道
            if (oldBinding.playTrackOnEnter && oldBinding.reverseTrackOnExit && oldBinding.onEnterTrack != null)
            {
                PlayTrack(oldBinding.onEnterTrack, oldBinding.onEnterTrackName, true, oldBinding.onExitTrackReverseMode, oldState, newState);
            }
        }

        // 如果是全局重置，则只执行退出逻辑，不执行进入逻辑
        if (isGlobalReset) return;

        // --- 执行进入逻辑 ---
        var newBinding = config.stateBindings.Find(b => b.state == newState);
        if (newBinding != null)
        {
            // 播放进入的动画预设
            if (!string.IsNullOrEmpty(newBinding.onEnterPresetName))
            {
                PlayPreset(newBinding.onEnterPresetName, false, oldState, newState);
            }

            // 播放进入的轨道
            if (newBinding.playTrackOnEnter && newBinding.onEnterTrack != null)
            {
                PlayTrack(newBinding.onEnterTrack, newBinding.onEnterTrackName, false, newBinding.onExitTrackReverseMode, oldState, newState);
            }
        }
    }

    private void PlayPreset(string presetName, bool reversed, UIState from, UIState to)
    {
        if (string.IsNullOrEmpty(presetName)) return;
        string detail = $"UIState: {from} -> {to} | Preset: {presetName}";
        using (UITweenCallContext.BeginScope(this, "UITweenStateMachine", gameObject.name, detail))
        {
            if (reversed) _player.PlayReversedByName(presetName);
            else _player.PlayByName(presetName);
        }
    }

    private void PlayTrack(UITweenTrack track, string trackName, bool reversed, UITweenTrack.ReversePlayMode reverseMode, UIState from, UIState to)
    {
        if (track == null || string.IsNullOrEmpty(trackName)) return;
        string detail = $"UIState: {from} -> {to} | Track: {trackName}";
        using (UITweenCallContext.BeginScope(this, "UITweenStateMachine", gameObject.name, detail))
        {
            if (reversed) track.PlayTrackReverse(trackName, reverseMode);
            else track.PlayTrack(trackName);
        }
    }

    #region Event Handlers
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_currentState != UIState.Pressed) TransitionTo(UIState.Hover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_currentState != UIState.Pressed) TransitionTo(UIState.Normal);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TransitionTo(UIState.Pressed);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_currentState == UIState.Pressed)
        {
            TransitionTo(eventData.pointerCurrentRaycast.gameObject == gameObject ? UIState.Hover : UIState.Normal);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (!_isInteractable) return;
        if (isSelected)
        {
            if (_currentState != UIState.Selected) TransitionTo(UIState.Selected);
        }
        else
        {
            if (_currentState == UIState.Selected) TransitionTo(UIState.Normal);
        }
    }

    public void SetDisabled(bool isDisabled)
    {
        _isInteractable = !isDisabled;
        if (isDisabled)
        {
            if (_currentState != UIState.Disabled) TransitionTo(UIState.Disabled);
        }
        else
        {
            if (_currentState == UIState.Disabled) TransitionTo(UIState.Normal);
        }
    }
    #endregion
}
