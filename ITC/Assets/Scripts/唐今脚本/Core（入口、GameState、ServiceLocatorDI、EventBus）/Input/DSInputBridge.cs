using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PixelCrushers;                   // SaveSystem
using PixelCrushers.DialogueSystem;   // DialogueManager

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// DSInputBridge
/// - 将“新输入系统”的动作映射到 PixelCrushers Dialogue System 的具体交互：
///   提交/取消/打开系统菜单/Backlog 面板开关/读速控制/快速存读档等；
/// - 运行时可自动寻找 Continue 按钮与 Backlog 面板；
/// - Inspector 支持一键“自动绑定”你的 InputActionAsset；
/// - 内置完整的“提交兜底链路”，避免因为没有焦点而无法提交的问题。
/// 使用说明：
/// 1. 将你的 InputActionAsset 拖入 actionsAsset；
/// 2. 点击“Try Auto Bind Now”或勾选 autoBindOnValidate；
/// 3. 可在“UI References”中手动指定 ContinueButton/BacklogPanel，
///    或保持默认名字以便运行时自动查找；
/// 4. 在“Save/Load”中设置快捷存档槽位 quickSlot；
/// 5. 运行游戏并验证按键行为。
/// </summary>
[DisallowMultipleComponent]
public class DSInputBridge : MonoBehaviour
{
    // ---------- 新增：自动绑定所需 ----------
    [Header("Input Actions Asset (Auto Bind)")]
    [Tooltip("拖入你的 Input Actions (InputActionAsset)。脚本会尝试按命名自动匹配。")]
    public InputActionAsset actionsAsset;

    [Tooltip("在编辑器修改时自动尝试匹配一次。也可手动点 Inspector 的按钮。")]
    public bool autoBindOnValidate = true;

    // ---------- 公开可在 Inspector 绑定的动作 ----------
    [Header("Input (Input System)")]
    public InputActionReference submit;      // Gameplay_Main/Submit
    public InputActionReference cancel;      // Gameplay_Main/Cancel
    public InputActionReference navigate;    // Gameplay_Main/Navigate（可选，用于 UGUI 导航）
    public InputActionReference scrollWheel; // Gameplay_Main/ScrollWheel（可选，用于 Backlog 滚动）
    public InputActionReference backlog;     // Gameplay_Main/Backlog
    public InputActionReference autoMode;    // Gameplay_Main/AutoMode
    public InputActionReference fastRead;    // Gameplay_Main/FastRead（单句爆发）
    public InputActionReference skipRead;    // Gameplay_Main/SkipRead（长按快进）
    public InputActionReference quickSave;   // Gameplay_Main/QuickSave
    public InputActionReference quickLoad;   // Gameplay_Main/QuickLoad
    public InputActionReference openMenu;    // Gameplay_Main/OpenMenu

    // ---------- 与 UI 交互所需的引用 ----------
    [Header("UI References")]
    [Tooltip("DS UI 的“继续/下一句”按钮。可留空，运行时自动查找。")]
    public Button continueButton;

    [Tooltip("文本日志（Backlog）根面板。可留空，运行时自动查找。")]
    public GameObject backlogPanel;

    [Tooltip("打开系统菜单时调用的事件（在 Inspector 里拖你的系统菜单打开函数）。")]
    public UnityEvent onOpenSystemMenu;

    [Tooltip("取消/回退逻辑（在 Inspector 里拖你的返回处理）。")]
    public UnityEvent onCancel;

    // ---------- 读速 ----------
    [Header("Read Speed")]
    [Tooltip("正常阅读时每秒字符数（与 DialogueManager 面板一致）。")]
    public float normalCPS = 30f;

    [Tooltip("快速阅读/跳读时使用的超高每秒字符数。")]
    public float fastCPS = 9999f;

    // ---------- 存读档 ----------
    [Header("Save/Load")]
    [Tooltip("快速存读档的槽位编号。")]
    public int quickSlot = 0;

    // ---------- 运行时自动查找 UI ----------
    [Header("Auto Find Runtime UI")]
    [Tooltip("勾选后，脚本会在运行时自动寻找 DS 实例化出来的 Continue 按钮和 Backlog 面板。")]
    public bool autoFindRuntimeUI = true;

