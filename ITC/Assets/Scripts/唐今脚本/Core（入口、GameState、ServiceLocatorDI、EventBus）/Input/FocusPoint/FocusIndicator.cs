// FocusIndicator.cs (增強日誌監控版)
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class FocusIndicator : MonoBehaviour
{
    [Header("行為設置")]
    [Tooltip("指示器相對於目標按鈕的位置偏移")]
    public Vector3 positionOffset = new Vector3(0, 0, 0);

    [Tooltip("指示器是否應該匹配目標按鈕的大小")]
    public bool matchTargetSize = true;

    [Header("智能光標控制")]
    [Tooltip("啟用此功能後，指示器將作為鍵鼠/手柄的光標替代")]
    public bool enableCursorControl = true;

    [Tooltip("需要鏈接到Input Action Asset中的Pointer > Delta [Vector2]動作")]
    public InputActionReference pointerDeltaAction;
    
    [Header("調試")]
    [Tooltip("啟用後，將在控制台打印詳細的日誌信息")]
    [SerializeField] private bool enableDebugLogging = true;
    
    private Image _spriteRenderer;

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        _spriteRenderer = this.GetComponent<Image>();
    }

    private void Start()
    {
        UpdateIndicatorState(null);
    }

    private void OnEnable()
    {
        SubscribeToEvents();

        if (enableCursorControl && pointerDeltaAction != null && pointerDeltaAction.action != null)
        {
            pointerDeltaAction.action.Enable();
            pointerDeltaAction.action.performed += OnPointerMove;
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (DialogueStateManager.Instance != null)
        {
            Debug.Log($"[FocusIndicator] 正在嘗試訂閱 StateManager (ID: {DialogueStateManager.Instance.GetInstanceID()}) 的 OnFocusChanged 事件。", this);
            DialogueStateManager.Instance.OnFocusChanged += HandleFocusChanged;
        }
        else
        {
            Debug.Log($"[FocusIndicator] 訂閱 StateManager 失败", this);
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
            pointerDeltaAction.action.Disable();
        }
    }

    private void OnPointerMove(InputAction.CallbackContext context)
    {
        if (!enableCursorControl) return;

        if (context.control.device is Mouse && context.ReadValue<Vector2>().sqrMagnitude > 0.1f)
        {
            if (DialogueStateManager.Instance != null)
            {
                 DialogueStateManager.Instance.NotifyDeviceUsed(context.control.device);
                 UpdateIndicatorState(null);
            }
        }
    }

    private void HandleFocusChanged(IInteractableUI newFocus)
    {
        Debug.Log($"[HandleFocusChanged!!!", this);
        UpdateIndicatorState(newFocus);
    }

    // 【核心修改】增加超詳細的日誌
    private void UpdateIndicatorState(IInteractableUI newFocus)
    {
        if (DialogueStateManager.Instance == null) return;

        var lastDevice = DialogueStateManager.Instance.LastUsedDevice;
        bool isMouseMode = (lastDevice == InputDeviceType.Mouse);
        string newFocusName = (newFocus as MonoBehaviour)?.name ?? "null";

        if (enableDebugLogging)
        {
            Debug.Log($"[FocusIndicator-Check] --- 狀態檢查 --- \n" +
                      $"全局設備: {lastDevice}, IsMouseMode: {isMouseMode}\n" +
                      $"接收到的新焦點: {newFocusName}");
        }

        if (enableCursorControl)
        {
            Cursor.visible = isMouseMode;
        }

        if (isMouseMode || newFocus == null)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[FocusIndicator-Result] 決策: 隱藏。 原因: isMouseMode={isMouseMode}, newFocus is null?={newFocus == null}", this);
            }
            _spriteRenderer.enabled = false;
            return;
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[FocusIndicator-Result] 決策: 顯示。 將定位到 '{newFocusName}'。", this);
        }

        _spriteRenderer.enabled = true;

        var targetObject = (newFocus as MonoBehaviour)?.gameObject;
        if(targetObject == null) {
            _spriteRenderer.enabled = false;
            return;
        }

        var targetRect = targetObject.GetComponent<RectTransform>();

        // ==================【核心修改：只设置位置，不改变其他任何东西】==================
        // 1. 先显示指示器
        _spriteRenderer.enabled = true;

        // 2. 获取目标UI元素的Transform
        var targetTransform = targetObject.transform;

        Vector3 targetCenter = targetRect != null
            ? targetRect.TransformPoint(targetRect.rect.center)
            : targetTransform.position;

        // 3. 直接将指示器的世界坐标设置为目标的世界坐标
        //    因为两者的Pivot都设置在中心(0.5, 0.5)，这会实现中心对齐
        this.transform.position = targetCenter + positionOffset;

        // ==================【修改结束，删掉所有关于大小和父对象的代码】==================
    }
}