// FocusScope.cs (完整修改版)
using System.Collections;
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
        _interactables = GetComponentsInChildren<IInteractableUI>(true).ToList();
        if (enableDebugLogging) Debug.Log($"[FocusScope] {gameObject.name}: 刷新列表，找到 {_interactables.Count} 個可交互元素。", this);
    }

    public void SetFocused(bool focused)
    {
        _isFocused = focused;
        if (_isFocused)
        {
            RefreshInteractables();

            if (_interactables != null && _interactables.Any())
            {
                var firstActiveInteractable = _interactables.FirstOrDefault(i => (i as MonoBehaviour).isActiveAndEnabled);

                if (firstActiveInteractable != null)
                {
                    SetFocus(_currentFocus ?? firstActiveInteractable);
                }
                else
                {
                    if (enableDebugLogging) Debug.LogWarning($"[FocusScope] {gameObject.name}: 被激活，但暫未找到任何**已激活**的子元素。將在下一幀重試...", this);
                    // 【核心修正】將協程委託給永遠激活的 StateManager 執行
                    DialogueStateManager.Instance.RunCoroutine(RetrySetFocusAfterFrame());
                }
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

    private IEnumerator RetrySetFocusAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        RefreshInteractables();
        var firstActiveInteractable = _interactables.FirstOrDefault(i => (i as MonoBehaviour).isActiveAndEnabled);

        if (firstActiveInteractable != null)
        {
            if (enableDebugLogging) Debug.Log($"[FocusScope] {gameObject.name}: 重試成功，找到可用焦點: {(firstActiveInteractable as MonoBehaviour).name}", this);
            SetFocus(firstActiveInteractable);
        }
        else
        {
             if (enableDebugLogging) Debug.LogWarning($"[FocusScope] {gameObject.name}: 重試後，依然未找到任何已激活的子元素。", this);
             SetFocus(null);
        }
    }

    public void HandleNavigation(Vector2 direction)
    {
        if (!_isFocused || _interactables == null || _interactables.Count < 2) return;

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
        // 如果焦点没有实际变化，则不执行任何操作
        if (_currentFocus == newFocus) return;
        
        if (enableDebugLogging) Debug.Log($"[FocusScope] {gameObject.name}: 嘗試設置新焦點为 {(newFocus as MonoBehaviour)?.name ?? "null"}", this);

        _currentFocus?.OnUnfocus();
        _currentFocus = newFocus;
        _currentFocus?.OnFocus();

        // 【核心修改】: 将直接调用事件改为调用StateManager的统一接口
        // 这样做的好处是逻辑更清晰，所有焦点事件都由StateManager广播
        if (DialogueStateManager.Instance != null)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[FocusScope] {gameObject.name}: 正在请求 StateManager 广播新焦点: {(_currentFocus as MonoBehaviour)?.name ?? "null"}", this);
            }
            // 通知StateManager，焦点已经改变
            DialogueStateManager.Instance.NotifyFocusChanged(_currentFocus);
        }
    }
}