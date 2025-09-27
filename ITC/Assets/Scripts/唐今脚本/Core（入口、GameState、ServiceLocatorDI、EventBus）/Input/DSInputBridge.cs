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

[DisallowMultipleComponent]
public class DSInputBridge : MonoBehaviour
{
    // ---------- 新增：自动绑定所需 ----------
    [Header("Input Actions Asset (Auto Bind)")]
    [Tooltip("拖入你的 Input Actions (InputActionAsset)。脚本会尝试按命名自动匹配。")]
    public InputActionAsset actionsAsset;

    [Tooltip("在编辑器修改时自动尝试匹配一次。也可手动点 Inspector 的按钮。")]
    public bool autoBindOnValidate = true;

    // ---------- 原有公开字段 ----------
    [Header("Input (Input System)")]
    public InputActionReference submit;      // Gameplay_Main/Submit
    public InputActionReference cancel;      // Gameplay_Main/Cancel
    public InputActionReference navigate;    // Gameplay_Main/Navigate（可选）
    public InputActionReference scrollWheel; // Gameplay_Main/ScrollWheel（可选）
    public InputActionReference backlog;     // Gameplay_Main/Backlog
    public InputActionReference autoMode;    // Gameplay_Main/AutoMode
    public InputActionReference fastRead;    // Gameplay_Main/FastRead（单句爆发）
    public InputActionReference skipRead;    // Gameplay_Main/SkipRead（长按快进）
    public InputActionReference quickSave;   // Gameplay_Main/QuickSave
    public InputActionReference quickLoad;   // Gameplay_Main/QuickLoad
    public InputActionReference openMenu;    // Gameplay_Main/OpenMenu

    [Header("UI References")]
    public Button continueButton;            // DS UI 的“继续/下一句”按钮
    public GameObject backlogPanel;          // 文本日志面板（可选）
    public UnityEvent onOpenSystemMenu;      // 打开系统菜单
    public UnityEvent onCancel;              // 回退逻辑

    [Header("Read Speed")]
    public float normalCPS = 30f;            // 与 Dialogue Manager 面板一致
    public float fastCPS = 9999f;            // 快速阅读/跳读时使用

    [Header("Save/Load")]
    public int quickSlot = 0;                // 快速存读档槽位号（自定）

    [Header("Auto Find Runtime UI")]
    [Tooltip("勾选后，脚本会在运行时自动寻找 DS 实例化出来的 Continue 按钮和 Backlog 面板。")]
    public bool autoFindRuntimeUI = true;

    [Tooltip("用于查找 Continue 按钮的节点名（精确匹配）。")]
    public string continueButtonName = "Continue Button";

    [Tooltip("用于查找 Backlog 面板的节点名（精确匹配，若未找到会尝试按组件类型查找）。")]
    public string backlogWindowName = "Dialogue Log Window";
    
    private float _origCPS;
    private bool _isSkipping;

    // ---------- 生命周期 ----------
    void OnEnable()
    {
        EnableAction(submit, OnSubmit);
        EnableAction(cancel, OnCancel);
        EnableAction(backlog, OnToggleBacklog);
        EnableAction(autoMode, OnToggleAuto);
        EnableAction(fastRead, OnFastReadOnce);
        EnableAction(skipRead, OnSkipPress, OnSkipRelease);
        EnableAction(quickSave, OnQuickSave);
        EnableAction(quickLoad, OnQuickLoad);
        EnableAction(openMenu, OnOpenMenu);

        _origCPS = DialogueManager.displaySettings.subtitleSettings.subtitleCharsPerSecond;
        if (_origCPS <= 0) _origCPS = normalCPS;
    }

