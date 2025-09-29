// FocusIndicator.cs (最终升级版)
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 智能焦点指示器，作为键鼠/手柄的光标替代。
/// 1. 订阅OnFocusChanged事件来追踪焦点。
/// 2. 监听鼠标移动，检测到后自动隐藏自身并显示系统光标。
/// 3. 焦点因键鼠/手柄改变时，自动显示自身并隐藏系统光标。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FocusIndicator : MonoBehaviour
{
    [Header("行为设置")]
    [Tooltip("指示器相对于目标按钮的位置偏移")]
    public Vector3 positionOffset = new Vector3(0, 0, 0);

    [Tooltip("指示器是否应该匹配目标按钮的大小")]
    public bool matchTargetSize = true;

    [Header("智能光标控制")]
    [Tooltip("启用此功能后，指示器将作为键鼠/手柄的光标替代")]
    public bool enableCursorControl = true;
    
    [Tooltip("需要链接到Input Action Asset中的Pointer > Delta [Vector2]动作")]
    public InputActionReference pointerDeltaAction;

    [Header("调试")]
    [Tooltip("启用后，将在控制台打印详细的日志信息")]
    [SerializeField] private bool enableDebugLogging = true;

    private RectTransform _rectTransform;
    private bool _isMouseInputActive = true; // 默认以鼠标模式启动

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // 尝试在Start中再次订阅，以更好地处理脚本执行顺序问题
        SubscribeToEvents();
        // 根据初始模式设置光标和指示器状态
        UpdateCursorAndIndicatorState();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    
    private void SubscribeToEvents()
    {
        // 确保只订阅一次
        if (DialogueStateManager.Instance != null)
        {
            DialogueStateManager.Instance.OnFocusChanged -= HandleFocusChanged; // 先移除，防止重复订阅
            DialogueStateManager.Instance.OnFocusChanged += HandleFocusChanged;
            if (enableDebugLogging) Debug.Log("[FocusIndicator] 成功订阅 OnFocusChanged 事件。", this);
        }
        else if (enableDebugLogging)
        {
             Debug.LogError("[FocusIndicator] StateManager.Instance 为空，订阅 OnFocusChanged 事件失败！", this);
        }

        if (pointerDeltaAction != null && pointerDeltaAction.action != null)
        {
            pointerDeltaAction.action.performed -= OnPointerMove;
            pointerDeltaAction.action.performed += OnPointerMove;
            pointerDeltaAction.action.Enable();
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (DialogueStateManager.Instance != null)
        {
            DialogueStateManager.Instance.OnFocusChanged -= HandleFocusChanged;
        }
        if (pointerDeltaAction != null && pointerDeltaAction.action != null)
        {
            pointerDeltaAction.action.performed -= OnPointerMove;
        }
    }

    // 监听鼠标/指针移动
    private void OnPointerMove(InputAction.CallbackContext context)
    {
        if (!enableCursorControl) return;

        // 只有在鼠标确实移动了（而不是其他指针设备）且当前不是鼠标模式时，才切换
        if (context.control.device is Mouse && context.ReadValue<Vector2>().sqrMagnitude > 0.1f)
        {
            if (!_isMouseInputActive)
            {
                 if (enableDebugLogging) Debug.Log("[FocusIndicator] 检测到鼠标移动，切换到鼠标模式。", this);
                _isMouseInputActive = true;
                UpdateCursorAndIndicatorState();
            }
        }
    }

    // 处理焦点变更
    private void HandleFocusChanged(IInteractableUI newFocus)
    {
        if (enableDebugLogging) Debug.Log($"[FocusIndicator] 收到 OnFocusChanged 事件，新焦点: {(newFocus as MonoBehaviour)?.name ?? "null"}", this);

        if (enableCursorControl)
        {
            // 只要焦点通过非鼠标方式改变，就切换到键鼠/手柄模式
            if (_isMouseInputActive)
            {
                _isMouseInputActive = false;
                 if (enableDebugLogging) Debug.Log("[FocusIndicator] 焦点改变，切换到键鼠/手柄模式。", this);
            }
        }
        
        UpdateCursorAndIndicatorState(newFocus);
    }
    
    // 统一更新光标和指示器状态
    private void UpdateCursorAndIndicatorState(IInteractableUI newFocus = null)
    {
        if (enableCursorControl)
        {
            Cursor.visible = _isMouseInputActive;
        }

        if (_isMouseInputActive || newFocus == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // --- 以下为显示和定位指示器的逻辑 ---
        gameObject.SetActive(true);

        var targetObject = (newFocus as MonoBehaviour)?.gameObject;
        if(targetObject == null) {
            gameObject.SetActive(false);
            return;
        }
        
        var targetTransform = targetObject.transform;
        
        transform.SetParent(targetTransform, true); // 使用 worldPositionStays = true
        transform.SetAsLastSibling(); // 确保在父级中渲染在最上层

        if (matchTargetSize)
        {
            var targetRect = targetObject.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                _rectTransform.anchorMin = new Vector2(0, 0);
                _rectTransform.anchorMax = new Vector2(1, 1);
                _rectTransform.offsetMin = Vector2.zero;
                _rectTransform.offsetMax = Vector2.zero;
            }
        }
        _rectTransform.localPosition = positionOffset; // 应用本地偏移
    }
}