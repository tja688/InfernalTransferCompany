// DSNewInputBridge.cs (最終版 - 自動追蹤設備)
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// MVC中的Controller。監聽Input System的輸入動作，並將它們翻譯成對DialogueStateManager的調用。
/// 集成了編輯器自動綁定功能，並能自動追蹤最後使用的輸入設備類型。
/// </summary>
public class DSNewInputBridge : MonoBehaviour
{
    // ---------- 編輯器自動綁定功能所需字段 ----------
    [Header("Input Actions Asset (Auto Bind)")]
    [Tooltip("拖入你的Input Actions (InputActionAsset)。腳本會嘗試按命名自動匹配。")]
    public InputActionAsset actionsAsset;

    [Tooltip("在編輯器修改時自動嘗試匹配一次。也可手動點擊Inspector的按鈕。")]
    public bool autoBindOnValidate = true;

    // ---------- 核心輸入動作引用 ----------
    [Header("Input Action References")]
    public InputActionReference submit;
    public InputActionReference cancel;
    public InputActionReference navigate;
    public InputActionReference backlog;
    public InputActionReference quickSave;
    public InputActionReference quickLoad;
    public InputActionReference openMenu;
    // ... 可根據你的InputActionAsset添加更多引用

    [Header("PlayerInput Control Scheme Sync (Optional)")]
    [Tooltip("如需在設備切換時驅動 PlayerInput 的控制方案變更，可在此指定。")]
    public PlayerInput playerInput;
    [Tooltip("是否在檢測到設備變化時自動切換 PlayerInput 的控制方案。")]
    public bool autoSwitchControlScheme = true;
    [Tooltip("鍵盤輸入時應切換到的控制方案名稱。通常為包含鍵盤+滑鼠的方案。")]
    public string keyboardControlScheme = "Keyboard&Mouse";
    [Tooltip("滑鼠輸入時應切換到的控制方案名稱。預設與鍵盤相同，以保證鍵鼠同時可用。")]
    public string mouseControlScheme = "Keyboard&Mouse";
    [Tooltip("手柄輸入時應切換到的控制方案名稱。")]
    public string gamepadControlScheme = "Gamepad";

    #region 生命周期与输入订阅

    private void OnEnable()
    {
        // 使用新的、能夠追蹤設備的訂閱方法
        SubscribeAndTrackDevice(submit, ctx => DialogueStateManager.Instance.OnSubmitIntent());
        SubscribeAndTrackDevice(cancel, ctx => DialogueStateManager.Instance.OnCancelIntent());
        SubscribeAndTrackDevice(navigate, ctx => DialogueStateManager.Instance.OnNavigateIntent(ctx.ReadValue<Vector2>()));
        SubscribeAndTrackDevice(backlog, ctx => DialogueStateManager.Instance.OnToggleBacklogIntent());
        SubscribeAndTrackDevice(quickSave, ctx => DialogueStateManager.Instance.OnQuickSaveIntent());
        SubscribeAndTrackDevice(quickLoad, ctx => DialogueStateManager.Instance.OnQuickLoadIntent());
    }

    private void OnDisable()
    {
        Unsubscribe(submit);
        Unsubscribe(cancel);
        Unsubscribe(navigate);
        Unsubscribe(backlog);
        Unsubscribe(quickSave);
        Unsubscribe(quickLoad);
    }

    // 【核心修改】創建一個新的訂閱方法，它會自動處理設備追蹤
    private void SubscribeAndTrackDevice(InputActionReference actionRef, System.Action<InputAction.CallbackContext> handler)
    {
        if (actionRef == null || actionRef.action == null) return;

        // 訂閱一個新的 lambda 表達式
        actionRef.action.performed += (ctx) =>
        {
            // 在執行原始邏輯之前，先通知 StateManager 設備變更
            if (ctx.control != null)
            {
                NotifyDeviceUsage(ctx.control.device);
            }
            // 然後再執行原始的回調
            handler(ctx);
        };

        actionRef.action.Enable();
    }