    [Tooltip("用于查找 Continue 按钮的节点名（精确匹配）。")]
    public string continueButtonName = "Continue Button";

    [Tooltip("用于查找 Backlog 面板的节点名（精确匹配，若未找到会尝试按组件类型查找）。")]
    public string backlogWindowName = "Dialogue Log Window";

    // ---------- 内部状态 ----------
    private float _origCPS;     // 进入脚本时的 DS 读速，供还原用
    private bool _isSkipping;   // 是否处于长按快进

    // ================== 生命周期 ==================

    private void OnEnable()
    {
        // 订阅输入事件并启用动作
        EnableAction(submit, OnSubmit);
        EnableAction(cancel, OnCancel);
        EnableAction(backlog, OnToggleBacklog);
        EnableAction(autoMode, OnToggleAuto);
        EnableAction(fastRead, OnFastReadOnce);
        EnableAction(skipRead, OnSkipPress, OnSkipRelease);
        EnableAction(quickSave, OnQuickSave);
        EnableAction(quickLoad, OnQuickLoad);
        EnableAction(openMenu, OnOpenMenu);

        // 记录进入时的 DS 读速（若 DS 面板里为 0 或异常，采用 normalCPS）
        _origCPS = DialogueManager.displaySettings.subtitleSettings.subtitleCharsPerSecond;
        if (_origCPS <= 0) _origCPS = normalCPS;
    }

    private void OnDisable()
    {
        // 恢复读速、复位状态（注意：DialogueManager 可能为空，故用安全写法）
        SafeSetCPS(_origCPS);
        _isSkipping = false;

        // 取消订阅并禁用动作
        DisableAction(submit, OnSubmit);
        DisableAction(cancel, OnCancel);
        DisableAction(backlog, OnToggleBacklog);
        DisableAction(autoMode, OnToggleAuto);
        DisableAction(fastRead, OnFastReadOnce);
        DisableAction(skipRead, OnSkipPress, OnSkipRelease);
        DisableAction(quickSave, OnQuickSave);
        DisableAction(quickLoad, OnQuickLoad);
        DisableAction(openMenu, OnOpenMenu);
    }

    /// <summary>
    /// 安全设置 DS 读速（结构体回写）
    /// </summary>
    private void SafeSetCPS(float cps)
    {
        if (DialogueManager.instance == null) return;

        var ds = DialogueManager.displaySettings;
        var sub = ds.subtitleSettings;               // 注意：struct 拷贝
        sub.subtitleCharsPerSecond = cps;
        ds.subtitleSettings = sub;                   // ★ 结构体回写
    }

    private void Start()
    {
        if (autoFindRuntimeUI)
            StartCoroutine(LateFindRuntimeUI());
    }

    /// <summary>
    /// 等两帧，待 DS UI 预制体实例化完成后再查找 Continue 按钮与 Backlog 面板。
    /// </summary>
    private System.Collections.IEnumerator LateFindRuntimeUI()
    {
        yield return null; // 等一帧
        yield return null; // 再等一帧

        // 1) Continue 按钮（按名字查找）
        if (continueButton == null)
        {
            var allBtns = Resources.FindObjectsOfTypeAll<Button>();
            var btn = allBtns.FirstOrDefault(b => b != null && b.name == continueButtonName);
            if (btn != null) continueButton = btn;
        }

        // 2) Backlog 面板：优先按组件类型名 DialogueLogWindow 查找 -> 再按名字 -> 再模糊包含“Backlog”
        if (backlogPanel == null)
        {
            var logWin = Resources.FindObjectsOfTypeAll<Component>()
                .FirstOrDefault(c => c && c.GetType().Name == "DialogueLogWindow");
            if (logWin != null) backlogPanel = logWin.gameObject;
        }
        if (backlogPanel == null)
        {
            var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
            backlogPanel = allGos.FirstOrDefault(go => go && go.name == backlogWindowName);
        }
        if (backlogPanel == null)
        {
            var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
            backlogPanel = allGos.FirstOrDefault(go => go && go.name.Contains("Backlog"));
        }
    }

