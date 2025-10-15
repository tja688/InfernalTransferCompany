using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

    [Tooltip("狀態與動畫的綁定列表")]
    public List<StateAnimationBinding> stateAnimations = new List<StateAnimationBinding>();

    private UITweenPlayer _player;
    private UIState _currentState = UIState.Normal;
    private bool _isInteractable = true;

    void Awake()
    {
        _player = GetComponent<UITweenPlayer>();
    }

    /// <summary>
    /// 核心狀態轉換邏輯
    /// </summary>
    /// <param name="newState">要轉換到的新狀態</param>
    private void TransitionTo(UIState newState)
    {
        if (!_isInteractable || _currentState == newState)
        {
            return;
        }

        // --- 修改後的退出邏輯 ---
        var oldBinding = FindBinding(_currentState);
        if (oldBinding != null)
        {
            // 檢查是否應倒播進入動畫
            if (oldBinding.reverseOnExit && !string.IsNullOrEmpty(oldBinding.onEnterPresetName))
            {
                _player.PlayReversedByName(oldBinding.onEnterPresetName);
            }
            // 否則，播放指定的退出動畫
            else if (!string.IsNullOrEmpty(oldBinding.onExitPresetName))
            {
                _player.PlayByName(oldBinding.onExitPresetName);
            }
        }
        
        // 查找新狀態的綁定，播放進入動畫（此部分邏輯不變）
        var newBinding = FindBinding(newState);
        if (newBinding != null && !string.IsNullOrEmpty(newBinding.onEnterPresetName))
        {
            _player.PlayByName(newBinding.onEnterPresetName);
        }

        _currentState = newState;
    }

    private StateAnimationBinding FindBinding(UIState state)
    {
        foreach (var binding in stateAnimations)
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
        if (_currentState != UIState.Pressed)
        {
            TransitionTo(UIState.Hover);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_currentState != UIState.Pressed)
        {
            TransitionTo(UIState.Normal);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TransitionTo(UIState.Pressed);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
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
}