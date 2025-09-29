// FocusScope.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 管理一组IInteractableUI元素。附加在UI面板的根对象上。
/// 负责处理来自StateManager的导航和提交请求，并管理其内部的焦点切换。
/// </summary>
public class FocusScope : MonoBehaviour
{
    private List<IInteractableUI> _interactables;
    private IInteractableUI _currentFocus;
    private bool _isFocused; // 此Scope是否是栈顶的活动Scope

    private void Awake()
    {
        // 自动查找所有子物体中的可交互元素
        _interactables = GetComponentsInChildren<IInteractableUI>(false).ToList();
    }

    /// <summary>
    /// 当此Scope被Push到栈顶或从栈顶Pop时，由StateManager调用。
    /// </summary>
    public void SetFocused(bool focused)
    {
        _isFocused = focused;
        if (_isFocused && _interactables.Any())
        {
            // 当Scope被激活时，默认聚焦到第一个元素或之前记住的焦点
            SetFocus(_currentFocus ?? _interactables.First(i => (i as MonoBehaviour).isActiveAndEnabled));
        }
        else if (!focused)
        {
            // 当Scope被钝化时，清除焦点
            SetFocus(null);
        }
    }

    public void HandleNavigation(Vector2 direction)
    {
        if (!_isFocused || _interactables.Count < 2) return;
        
        // 注意：这是一个简化的导航逻辑，仅适用于线性列表（垂直或水平）。
        // 更复杂的网格布局需要更高级的算法（如基于UI元素的屏幕位置计算）。
        int currentIndex = _interactables.IndexOf(_currentFocus);
        if (direction.y < -0.5f) // Down
        {
            currentIndex = (currentIndex + 1) % _interactables.Count;
        }
        else if (direction.y > 0.5f) // Up
        {
            currentIndex = (currentIndex - 1 + _interactables.Count) % _interactables.Count;
        }
        // 可在此添加左右导航逻辑...

        SetFocus(_interactables[currentIndex]);
    }

    public void HandleSubmission()
    {
        if (!_isFocused) return;
        _currentFocus?.OnSubmit();
    }

    public void HandleCancel()
    {
        if (!_isFocused) return;
        // 默认的取消行为可以是Pop自己，例如关闭菜单
        // 具体的关闭逻辑应该由管理此Scope的UIView来做
        Debug.Log($"Cancel intent received in scope: {gameObject.name}. A UI Manager should now PopScope().");
    }

    private void SetFocus(IInteractableUI newFocus)
    {
        if (_currentFocus == newFocus) return;

        _currentFocus?.OnUnfocus();
        _currentFocus = newFocus;
        _currentFocus?.OnFocus();

        // 通知StateManager，焦点已变更 (观察者模式)
        DialogueStateManager.Instance?.OnFocusChanged?.Invoke(_currentFocus);
    }
}