    private void NotifyDeviceUsage(InputDevice device)
    {
        if (device == null) return;

        if (DialogueStateManager.Instance != null)
        {
            DialogueStateManager.Instance.NotifyDeviceUsed(device);
        }

        if (!autoSwitchControlScheme || playerInput == null) return;

        if (device is Gamepad)
        {
            SwitchControlScheme(gamepadControlScheme, new InputDevice[] { device });
        }
        else if (device is Keyboard keyboard)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                SwitchControlScheme(keyboardControlScheme, new InputDevice[] { keyboard, mouse });
            }
            else
            {
                SwitchControlScheme(keyboardControlScheme, new InputDevice[] { keyboard });
            }
        }
        else if (device is Mouse mouse)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                SwitchControlScheme(mouseControlScheme, new InputDevice[] { keyboard, mouse });
            }
            else
            {
                SwitchControlScheme(mouseControlScheme, new InputDevice[] { mouse });
            }
        }
    }

    private void SwitchControlScheme(string schemeName, InputDevice[] devices)
    {
        if (playerInput == null || string.IsNullOrEmpty(schemeName) || devices == null || devices.Length == 0)
        {
            return;
        }

        if (playerInput.currentControlScheme == schemeName)
        {
            return;
        }

        playerInput.SwitchCurrentControlScheme(schemeName, devices);
    }

    private void Unsubscribe(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return;
        // 注意：因為我們在訂閱時使用了匿名(lambda)函數，舊的事件處理器無法被精確移除。
        // 所以直接禁用Action是在OnDisable中推薦的做法，效果一致。
        actionRef.action.Disable();
    }

    #endregion

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (autoBindOnValidate)
            TryAutoBindAll(false);
    }
    #endif

    #region 自动绑定核心逻辑

    private static readonly (string field, string actionName)[] s_FieldToAction =
    {
        (nameof(submit),     "Submit"),
        (nameof(cancel),     "Cancel"),
        (nameof(navigate),   "Navigate"),
        (nameof(backlog),    "Backlog"),
        (nameof(quickSave),  "QuickSave"),
        (nameof(quickLoad),  "QuickLoad"),
        (nameof(openMenu),   "OpenMenu"),
    };

    public void TryAutoBindAll(bool logToConsole = true)
    {
        if (actionsAsset == null)
        {
            if (logToConsole) Debug.LogWarning("[DSInputBridge] Actions Asset未分配，無法自動綁定。", this);
            return;
        }

        foreach (var pair in s_FieldToAction)
        {
            var fieldInfo = GetType().GetField(pair.field);
            if (fieldInfo == null) continue;

            var current = fieldInfo.GetValue(this) as InputActionReference;
            if (current != null && current.asset != null) continue;

            var matches = FindActionsByName(pair.actionName);
            if (matches.Count == 1)
            {
                var created = InputActionReference.Create(matches[0]);
                fieldInfo.SetValue(this, created);
#if UNITY_EDITOR
                if (logToConsole)
                    Debug.Log($"[DSInputBridge] Auto-bound: {pair.field} -> {GetDisplayPath(matches[0])}", this);
                EditorUtility.SetDirty(this);
#endif
            }
            else if (logToConsole)
            {
                if (matches.Count == 0)
                    Debug.LogWarning($"[DSInputBridge] 在 {actionsAsset.name} 中未找到名为 \"{pair.actionName}\" 的 Action。", this);
                else
                    Debug.LogWarning($"[DSInputBridge] 在 {actionsAsset.name} 中找到多个名为 \"{pair.actionName}\" 的 Action，请手动指定。", this);
            }
        }
    }

    private List<InputAction> FindActionsByName(string actionName)
    {
        var list = new List<InputAction>();
        if (actionsAsset == null) return list;
        foreach (var map in actionsAsset.actionMaps)
            foreach (var act in map.actions)
                if (act.name.Equals(actionName, System.StringComparison.OrdinalIgnoreCase))
                    list.Add(act);
        return list;
    }

    private static string GetDisplayPath(InputAction a) => a != null ? $"{a.actionMap?.name}/{a.name}" : "(None)";

    #endregion
}