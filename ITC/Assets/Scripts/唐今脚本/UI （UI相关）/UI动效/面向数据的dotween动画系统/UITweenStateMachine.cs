using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 響應UI交互事件，並播放對應的低優先級動畫的狀態機。
/// 必須與 UITweenPlayer 組件掛載在同一個 GameObject 上。
/// </summary>
[RequireComponent(typeof(UITweenPlayer))]
public class UITweenStateMachine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    /// <summary>
    /// 定義了UI元素可能存在的狀態
    /// </summary>
    public enum UIState
    {
        Normal,   // 正常/默認狀態
        Hover,    // 鼠標懸停狀態
        Pressed,  // 鼠標按下狀態
        Selected, // 選中狀態（例如 Toggle on）
        Disabled  // 禁用狀態
    }

    /// <summary>
    /// 將一個UI狀態與進入/退出時播放的動畫預設名稱進行綁定
    /// </summary>
    [System.Serializable]
    public class StateAnimationBinding
    {
        [Tooltip("此綁定對應的UI狀態")]
        public UIState state;

        [Tooltip("當進入此狀態時要播放的動畫預設名稱")]
        public string onEnterPresetName;

        [Tooltip("如果勾選，退出狀態時將反向播放進入動畫，並忽略下方的“退出動畫名稱”")]
        public bool reverseOnExit = true;
        
        [Tooltip("（可選）當退出此狀態時要播放的動畫預設名稱（僅在 reverseOnExit 未勾選時生效）")]
        public string onExitPresetName;
    }

    [Serializable]
    public class PanelStateAnimationProfile
    {
        [Tooltip("此配置適用的面板狀態名稱。")]
        public string panelStateName;

        [Tooltip("當處於對應面板狀態時使用的狀態動畫綁定。")]
        public List<StateAnimationBinding> stateAnimations = new();

        [Tooltip("是否響應指針事件。")]
        public bool respondToPointerEvents = true;
    }

    [Tooltip("狀態與動畫的綁定列表（作為默認配置使用）。")]
    public List<StateAnimationBinding> stateAnimations = new List<StateAnimationBinding>();

    [Tooltip("當前按鈕關注的面板狀態配置資源。")]
    public UIPanelStateConfiguration panelStateConfiguration;

    [Tooltip("針對特定面板狀態的覆蓋配置。")]
    public List<PanelStateAnimationProfile> panelStateProfiles = new();

    private UITweenPlayer _player;
    private UIState _currentState = UIState.Normal;
    private bool _isInteractable = true;
    private bool _respondToPointerEvents = true;
    private PanelStateAnimationProfile _activePanelProfile;
    private string _currentPanelState;

    void Awake()
    {
        _player = GetComponent<UITweenPlayer>();
        SubscribeToPanelStateMachine();
        RefreshPanelState(UIPanelStateMachine.Instance != null ? UIPanelStateMachine.Instance.CurrentState : null);
    }

    void OnEnable()
    {
        SubscribeToPanelStateMachine();
    }

    void OnDisable()
    {
        UnsubscribeFromPanelStateMachine();
    }

    void OnDestroy()
    {
        UnsubscribeFromPanelStateMachine();
    }

    /// <summary>
    /// 核心狀態轉換邏輯
    /// </summary>
    /// <param name="newState">要轉換到的新狀態</param>
    private void TransitionTo(UIState newState)
    {
        if (!_isInteractable || !_respondToPointerEvents || _currentState == newState)
        {
            return;
        }

        // --- 修改後的退出邏輯 ---
        var oldBinding = FindBinding(_currentState);
        if (oldBinding != null)
        {
            if (oldBinding.reverseOnExit && !string.IsNullOrEmpty(oldBinding.onEnterPresetName))
            {
                PlayStateAnimation("Exit", oldBinding.onEnterPresetName, true, _currentState, newState);
            }
            else if (!string.IsNullOrEmpty(oldBinding.onExitPresetName))
            {
                PlayStateAnimation("Exit", oldBinding.onExitPresetName, false, _currentState, newState);
            }
        }

        var newBinding = FindBinding(newState);
        if (newBinding != null)
        {
            PlayStateAnimation("Enter", newBinding.onEnterPresetName, false, _currentState, newState);
        }

        _currentState = newState;
    }

    private StateAnimationBinding FindBinding(UIState state)
    {
        foreach (var binding in GetActiveBindings())
        {
            if (binding.state == state)
            {
                return binding;
            }
        }
        return null;
    }
    
    // --- Unity UI Event System Interfaces (無需修改) ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_respondToPointerEvents && _currentState != UIState.Pressed)
        {
            TransitionTo(UIState.Hover);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_respondToPointerEvents && _currentState != UIState.Pressed)
        {
            TransitionTo(UIState.Normal);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_respondToPointerEvents)
        {
            return;
        }

        TransitionTo(UIState.Pressed);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_respondToPointerEvents)
        {
            return;
        }

        if (_currentState == UIState.Pressed)
        {
            if (eventData.pointerCurrentRaycast.gameObject == gameObject)
            {
                TransitionTo(UIState.Hover);
            }
            else
            {
                TransitionTo(UIState.Normal);
            }
        }
    }

    // --- Public API for external control (無需修改) ---
    
    public void SetSelected(bool isSelected)
    {
        if (!_respondToPointerEvents)
        {
            return;
        }

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

    private void PlayStateAnimation(string phase, string presetName, bool reversed, UIState fromState, UIState toState)
    {
        if (string.IsNullOrEmpty(presetName)) return;
        string transitionLabel = fromState + "→" + toState;
        string detail = phase + " " + transitionLabel + " · " + presetName + (reversed ? " (Reversed)" : string.Empty);
        using (UITweenCallContext.BeginScope(this, "StateMachine", gameObject != null ? gameObject.name : name, detail))
        {
            if (reversed)
            {
                _player.PlayReversedByName(presetName);
            }
            else
            {
                _player.PlayByName(presetName);
            }
        }
    }

    List<StateAnimationBinding> GetActiveBindings()
    {
        if (_activePanelProfile != null && _activePanelProfile.stateAnimations != null && _activePanelProfile.stateAnimations.Count > 0)
        {
            return _activePanelProfile.stateAnimations;
        }

        return stateAnimations ??= new List<StateAnimationBinding>();
    }

    void SubscribeToPanelStateMachine()
    {
        var machine = UIPanelStateMachine.Instance;
        if (machine == null)
        {
            return;
        }

        machine.StateTransitionRequested -= HandlePanelStateRequested;
        machine.StateTransitionRequested += HandlePanelStateRequested;
    }

    void UnsubscribeFromPanelStateMachine()
    {
        var machine = UIPanelStateMachine.Instance;
        if (machine == null)
        {
            return;
        }

        machine.StateTransitionRequested -= HandlePanelStateRequested;
    }

    void HandlePanelStateRequested(string _, string targetState)
    {
        RefreshPanelState(targetState);
    }

    void RefreshPanelState(string targetState)
    {
        _currentPanelState = targetState;
        _activePanelProfile = null;
        _respondToPointerEvents = false;

        if (string.IsNullOrEmpty(targetState))
        {
            ResetToNormal();
            return;
        }

        if (panelStateConfiguration != null && !panelStateConfiguration.Contains(targetState))
        {
            ResetToNormal();
            return;
        }

        foreach (var profile in panelStateProfiles)
        {
            if (profile == null || string.IsNullOrEmpty(profile.panelStateName))
            {
                continue;
            }

            if (string.Equals(profile.panelStateName, targetState, StringComparison.Ordinal))
            {
                _activePanelProfile = profile;
                bool hasBindings = GetActiveBindings() != null && GetActiveBindings().Count > 0;
                _respondToPointerEvents = profile.respondToPointerEvents && hasBindings;
                if (!_respondToPointerEvents)
                {
                    ResetToNormal();
                }
                return;
            }
        }

        // 未配置的面板狀態不響應指針事件。
        ResetToNormal();
    }

    void ResetToNormal()
    {
        _currentState = UIState.Normal;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (panelStateProfiles == null || panelStateConfiguration == null)
        {
            return;
        }

        var validNames = new HashSet<string>(panelStateConfiguration.GetStateNames(), StringComparer.Ordinal);
        foreach (var profile in panelStateProfiles)
        {
            if (profile == null || string.IsNullOrEmpty(profile.panelStateName))
            {
                continue;
            }

            if (!validNames.Contains(profile.panelStateName))
            {
                profile.panelStateName = string.Empty;
            }
        }
    }
#endif
}