    void OnDisable()
    {
        // DialogueManager 可能此时不存在/已销毁，先判断
        SafeSetCPS(_origCPS);
        _isSkipping = false;

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
    void SafeSetCPS(float cps)
    {
        // 任何时候都先判断 DialogueManager 是否可用
        if (DialogueManager.instance == null) return;

        // displaySettings 是只读属性，但内部的 subtitleSettings 可直接写字段
        var ds = DialogueManager.displaySettings;
        var sub = ds.subtitleSettings;
        sub.subtitleCharsPerSecond = cps;
        ds.subtitleSettings = sub;               // 结构体回写
    }

    void Start()
    {
        if (autoFindRuntimeUI)
            StartCoroutine(LateFindRuntimeUI());
    }

    System.Collections.IEnumerator LateFindRuntimeUI()
    {
        // 让 Dialogue Manager 有时间把 UI/预制体实例化出来
        yield return null; 
        yield return null;

        // 1) 找 Continue 按钮（按名字）
        if (continueButton == null)
        {
            var allBtns = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>();
            var btn = allBtns.FirstOrDefault(b => b != null && b.name == continueButtonName);
            if (btn != null) continueButton = btn;
        }

        // 2) 找 Backlog 面板：优先找带 DialogueLogWindow 组件的；否则按名字找；再否则找名含 "Backlog" 的面板
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
    
    void Update()
    {
        if (_isSkipping && continueButton != null) SubmitUI(continueButton.gameObject);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (autoBindOnValidate)
            TryAutoBindAll(false); // 静默模式，不打印日志
    }
#endif

    // ---------- 自动绑定核心 ----------
    // 字段名 → 目标 Action 名（基于你的命名）
    static readonly (string field, string actionName)[] s_FieldToAction =
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

    /// <summary>遍历上表，对每个未设置的字段尝试创建唯一匹配的 InputActionReference。</summary>
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
                UnityEditor.EditorUtility.SetDirty(this);
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

    /// <summary>在整个 Asset 中按 action.name 精确匹配。</summary>
    List<InputAction> FindActionsByName(string actionName)
    {
        var list = new List<InputAction>();
        foreach (var map in actionsAsset.actionMaps)
            foreach (var act in map.actions)
                if (act.name == actionName)
                    list.Add(act);
        return list;
    }

    static string GetDisplayPath(InputAction a)
        => a != null ? $"{a.actionMap?.name}/{a.name}" : "(None)";

    // ---------- 绑定/解绑辅助 ----------
    void EnableAction(InputActionReference aref, System.Action<InputAction.CallbackContext> performed,
                      System.Action<InputAction.CallbackContext> canceled = null)
    {
        if (aref == null) return;
        aref.action.performed += performed;
        if (canceled != null) aref.action.canceled += canceled;
        aref.action.Enable();
    }
    void DisableAction(InputActionReference aref, System.Action<InputAction.CallbackContext> performed,
                       System.Action<InputAction.CallbackContext> canceled = null)
    {
        if (aref == null) return;
        aref.action.performed -= performed;
        if (canceled != null) aref.action.canceled -= canceled;
        aref.action.Disable();
    }

    // ---------- Handlers ----------
    void OnSubmit(InputAction.CallbackContext _)
    {
        // 1) 如果当前选中的是一个响应按钮 → 直接“点击”它
        var current = EventSystem.current?.currentSelectedGameObject;
        if (current != null && current.GetComponent<PixelCrushers.DialogueSystem.StandardUIResponseButton>() != null)
        {
            ExecuteEvents.Execute(current, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            return;
        }

        // 2) 如果响应菜单已打开但当前没有选中任何按钮 → 选中并点击首个响应
        var firstResp = FindObjectOfType<PixelCrushers.DialogueSystem.StandardUIResponseButton>();
        if (firstResp != null)
        {
            var go = firstResp.gameObject;
            EventSystem.current.SetSelectedGameObject(go);
            ExecuteEvents.Execute(go, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            return;
        }

        // 3) 否则：处于字幕阶段，点击“继续”
        if (continueButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(continueButton.gameObject);
            ExecuteEvents.Execute(continueButton.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            return;
        }

        // 4) 最末兜底：直接让对话继续（不依赖 UI）
        if (DialogueManager.isConversationActive)
        {
            // 兼容不同版本，尽量用消息或公共方法
            var stdUI = FindObjectOfType<PixelCrushers.DialogueSystem.StandardDialogueUI>();
            if (stdUI != null) stdUI.SendMessage("OnContinue", SendMessageOptions.DontRequireReceiver);
        }
    }
    void OnCancel(InputAction.CallbackContext _) { onCancel?.Invoke(); }
    void OnToggleBacklog(InputAction.CallbackContext _) { if (backlogPanel) { backlogPanel.SetActive(!backlogPanel.activeSelf); var s = backlogPanel.GetComponentInChildren<Selectable>(); if (backlogPanel.activeSelf && s) s.Select(); } }
    void OnToggleAuto(InputAction.CallbackContext _) { bool on = !Mathf.Approximately(DialogueManager.displaySettings.subtitleSettings.subtitleCharsPerSecond, fastCPS); SetCPS(on ? fastCPS : _origCPS); }
    void OnFastReadOnce(InputAction.CallbackContext _) { StartCoroutine(FastReadBurst()); }
    System.Collections.IEnumerator FastReadBurst() { float prev = DialogueManager.displaySettings.subtitleSettings.subtitleCharsPerSecond; SetCPS(fastCPS); yield return null; if (continueButton) SubmitUI(continueButton.gameObject); SetCPS(prev); }
    void OnSkipPress(InputAction.CallbackContext _) { _isSkipping = true; SetCPS(fastCPS); }
    void OnSkipRelease(InputAction.CallbackContext _) { _isSkipping = false; SetCPS(_origCPS); }
    void OnQuickSave(InputAction.CallbackContext _) { SaveSystem.SaveToSlot(quickSlot); }
    void OnQuickLoad(InputAction.CallbackContext _) { SaveSystem.LoadFromSlot(quickSlot); }
    void OnOpenMenu(InputAction.CallbackContext _) { onOpenSystemMenu?.Invoke(); }

    // ---------- Utilities ----------
    void SubmitUI(GameObject go)
    {
        if (!go || !EventSystem.current) return;
        EventSystem.current.SetSelectedGameObject(go);
        ExecuteEvents.Execute(go, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }
    void SetCPS(float cps)
    {
        DialogueManager.displaySettings.subtitleSettings.subtitleCharsPerSecond = cps;
    }
}

#if UNITY_EDITOR
// =================== 自定义 Inspector：提供下拉选择 ===================
[CustomEditor(typeof(DSInputBridge))]
public class DSInputBridgeEditor : Editor
{
    private DSInputBridge _t;
    private string[] _options;        // "Map/Action"
    private InputAction[] _acts;      // 对应真实对象
    private GUIStyle _miniButton;

    void OnEnable()
    {
        _t = (DSInputBridge)target;
        _miniButton = new GUIStyle(EditorStyles.miniButton) { fixedHeight = 18f };
        RefreshActionList();
    }

    void RefreshActionList()
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

        EditorGUI.BeginChangeCheck();
        _t.actionsAsset = (InputActionAsset)EditorGUILayout.ObjectField(new GUIContent("Actions Asset"), _t.actionsAsset, typeof(InputActionAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            RefreshActionList();
        }

        _t.autoBindOnValidate = EditorGUILayout.ToggleLeft("Auto Bind On Validate", _t.autoBindOnValidate);

        if (GUILayout.Button("Try Auto Bind Now", _t.actionsAsset ? EditorStyles.miniButton : EditorStyles.miniButtonMid))
        {
            _t.TryAutoBindAll(true);
            RefreshActionList();
        }

        EditorGUILayout.Space(6);
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
        base.OnInspectorGUI(); // 画 UI 引用、速度、存档槽等

        serializedObject.ApplyModifiedProperties();
    }

    void DrawActionPopup(string label, ref InputActionReference field)
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
