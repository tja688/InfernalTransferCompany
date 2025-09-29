// DialogueStateManager.cs (Corrected Version 3)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// MVC中的Model。管理全局游戏状态与UI焦点。
/// 这是一个单例，负责维护一个FocusScope堆栈，并将输入意图分派给栈顶的Scope。
/// </summary>
public class DialogueStateManager : MonoBehaviour
{
    public static DialogueStateManager Instance { get; private set; }

    private readonly Stack<FocusScope> _focusScopeStack = new Stack<FocusScope>();
    public System.Action<IInteractableUI> OnFocusChanged;
    private StandardDialogueUI _dialogueUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _dialogueUI = FindObjectOfType<StandardDialogueUI>();
    }

    public void OnSubmitIntent()
    {
        if (DialogueManager.isConversationActive && DialogueManager.currentConversationState != null && DialogueManager.currentConversationState.pcResponses.Length > 0)
        {
            // 如果存在玩家选项，再来处理UI提交的逻辑
            var currentSelected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (currentSelected != null && currentSelected.GetComponent<StandardUIResponseButton>() != null)
            {
                var interactable = currentSelected.GetComponent<IInteractableUI>();
                interactable?.OnSubmit();
                return;
            }
        }
        
        if (DialogueManager.isConversationActive && DialogueManager.currentConversationState.subtitle.sequence == "Continue()")
        {
            if (_dialogueUI != null)
            {
                _dialogueUI.OnContinue();
            }
            else 
            {
                var currentUI = DialogueManager.Instance.DialogueUI as StandardDialogueUI;
                if(currentUI != null) currentUI.OnContinue();
            }
            return;
        }

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
        scope.SetFocused(true);
    }

    public void PopScope()
    {
        if (_focusScopeStack.Count == 0) return;
        
        _focusScopeStack.Pop().SetFocused(false);
        
        if (_focusScopeStack.Any())
        {
            _focusScopeStack.Peek().SetFocused(true);
        }
    }
}