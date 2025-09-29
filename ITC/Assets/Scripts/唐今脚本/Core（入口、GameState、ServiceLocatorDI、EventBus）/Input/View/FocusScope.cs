// FocusScope.cs (最终修正版 - 修复 InvalidOperationException)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class FocusScope : MonoBehaviour
{
    private List<IInteractableUI> _interactables = new List<IInteractableUI>(); 
    private IInteractableUI _currentFocus;
    private bool _isFocused;

    [SerializeField] private bool enableDebugLogging = true;

    private void RefreshInteractables()
    {
        _interactables = GetComponentsInChildren<IInteractableUI>(true).ToList(); // 改为true，查找包括未激活的
        if (enableDebugLogging) Debug.Log($"[FocusScope] {gameObject.name}: 刷新列表，找到 {_interactables.Count} 个可交互元素。", this);
    }

    public void SetFocused(bool focused)
    {
        _isFocused = focused;
        if (_isFocused)
        {
            RefreshInteractables();

            if (_interactables != null && _interactables.Any())
            {
                // 【最终修正】: 使用 FirstOrDefault 代替 First，避免在没有激活按钮时抛出异常。
                var firstActiveInteractable = _interactables.FirstOrDefault(i => (i as MonoBehaviour).isActiveAndEnabled);
                
                // 如果当前已有焦点则保持，否则尝试使用找到的第一个可用焦点
                SetFocus(_currentFocus ?? firstActiveInteractable);
            }
            else if (enableDebugLogging)
            {
                 Debug.LogWarning($"[FocusScope] {gameObject.name}: 被激活，但未找到任何可交互的子元素。", this);
            }
        }
        else
        {
            SetFocus(null);
        }
    }

    public void HandleNavigation(Vector2 direction)
    {
        if (!_isFocused || _interactables == null || _interactables.Count < 2) return;
        
        // 只在当前可用的按钮中导航
        var activeInteractables = _interactables.Where(i => (i as MonoBehaviour).isActiveAndEnabled).ToList();
        if (activeInteractables.Count < 1) return;

        int currentIndex = activeInteractables.IndexOf(_currentFocus);
        
        if (direction.y < -0.5f) // Down
        {
            currentIndex = (currentIndex + 1) % activeInteractables.Count;
        }
        else if (direction.y > 0.5f) // Up
        {
            currentIndex = (currentIndex < 1) ? (activeInteractables.Count - 1) : (currentIndex - 1);
        }

        SetFocus(activeInteractables[currentIndex]);
    }

    public void HandleSubmission()
    {
        if (!_isFocused) return;
        _currentFocus?.OnSubmit();
    }

    public void HandleCancel()
    {
        if (!_isFocused) return;
        Debug.Log($"Cancel intent received in scope: {gameObject.name}. A UI Manager should now PopScope().");
    }

    private void SetFocus(IInteractableUI newFocus)
    {
        if (_currentFocus == newFocus && _currentFocus != null) return;
        if (enableDebugLogging) Debug.Log($"[FocusScope] {gameObject.name}: 尝试设置新焦点为 {(newFocus as MonoBehaviour)?.name ?? "null"}", this);

        _currentFocus?.OnUnfocus();
        _currentFocus = newFocus;
        _currentFocus?.OnFocus();

        if (DialogueStateManager.Instance != null)
        {
            if (enableDebugLogging) Debug.Log($"[FocusScope] {gameObject.name}: 正在调用 OnFocusChanged 事件，广播新焦点: {(_currentFocus as MonoBehaviour)?.name ?? "null"}", this);
            DialogueStateManager.Instance.OnFocusChanged?.Invoke(_currentFocus);
        }
        else if (enableDebugLogging)
        {
            Debug.LogError($"[FocusScope] {gameObject.name}: StateManager.Instance 为空，无法广播 OnFocusChanged 事件！", this);
        }
    }
}