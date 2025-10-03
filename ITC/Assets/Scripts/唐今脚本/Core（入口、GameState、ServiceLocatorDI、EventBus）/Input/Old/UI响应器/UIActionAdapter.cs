// UIActionAdapter.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 通用UI适配器，实现IInteractableUI接口。
/// 将系统的OnFocus/OnUnfocus/OnSubmit事件桥接到可在Inspector中配置的UnityEvent。
/// </summary>
public class UIActionAdapter : MonoBehaviour, IInteractableUI
{
    [Tooltip("当此元素获得焦点时触发的事件")]
    public UnityEvent OnFocused;

    [Tooltip("当此元素失去焦点时触发的事件")]
    public UnityEvent OnUnfocused;

    [Tooltip("当此元素被提交时触发的事件")]
    public UnityEvent OnSubmitted;

    public void OnFocus() => OnFocused?.Invoke();
    public void OnUnfocus() => OnUnfocused?.Invoke();
    public void OnSubmit() => OnSubmitted?.Invoke();
}