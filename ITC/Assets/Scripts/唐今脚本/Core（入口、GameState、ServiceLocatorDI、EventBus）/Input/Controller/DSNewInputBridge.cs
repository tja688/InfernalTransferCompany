// DSInputBridge.cs (Refactored with Auto-Bind and Custom Inspector)
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// MVC中的Controller。监听Input System的输入动作，并将它们翻译成对DialogueStateManager的调用。
/// 集成了编辑器自动绑定功能，极大简化了配置流程。
/// </summary>
public class DSNewInputBridge : MonoBehaviour
{
    // ---------- 编辑器自动绑定功能所需字段 ----------
    [Header("Input Actions Asset (Auto Bind)")]
    [Tooltip("拖入你的Input Actions (InputActionAsset)。脚本会尝试按命名自动匹配。")]
    public InputActionAsset actionsAsset;

    [Tooltip("在编辑器修改时自动尝试匹配一次。也可手动点击Inspector的按钮。")]
    public bool autoBindOnValidate = true;

    // ---------- 核心输入动作引用 ----------
    [Header("Input Action References")]
    public InputActionReference submit;
    public InputActionReference cancel;
    public InputActionReference navigate;
    public InputActionReference backlog;
    public InputActionReference quickSave;
    public InputActionReference quickLoad;
    public InputActionReference openMenu; // 假设也需要这个
    // ... 可根据你的InputActionAsset添加更多引用

    #region 生命周期与输入订阅

    private void OnEnable()
    {
        // 订阅所有相关的输入动作
        Subscribe(submit, ctx => DialogueStateManager.Instance.OnSubmitIntent());
        Subscribe(cancel, ctx => DialogueStateManager.Instance.OnCancelIntent());
        Subscribe(navigate, ctx => DialogueStateManager.Instance.OnNavigateIntent(ctx.ReadValue<Vector2>()));
        Subscribe(backlog, ctx => DialogueStateManager.Instance.OnToggleBacklogIntent());
        Subscribe(quickSave, ctx => DialogueStateManager.Instance.OnQuickSaveIntent());
        Subscribe(quickLoad, ctx => DialogueStateManager.Instance.OnQuickLoadIntent());
        // 可以在这里订阅 openMenu 等其他动作
    }

    private void OnDisable()
    {
        // 注意：Unsubscribe方法现在通过禁用action来工作，因为它无法移除匿名函数。
        Unsubscribe(submit);
        Unsubscribe(cancel);
        Unsubscribe(navigate);
        Unsubscribe(backlog);
        Unsubscribe(quickSave);
        Unsubscribe(quickLoad);
    }

    private void Subscribe(InputActionReference actionRef, System.Action<InputAction.CallbackContext> handler)
    {
        if (actionRef == null || actionRef.action == null) return;
        actionRef.action.performed += handler;
        actionRef.action.Enable();
    }

    private void Unsubscribe(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return;
        actionRef.action.Disable();
    }

    #endregion

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // 在 Inspector 发生修改时，尝试做一次自动绑定（静默模式）
        if (autoBindOnValidate)
            TryAutoBindAll(false);
    }
    #endif

    #region 自动绑定核心逻辑

    // 字段名 → 目标 Action 名 (基于您的命名习惯)
    private static readonly (string field, string actionName)[] s_FieldToAction =
    {
        (nameof(submit),     "Submit"),
        (nameof(cancel),     "Cancel"),
        (nameof(navigate),   "Navigate"),
        (nameof(backlog),    "Backlog"),
        (nameof(quickSave),  "QuickSave"),
        (nameof(quickLoad),  "QuickLoad"),
        (nameof(openMenu),   "OpenMenu"),
        // ... 如果未来有更多动作，在这里添加映射
    };

    /// <summary>
    /// 遍历映射表，为每个“尚未设置”的字段在actionsAsset中精确按名字查找匹配的Action。
    /// </summary>
    public void TryAutoBindAll(bool logToConsole = true)
    {
        if (actionsAsset == null)
        {
            if (logToConsole) Debug.LogWarning("[DSInputBridge] Actions Asset未分配，无法自动绑定。", this);
            return;
        }

        foreach (var pair in s_FieldToAction)
        {
            var fieldInfo = GetType().GetField(pair.field);
            if (fieldInfo == null) continue;
            
            var current = fieldInfo.GetValue(this) as InputActionReference;
            if (current != null && current.asset != null) continue; // 如果已有引用，则跳过

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
                if (act.name.Equals(actionName, System.StringComparison.OrdinalIgnoreCase)) // 忽略大小写匹配
                    list.Add(act);
        return list;
    }

    private static string GetDisplayPath(InputAction a) => a != null ? $"{a.actionMap?.name}/{a.name}" : "(None)";

    #endregion
}
