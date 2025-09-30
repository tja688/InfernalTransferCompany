// DialogueStateManager.cs (完整修改版)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using PixelCrushers.DialogueSystem;

// 定義一個全局可用的設備類型枚舉
public enum InputDeviceType { Mouse, Keyboard, Gamepad }

public class DialogueStateManager : MonoBehaviour
{
    public static DialogueStateManager Instance { get; private set; }

    private readonly Stack<FocusScope> _focusScopeStack = new Stack<FocusScope>();
    public System.Action<IInteractableUI> OnFocusChanged;
    public IInteractableUI CurrentFocus { get; private set; }
    private StandardDialogueUI _dialogueUI;

    // 新增：全局設備狀態屬性，默認為滑鼠
    public InputDeviceType LastUsedDevice { get; private set; } = InputDeviceType.Mouse;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 【新增日誌】告訴我們有一個重複的實例被銷毀了
            Debug.LogWarning($"[StateManager] 發現重複的實例 (ID: {GetInstanceID()})，將其銷毀。當前激活的單例ID為: {Instance.GetInstanceID()}", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // 【新增日誌】告訴我們哪個實例被設定為單例
        Debug.Log($"[StateManager] 實例 (ID: {GetInstanceID()}) 已被設定為全局單例。", this);
    }

    private void Start()
    {
        _dialogueUI = FindObjectOfType<StandardDialogueUI>();
    }

    // 新增：公共方法，供 DSNewInputBridge 調用以更新設備狀態
    public void NotifyDeviceUsed(InputDevice device)
    {
        if (device is Mouse)
        {
            if (LastUsedDevice != InputDeviceType.Mouse)
            {
                LastUsedDevice = InputDeviceType.Mouse;
                Debug.Log("[StateManager] Switched to Mouse input.");
            }
        }
        else if (device is Keyboard)
        {
             if (LastUsedDevice != InputDeviceType.Keyboard)
            {
                LastUsedDevice = InputDeviceType.Keyboard;
                Debug.Log("[StateManager] Switched to Keyboard input.");
            }
        }
        else if (device is Gamepad)
        {
             if (LastUsedDevice != InputDeviceType.Gamepad)
            {
                LastUsedDevice = InputDeviceType.Gamepad;
                Debug.Log("[StateManager] Switched to Gamepad input.");
            }
        }
    }

    // 【新增】: 创建一个统一的公共方法来广播焦点变化
    // 这是所有其他脚本应该调用的唯一接口
    public void NotifyFocusChanged(IInteractableUI newFocus)
    {
        // 使用 ?.Invoke() 是一个安全的操作，即使没有任何对象订阅事件，也不会报错
        CurrentFocus = newFocus;
        OnFocusChanged?.Invoke(newFocus);
        
        // 【可选的调试日志】确认事件已从此地触发
        string newFocusName = (newFocus as MonoBehaviour)?.name ?? "null";
        Debug.Log($"[StateManager] OnFocusChanged 事件已觸發。新焦點: {newFocusName}");
    }

    public void OnSubmitIntent()
    {
        // 優先處理對話系統自身的 "Continue" 提示
        if (DialogueManager.isConversationActive &&
            DialogueManager.currentConversationState != null &&
            DialogueManager.currentConversationState.subtitle.sequence == "Continue()")
        {
            var currentUI = DialogueManager.Instance.DialogueUI as StandardDialogueUI;
            if(currentUI != null)
            {
                currentUI.OnContinue();
                return;
            }
        }

        // 如果不是Continue狀態，則將提交意圖傳遞給我們的焦點系統
        if (_focusScopeStack.Any())
        {
            _focusScopeStack.Peek()?.HandleSubmission();
        }
    }

    public void OnCancelIntent()
    {
        if (_focusScopeStack.Any())
        {
             _focusScopeStack.Peek()?.HandleCancel();
        }
    }

    public void OnNavigateIntent(Vector2 direction)
    {
        if (_focusScopeStack.Any())
        {
            _focusScopeStack.Peek()?.HandleNavigation(direction);
        }
    }

    public void OnToggleBacklogIntent()
    {
        Debug.Log("Backlog Intent Received");
    }

    public void OnQuickSaveIntent()
    {
        Debug.Log("Quick Save Intent Received");
    }

    public void OnQuickLoadIntent()
    {
        Debug.Log("Quick Load Intent Received");
    }

    public void PushScope(FocusScope scope)
    {
        if (_focusScopeStack.Any())
        {
            _focusScopeStack.Peek().SetFocused(false);
        }
        _focusScopeStack.Push(scope);
        
        // 【核心修改】: 现在由 StateManager 负责调用 SetFocused
        // 这确保了流程的正确性
        scope.SetFocused(true);
    }

    public void PopScope()
    {
        if (_focusScopeStack.Count == 0) return;

        // 先弹出旧的scope，并取消其焦点状态
        _focusScopeStack.Pop().SetFocused(false);

        if (_focusScopeStack.Any())
        {
            var newActiveScope = _focusScopeStack.Peek();
            // 【核心修改】: 激活新的顶层scope
            newActiveScope.SetFocused(true);
        }
        else
        {
            // 【核心修改】: 如果所有Scope都弹出了，广播一个null焦点
            NotifyFocusChanged(null);
        }
    }
    
    public void RunCoroutine(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }
}