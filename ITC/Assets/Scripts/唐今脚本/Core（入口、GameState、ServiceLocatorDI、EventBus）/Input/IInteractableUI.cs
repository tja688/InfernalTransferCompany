// IInteractableUI.cs
using UnityEngine;

/// <summary>
/// 可交互UI元素的接口。
/// 任何希望被焦点系统管理的UI组件都应依附于一个实现了此接口的GameObject。
/// </summary>
public interface IInteractableUI
{
    GameObject gameObject { get; }

    /// <summary>
    /// 当此元素获得焦点时调用。
    /// </summary>
    void OnFocus();

    /// <summary>
    /// 当此元素失去焦点时调用。
    /// </summary>
    void OnUnfocus();

    /// <summary>
    /// 当系统决定提交此元素时调用。
    /// </summary>
    void OnSubmit();
}