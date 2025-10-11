using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(UITweenPlayer))]
public class UIStateMachine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    // 在 Inspector 中配置所有状态的动画
    public List<StateAnimationBinding> stateAnimations = new List<StateAnimationBinding>();

    // 初始状态
    public UIState startingState = UIState.Normal;
    
    private UIState _currentState;
    private UITweenPlayer _tweenPlayer;
    private Dictionary<UIState, StateAnimationBinding> _bindingMap;

    void Awake()
    {
        _tweenPlayer = GetComponent<UITweenPlayer>();
        
        // 将 List 转换为字典，方便快速查找
        _bindingMap = stateAnimations.ToDictionary(b => b.state, b => b);
    }

    void Start()
    {
        // 初始化到起始状态，但不播放动画
        _currentState = startingState;
    }

    // ---- 核心：状态转换方法 ----
    private void TransitionTo(UIState newState)
    {
        if (_currentState == newState) return;

        // 1. 播放“退出旧状态”的动画
        if (_bindingMap.TryGetValue(_currentState, out var oldBinding))
        {
            if (oldBinding.reverseOnExit && oldBinding.onEnterPreset != null)
            {
                _tweenPlayer.PlayReversed(oldBinding.onEnterPreset);
            }
            else if (oldBinding.onExitPreset != null)
            {
                _tweenPlayer.Play(oldBinding.onExitPreset);
            }
        }
        
        // 2. 播放“进入新状态”的动画
        if (_bindingMap.TryGetValue(newState, out var newBinding))
        {
            if (newBinding.onEnterPreset != null)
            {
                _tweenPlayer.Play(newBinding.onEnterPreset);
            }
        }

        // 3. 更新当前状态
        _currentState = newState;
    }

    // ---- UI 事件监听与状态决策 ----
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_currentState == UIState.Pressed || _currentState == UIState.Disabled) return;
        TransitionTo(UIState.Hover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_currentState == UIState.Pressed || _currentState == UIState.Disabled) return;
        TransitionTo(UIState.Normal);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_currentState == UIState.Disabled) return;
        TransitionTo(UIState.Pressed);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 当鼠标抬起时，要判断指针是否还在对象上
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
    
    // ---- 外部控制接口 (例如：用于Toggle) ----
    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            // 这里可以处理更复杂的状态组合，例如进入 "Selected" 状态
            // 暂时简化处理
            if (_currentState != UIState.Selected) TransitionTo(UIState.Selected);
        }
        else
        {
            if (_currentState == UIState.Selected) TransitionTo(UIState.Normal);
        }
    }
}