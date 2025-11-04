using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
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
    /// 配置项类型
    /// </summary>
    public enum ConfigItemType
    {
        Preset,      // 动画预设
        Track,       // 动画轨道
        ExternalPlayer, // 外部Player
        UnityEvent   // Unity事件
    }

    /// <summary>
    /// 动画预设配置项
    /// </summary>
    [System.Serializable]
    public class PresetConfigItem
    {
        public string onEnterPresetName;
        public bool reverseOnExit = true;
        public string onExitPresetName;
        public UITweenPlayer.BaselineCaptureMode onEnterBaselineMode = UITweenPlayer.BaselineCaptureMode.CurrentState;
        public UITweenPlayer.BaselineCaptureMode onExitBaselineMode = UITweenPlayer.BaselineCaptureMode.CurrentState;
    }

    /// <summary>
    /// 动画轨道配置项
    /// </summary>
    [System.Serializable]
    public class TrackConfigItem
    {
        public UITweenTrack onEnterTrack;
        public string onEnterTrackName;
        public bool reverseTrackOnExit = false;
        public UITweenTrack.ReversePlayMode onExitTrackReverseMode = UITweenTrack.ReversePlayMode.Default;
    }

    /// <summary>
    /// 外部Player配置项
    /// </summary>
    [System.Serializable]
    public class ExternalPlayerConfigItem
    {
        public UITweenPlayer externalPlayer;
        public string onEnterPresetName;
        public bool reverseOnExit = true;
        public string onExitPresetName;
    }

    /// <summary>
    /// Unity事件配置项
    /// </summary>
    [System.Serializable]
    public class UnityEventConfigItem
    {
        public UnityEvent onEnterEvent = new UnityEvent();
        public UnityEvent onExitEvent = new UnityEvent();
    }

    /// <summary>
    /// 状态配置项（包装器）
    /// </summary>
    [System.Serializable]
    public class StateConfigItem
    {
        public ConfigItemType itemType;
        public PresetConfigItem presetConfig;
        public TrackConfigItem trackConfig;
        public ExternalPlayerConfigItem externalPlayerConfig;
        public UnityEventConfigItem unityEventConfig;

        public StateConfigItem(ConfigItemType type)
        {
            itemType = type;
            switch (type)
            {
                case ConfigItemType.Preset:
                    presetConfig = new PresetConfigItem();
                    break;
                case ConfigItemType.Track:
                    trackConfig = new TrackConfigItem();
                    break;
                case ConfigItemType.ExternalPlayer:
                    externalPlayerConfig = new ExternalPlayerConfigItem();
                    break;
                case ConfigItemType.UnityEvent:
                    unityEventConfig = new UnityEventConfigItem();
                    break;
            }
        }
    }

    /// <summary>
    /// 单个UI状态的动画/轨道绑定
    /// </summary>
    [System.Serializable]
    public class UIStateBinding
    {
        public UIState state;
        public List<StateConfigItem> configItems = new List<StateConfigItem>();
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
            foreach (var configItem in oldBinding.configItems)
            {
                ProcessConfigItemExit(configItem, oldState, newState);
            }
        }

        // 如果是全局重置，则只执行退出逻辑，不执行进入逻辑
        if (isGlobalReset) return;

        // --- 执行进入逻辑 ---
        var newBinding = config.stateBindings.Find(b => b.state == newState);
        if (newBinding != null)
        {
            foreach (var configItem in newBinding.configItems)
            {
                ProcessConfigItemEnter(configItem, oldState, newState);
            }
        }
    }

    /// <summary>
    /// 处理配置项的进入逻辑
    /// </summary>
    private void ProcessConfigItemEnter(StateConfigItem configItem, UIState from, UIState to)
    {
        switch (configItem.itemType)
        {
            case ConfigItemType.Preset:
                if (configItem.presetConfig != null && !string.IsNullOrEmpty(configItem.presetConfig.onEnterPresetName))
                {
                    PlayPreset(configItem.presetConfig.onEnterPresetName, false, from, to, configItem.presetConfig.onEnterBaselineMode);
                }
                break;

            case ConfigItemType.Track:
                if (configItem.trackConfig != null && configItem.trackConfig.onEnterTrack != null && !string.IsNullOrEmpty(configItem.trackConfig.onEnterTrackName))
                {
                    PlayTrack(configItem.trackConfig.onEnterTrack, configItem.trackConfig.onEnterTrackName, false, configItem.trackConfig.onExitTrackReverseMode, from, to);
                }
                break;

            case ConfigItemType.ExternalPlayer:
                if (configItem.externalPlayerConfig != null && configItem.externalPlayerConfig.externalPlayer != null && !string.IsNullOrEmpty(configItem.externalPlayerConfig.onEnterPresetName))
                {
                    PlayExternalPlayer(configItem.externalPlayerConfig.externalPlayer, configItem.externalPlayerConfig.onEnterPresetName, false, from, to);
                }
                break;

            case ConfigItemType.UnityEvent:
                if (configItem.unityEventConfig != null)
                {
                    configItem.unityEventConfig.onEnterEvent?.Invoke();
                }
                break;
        }
    }

    /// <summary>
    /// 处理配置项的退出逻辑
    /// </summary>
    private void ProcessConfigItemExit(StateConfigItem configItem, UIState from, UIState to)
    {
        switch (configItem.itemType)
        {
            case ConfigItemType.Preset:
                if (configItem.presetConfig != null)
                {
                    if (configItem.presetConfig.reverseOnExit && !string.IsNullOrEmpty(configItem.presetConfig.onEnterPresetName))
                    {
                        PlayPreset(configItem.presetConfig.onEnterPresetName, true, from, to, configItem.presetConfig.onEnterBaselineMode);
                    }
                    else if (!string.IsNullOrEmpty(configItem.presetConfig.onExitPresetName))
                    {
                        PlayPreset(configItem.presetConfig.onExitPresetName, false, from, to, configItem.presetConfig.onExitBaselineMode);
                    }
                }
                break;

            case ConfigItemType.Track:
                if (configItem.trackConfig != null && configItem.trackConfig.onEnterTrack != null && !string.IsNullOrEmpty(configItem.trackConfig.onEnterTrackName))
                {
                    if (configItem.trackConfig.reverseTrackOnExit)
                    {
                        PlayTrack(configItem.trackConfig.onEnterTrack, configItem.trackConfig.onEnterTrackName, true, configItem.trackConfig.onExitTrackReverseMode, from, to);
                    }
                }
                break;

            case ConfigItemType.ExternalPlayer:
                if (configItem.externalPlayerConfig != null && configItem.externalPlayerConfig.externalPlayer != null)
                {
                    if (configItem.externalPlayerConfig.reverseOnExit && !string.IsNullOrEmpty(configItem.externalPlayerConfig.onEnterPresetName))
                    {
                        PlayExternalPlayer(configItem.externalPlayerConfig.externalPlayer, configItem.externalPlayerConfig.onEnterPresetName, true, from, to);
                    }
                    else if (!string.IsNullOrEmpty(configItem.externalPlayerConfig.onExitPresetName))
                    {
                        PlayExternalPlayer(configItem.externalPlayerConfig.externalPlayer, configItem.externalPlayerConfig.onExitPresetName, false, from, to);
                    }
                }
                break;

            case ConfigItemType.UnityEvent:
                if (configItem.unityEventConfig != null)
                {
                    configItem.unityEventConfig.onExitEvent?.Invoke();
                }
                break;
        }
    }

    private void PlayPreset(string presetName, bool reversed, UIState from, UIState to, UITweenPlayer.BaselineCaptureMode baselineMode = UITweenPlayer.BaselineCaptureMode.CurrentState)
    {
        if (string.IsNullOrEmpty(presetName)) return;
        string detail = $"UIState: {from} -> {to} | Preset: {presetName}";
        using (UITweenCallContext.BeginScope(this, "UITweenStateMachine", gameObject.name, detail))
        {
            if (reversed) _player.PlayReversedByName(presetName, baselineMode);
            else _player.PlayByName(presetName, baselineMode);
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

    private void PlayExternalPlayer(UITweenPlayer player, string presetName, bool reversed, UIState from, UIState to)
    {
        if (player == null || string.IsNullOrEmpty(presetName)) return;
        string detail = $"UIState: {from} -> {to} | ExternalPlayer: {presetName}";
        using (UITweenCallContext.BeginScope(this, "UITweenStateMachine", gameObject.name, detail))
        {
            if (reversed) player.PlayReversedByName(presetName);
            else player.PlayByName(presetName);
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