    private void Update()
    {
        // 长按快进：每帧触发一次“提交”，快速推进到下一句
        if (_isSkipping && continueButton != null) SubmitUI(continueButton.gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 在 Inspector 发生修改时，尝试做一次自动绑定（静默模式）
        if (autoBindOnValidate)
            TryAutoBindAll(false);
    }
#endif

    // ================== 自动绑定核心 ==================

    // 字段名 → 目标 Action 名（基于你的命名）
    private static readonly (string field, string actionName)[] s_FieldToAction =
    {
        (nameof(submit),     "Submit"),
        (nameof(cancel),     "Cancel"),
        (nameof(navigate),   "Navigate"),
        (nameof(scrollWheel),"ScrollWheel"),
        (nameof(backlog),    "Backlog"),
        (nameof(autoMode),   "AutoMode"),
        (nameof(fastRead),   "FastRead"),
        (nameof(skipRead),   "SkipRead"),
        (nameof(quickSave),  "QuickSave"),
        (nameof(quickLoad),  "QuickLoad"),
        (nameof(openMenu),   "OpenMenu"),
    };

    /// <summary>
    /// 遍历上表，对每个“尚未设置”的字段尝试在 actionsAsset 中**精确按名字**找到唯一匹配的 Action，
    /// 并创建 InputActionReference 赋回 Inspector 字段。
    /// </summary>
    public void TryAutoBindAll(bool logToConsole = true)
    {
        if (actionsAsset == null) return;

        foreach (var pair in s_FieldToAction)
        {
            var fieldInfo = GetType().GetField(pair.field);
            var current = fieldInfo.GetValue(this) as InputActionReference;
            if (current != null) continue; // 已有就不改

            var matches = FindActionsByName(pair.actionName);
            if (matches.Count == 1)
            {
                var created = InputActionReference.Create(matches[0]);
                fieldInfo.SetValue(this, created);
#if UNITY_EDITOR
                if (logToConsole)
                    Debug.Log($"[DSInputBridge] Auto-bound {pair.field} -> {GetDisplayPath(matches[0])}", this);
                EditorUtility.SetDirty(this);
#endif
            }
            else
            {
#if UNITY_EDITOR
                if (logToConsole)
                {
                    if (matches.Count == 0)
                        Debug.LogWarning($"[DSInputBridge] 未在 {actionsAsset.name} 中找到名为 \"{pair.actionName}\" 的 Action。请在 Inspector 下拉中手动选择。", this);
                    else
                        Debug.LogWarning($"[DSInputBridge] 在 {actionsAsset.name} 中找到多个名为 \"{pair.actionName}\" 的 Action（{matches.Count} 个）。请在 Inspector 下拉中手动选择。", this);
                }
#endif
            }
        }
    }

    /// <summary>
    /// 在整个 InputActionAsset 中按动作名精确匹配。
    /// </summary>
    private List<InputAction> FindActionsByName(string actionName)
    {
        var list = new List<InputAction>();
        foreach (var map in actionsAsset.actionMaps)
            foreach (var act in map.actions)
                if (act.name == actionName)
                    list.Add(act);
        return list;
    }

    private static string GetDisplayPath(InputAction a)
        => a != null ? $"{a.actionMap?.name}/{a.name}" : "(None)";

    // ================== 绑定/解绑辅助 ==================

    /// <summary>
    /// 为给定 InputActionReference 绑定 performed/canceled 回调并启用。
    /// </summary>
    private void EnableAction(InputActionReference aref,
                              System.Action<InputAction.CallbackContext> performed,
                              System.Action<InputAction.CallbackContext> canceled = null)
    {
        if (aref == null) return;
        aref.action.performed += performed;
        if (canceled != null) aref.action.canceled += canceled;
        aref.action.Enable();
    }

    /// <summary>
    /// 为给定 InputActionReference 解绑 performed/canceled 并禁用。
    /// </summary>
    private void DisableAction(InputActionReference aref,
                               System.Action<InputAction.CallbackContext> performed,
                               System.Action<InputAction.CallbackContext> canceled = null)
    {
        if (aref == null) return;
        aref.action.performed -= performed;
        if (canceled != null) aref.action.canceled -= canceled;
        aref.action.Disable();
    }

    // ================== Handlers（输入回调） ==================

    /// <summary>
    /// 提交键：完整兜底链路（响应选项 -> 首个响应 -> Continue 按钮 -> OnContinue 消息）
    /// 解决“没有焦点无法提交”的问题。
    /// </summary>
    private void OnSubmit(InputAction.CallbackContext _)
    {
        // 1) 当前若已有一个 StandardUIResponseButton 被选中 → 直接“点击”
        var current = EventSystem.current?.currentSelectedGameObject;
        if (current != null &&
            current.GetComponent<PixelCrushers.DialogueSystem.StandardUIResponseButton>() != null)
        {
            ExecuteEvents.Execute(current, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            return;
        }

        // 2) 没有选中任何响应 → 尝试寻找场上第一个响应按钮，并选中 + 点击
        var firstResp = FindObjectOfType<PixelCrushers.DialogueSystem.StandardUIResponseButton>();
        if (firstResp != null)
        {
            var go = firstResp.gameObject;
            EventSystem.current?.SetSelectedGameObject(go);
            ExecuteEvents.Execute(go, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            return;
        }

        // 3) 处于字幕阶段 → 点击 Continue
        if (continueButton != null)
        {
            SubmitUI(continueButton.gameObject);
            return;
        }

        // 4) 最末兜底：直接让对话继续（即便没有 UI）
        if (DialogueManager.isConversationActive)
        {
            var stdUI = FindObjectOfType<PixelCrushers.DialogueSystem.StandardDialogueUI>();
            if (stdUI != null) stdUI.SendMessage("OnContinue", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void OnCancel(InputAction.CallbackContext _)     { onCancel?.Invoke(); }

    private void OnToggleBacklog(InputAction.CallbackContext _)
    {
        if (!backlogPanel) return;
        backlogPanel.SetActive(!backlogPanel.activeSelf);

        // 打开时给面板内任意 Selectable 一个焦点，便于手柄/方向键直接操作
        if (backlogPanel.activeSelf)
        {
            var s = backlogPanel.GetComponentInChildren<Selectable>();
            if (s) s.Select();
        }
    }

    private void OnToggleAuto(InputAction.CallbackContext _)
    {
        // 切换“极速/原速”，用当前 DS 的 CPS 判断状态
        var cur = DialogueManager.displaySettings.subtitleSettings.subtitleCharsPerSecond;
        bool goFast = !Mathf.Approximately(cur, fastCPS);
        SetCPS(goFast ? fastCPS : _origCPS);
    }

    private void OnFastReadOnce(InputAction.CallbackContext _)
    {
        // 本帧提速到 fastCPS，立刻提交一次，然后恢复原来的 CPS
        StartCoroutine(FastReadBurst());
    }

    private System.Collections.IEnumerator FastReadBurst()
    {
        float prev = DialogueManager.displaySettings.subtitleSettings.subtitleCharsPerSecond;
        SetCPS(fastCPS);
        yield return null; // 等一帧，使得提速对本帧字幕生效
        if (continueButton) SubmitUI(continueButton.gameObject);
        SetCPS(prev);
    }

    private void OnSkipPress(InputAction.CallbackContext _)
    {
        _isSkipping = true;
        SetCPS(fastCPS);
    }

    private void OnSkipRelease(InputAction.CallbackContext _)
    {
        _isSkipping = false;
        SetCPS(_origCPS);
    }

    private void OnQuickSave(InputAction.CallbackContext _)  { SaveSystem.SaveToSlot(quickSlot); }
    private void OnQuickLoad(InputAction.CallbackContext _)  { SaveSystem.LoadFromSlot(quickSlot); }
    private void OnOpenMenu (InputAction.CallbackContext _)  { onOpenSystemMenu?.Invoke(); }

    // ================== Utilities ==================

    /// <summary>
    /// 对给定 UI 对象执行“提交”（会先选中再触发 submitHandler）。
    /// </summary>
    private void SubmitUI(GameObject go)
    {
        if (!go || !EventSystem.current) return;
        EventSystem.current.SetSelectedGameObject(go);
        ExecuteEvents.Execute(go, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }

    /// <summary>
    /// 设置 DS 的每秒字符数（结构体回写版，确保生效）。
    /// </summary>
    private void SetCPS(float cps)
    {
        if (DialogueManager.instance == null) return;

        var ds = DialogueManager.displaySettings;
        var sub = ds.subtitleSettings;               // 注意：struct 拷贝
        sub.subtitleCharsPerSecond = cps;
        ds.subtitleSettings = sub;                   // ★ 结构体回写
    }
}

#if UNITY_EDITOR
// =================== 自定义 Inspector：提供下拉选择 / 自动绑定按钮 ===================
[CustomEditor(typeof(DSInputBridge))]
public class DSInputBridgeEditor : Editor
{
    private DSInputBridge _t;
    private string[] _options;        // 显示用 "Map/Action"
    private InputAction[] _acts;      // 实际对象
    private GUIStyle _miniButton;

    private void OnEnable()
    {
        _t = (DSInputBridge)target;
        _miniButton = new GUIStyle(EditorStyles.miniButton) { fixedHeight = 18f };
        RefreshActionList();
    }

    private void RefreshActionList()
    {
        if (_t.actionsAsset == null)
        {
            _options = new[] { "(No Actions Asset)" };
            _acts = new InputAction[0];
            return;
        }

        var acts = new List<InputAction>();
        var labels = new List<string>();
        foreach (var map in _t.actionsAsset.actionMaps)
            foreach (var a in map.actions)
            {
                acts.Add(a);
                labels.Add($"{map.name}/{a.name}");
            }
        _acts = acts.ToArray();
        _options = labels.ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 选择 ActionAsset
        EditorGUI.BeginChangeCheck();
        _t.actionsAsset = (InputActionAsset)EditorGUILayout.ObjectField(
            new GUIContent("Actions Asset"), _t.actionsAsset, typeof(InputActionAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            RefreshActionList();
        }

        // 自动绑定选项与按钮
        _t.autoBindOnValidate = EditorGUILayout.ToggleLeft("Auto Bind On Validate", _t.autoBindOnValidate);
        if (GUILayout.Button("Try Auto Bind Now", _t.actionsAsset ? EditorStyles.miniButton : EditorStyles.miniButtonMid))
        {
            _t.TryAutoBindAll(true);
            RefreshActionList();
        }

        EditorGUILayout.Space(6);

        // 逐个字段的下拉框
        DrawActionPopup(nameof(_t.submit), ref _t.submit);
        DrawActionPopup(nameof(_t.cancel), ref _t.cancel);
        DrawActionPopup(nameof(_t.navigate), ref _t.navigate);
        DrawActionPopup(nameof(_t.scrollWheel), ref _t.scrollWheel);
        DrawActionPopup(nameof(_t.backlog), ref _t.backlog);
        DrawActionPopup(nameof(_t.autoMode), ref _t.autoMode);
        DrawActionPopup(nameof(_t.fastRead), ref _t.fastRead);
        DrawActionPopup(nameof(_t.skipRead), ref _t.skipRead);
        DrawActionPopup(nameof(_t.quickSave), ref _t.quickSave);
        DrawActionPopup(nameof(_t.quickLoad), ref _t.quickLoad);
        DrawActionPopup(nameof(_t.openMenu), ref _t.openMenu);

        EditorGUILayout.Space(8);

        // 其他公开字段（UI 引用、读速、存档槽等）
        base.OnInspectorGUI();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 为某个 InputActionReference 字段绘制下拉选择（Map/Action）与 Clear 按钮。
    /// </summary>
    private void DrawActionPopup(string label, ref InputActionReference field)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel(ObjectNames.NicifyVariableName(label));

            int current = -1;
            if (field != null && field.action != null && _acts != null)
                current = System.Array.IndexOf(_acts, field.action);

            GUI.enabled = _t.actionsAsset != null && _acts.Length > 0;
            int next = EditorGUILayout.Popup(current, _options);
            GUI.enabled = true;

            if (next != current && next >= 0 && next < _acts.Length)
            {
                field = InputActionReference.Create(_acts[next]);
                EditorUtility.SetDirty(_t);
            }

            if (GUILayout.Button("Clear", _miniButton, GUILayout.Width(50)))
            {
                field = null;
                EditorUtility.SetDirty(_t);
            }
        }
    }
}
#endif
