using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using PixelCrushers.DialogueSystem;
using PrimeTween;

/// <summary>
/// 纯状态驱动的历史对话管理器：
/// - 只调用 RecordUnitView 的状态切换 & 运动接口；
/// - 背景切换完全由 RecordUnitView 内部负责；
/// - 统一移动时长与 Ease；
/// - 鼠标滚轮浏览历史；
/// - 任意打字机进行中时，禁止滚动；
/// - 层级排序：当前(Initial) > 历史(History) > 隐藏(Hidden)。
/// </summary>
public class HistoryDialogueManager_PureState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IScrollHandler {
    [Header("Prefab & Hierarchy")]
    public RecordUnitView unitPrefab;         // 最小记录单位（内部自管 spriteA/B）
    public RectTransform listParent;          // 生成父节点
    public RectTransform scrollArea;          // 鼠标移入此区域才启用滚轮

    [Header("Positions (anchoredPosition)")]
    public Vector2 posInitial = new(0, 0);    // 初次展示位置（A）
    public Vector2 posHistory = new(0, -60);  // 历史展示位置（B）
    public Vector2 posHidden  = new(0, -200); // 隐藏位置（C）

    [Header("Unified Move Style")]
    public float moveDuration = 0.25f;
    public Ease  moveEase     = Ease.InOutSine;
    public float fadeDuration = 0.20f;        // 隐显时可用（RecordUnitView.FadeTo）

    [Header("Scroll")]
    public bool enableScroll   = true;
    public float scrollNotch   = 1f;          // 累计到这个阈值触发一次切换
    public double scrollThrottle = 0.12;      // 切换节流（秒）

    [Header("Guards & Filters")]
    public bool onlyShowNPC  = true;          // 只展示 NPC 台词
    public double dedupeWindow = 0.10;        // 去重阀（秒）
    public bool debugLog = false;

    // 运行时
    readonly List<RecordUnitView> units = new(); // 从旧到新
    bool pointerInside;
    float scrollAccum;
    double lastScrollTime;
    string _lastKey; double _lastKeyTime;

    // 当前是否有任意条在打字（禁止滚动）
    bool AnyTyping {
        get {
            for (int i = 0; i < units.Count; i++) {
                var u = units[i];
                if (u && u.IsTyping) return true;
            }
            return false;
        }
    }

    // -1 表示“跟随最新”；>=0 表示聚焦某条历史
    int focusedIdx = -1;

    void OnEnable() {
        DSGlobalMessageBridge.OnConvStart += OnConversationStart;
        DSGlobalMessageBridge.OnConvLine  += OnConversationLine;
        DSGlobalMessageBridge.OnConvEnd   += OnConversationEnd;
    }
    void OnDisable() {
        DSGlobalMessageBridge.OnConvStart -= OnConversationStart;
        DSGlobalMessageBridge.OnConvLine  -= OnConversationLine;
        DSGlobalMessageBridge.OnConvEnd   -= OnConversationEnd;
    }

    // ---------- DS 事件 ----------
    void OnConversationStart(Transform actor) {
        // 不创建，等第一句 NPC 来再处理
        if (debugLog) Debug.Log("[Mgr] ConversationStart");
    }

    void OnConversationEnd(Transform actor) {
        if (debugLog) Debug.Log("[Mgr] ConversationEnd");
        // 历史保留，不清空
    }

    void OnConversationLine(Subtitle s) {
        if (s == null) return;

        // 过滤玩家台词 / 空文本 / 重复
        bool isPlayer = s.speakerInfo != null && s.speakerInfo.isPlayer;
        if (onlyShowNPC && isPlayer) { if (debugLog) Debug.Log("[Mgr] Skip player line"); return; }
        string text = s.formattedText?.text ?? "";
        if (string.IsNullOrWhiteSpace(text)) { if (debugLog) Debug.Log("[Mgr] Skip empty text"); return; }
        int conv = s.dialogueEntry?.conversationID ?? -1;
        int id   = s.dialogueEntry?.id ?? -1;
        string key = $"{conv}:{id}:{text}";
        if (_lastKey == key && (Time.timeAsDouble - _lastKeyTime) < dedupeWindow) {
            if (debugLog) Debug.Log("[Mgr] Dup suppressed");
            return;
        }
        _lastKey = key; _lastKeyTime = Time.timeAsDouble;

        // 若正在浏览历史（focusedIdx >= 0），新消息到来：按产品需要
        // 这里选择：不打断当前浏览，但把“最新条”依然生成并定格到 History 位；
        // 也可以选择强制回到“跟随最新”（focusedIdx = -1）。
        if (focusedIdx >= 0) {
            if (debugLog) Debug.Log("[Mgr] New line while browsing history (focus locked)");
        }

        // 1) 把上一条（若存在）切到 历史位（B），再把更早一条切到 隐藏位（C）
        var last = units.Count > 0 ? units[^1] : null;
        if (last != null) {
            if (last.IsTyping) last.StopTyping(); // 立即定格，避免冲突
            // 进入历史态（背景切换由单元内部负责）
            last.SetStateHistoryInstant(last.GetComponent<RectTransform>().anchoredPosition);
            last.MoveTo(posHistory, moveDuration, moveEase);

            if (units.Count > 1) {
                var older = units[^2];
                if (older) {
                    older.SetStateHiddenInstant(older.GetComponent<RectTransform>().anchoredPosition);
                    older.MoveTo(posHidden, moveDuration, moveEase);
                    older.FadeTo(0f, fadeDuration, moveEase);
                }
            }
        }

        // 2) 生成新单元：Initial 位（A），开始打字
        var u = Instantiate(unitPrefab, listParent);
        u.SetStateInitialInstant(posInitial);
        u.FadeTo(1f, 0.01f, Ease.Linear);
        u.OnTypingFinished += OnUnitTypingFinished;
        units.Add(u);

        string speaker = s.speakerInfo?.Name;
        u.PlayTypewriter(speaker, text);

        // 3) 统一层级：当前 > 历史 > 隐藏
        ReorderLayers();
        // 新消息默认回到“跟随最新”
        focusedIdx = -1;
    }

    void OnUnitTypingFinished(RecordUnitView u) {
        if (u == null) return;
        u.OnTypingFinished -= OnUnitTypingFinished;

        // 初次展示结束后，进入历史态（背景切换由单元内部处理）
        if (u.State == RecordUnitView.ViewState.Initial) {
            u.SetStateHistoryInstant(u.GetComponent<RectTransform>().anchoredPosition);
            u.MoveTo(posHistory, moveDuration, moveEase);
        }

        // 打字结束也统一一下层级（以防期间有滚轮操作）
        ReorderLayers();
    }

    // ---------- 滚轮 ----------
    public void OnPointerEnter(PointerEventData e) {
        if (!enableScroll) return;
        if (scrollArea == null || e.pointerEnter == scrollArea.gameObject || e.pointerEnter.transform.IsChildOf(scrollArea)) {
            pointerInside = true;
            scrollAccum = 0f;
        }
    }
    public void OnPointerExit(PointerEventData e) {
        if (!enableScroll) return;
        pointerInside = false;
        scrollAccum = 0f;
    }

    public void OnScroll(PointerEventData e) {
        if (!enableScroll || !pointerInside) return;
        if (AnyTyping) return; // **打字中禁滚**

        if (Time.timeAsDouble - lastScrollTime < scrollThrottle) return;
        scrollAccum += e.scrollDelta.y;

        if (scrollAccum >= scrollNotch) {
            ShowOlderOne(); scrollAccum = 0f; lastScrollTime = Time.timeAsDouble;
        } else if (scrollAccum <= -scrollNotch) {
            ShowNewerOne(); scrollAccum = 0f; lastScrollTime = Time.timeAsDouble;
        }
    }

    void ShowOlderOne() {
        if (units.Count == 0) return;
        if (focusedIdx < 0) focusedIdx = units.Count - 1; // 锁到最新
        if (focusedIdx > 0) focusedIdx--;
        ApplyFocus();
    }

    void ShowNewerOne() {
        if (units.Count == 0) return;
        if (focusedIdx < 0) return; // 已跟随最新
        if (focusedIdx < units.Count - 1) focusedIdx++;
        else focusedIdx = -1; // 回到最新
        ApplyFocus();
    }

    void ApplyFocus() {
        // 策略：聚焦的那一条显示在 History 位；其余全部隐藏。
        // 若 focusedIdx == -1，则显示最新一条在 History 位，其余隐藏。
        int idxToShow = (focusedIdx < 0) ? units.Count - 1 : focusedIdx;

        for (int i = 0; i < units.Count; i++) {
            var u = units[i];
            if (!u) continue;

            if (i == idxToShow) {
                // 聚焦：历史态
                u.SetStateHistoryInstant(u.GetComponent<RectTransform>().anchoredPosition);
                u.MoveTo(posHistory, moveDuration, moveEase);
                u.FadeTo(1f, fadeDuration, moveEase);
            } else {
                // 其余：隐藏
                u.SetStateHiddenInstant(u.GetComponent<RectTransform>().anchoredPosition);
                u.MoveTo(posHidden, moveDuration, moveEase);
                u.FadeTo(0f, fadeDuration, moveEase);
            }
        }

        ReorderLayers();
    }

    // ---------- 层级排序：当前 > 历史 > 隐藏 ----------
    void ReorderLayers() {
        // 规则：
        // - 处于 Initial（正在展示/打字）的放到最上（最后一个 sibling）
        // - 处于 History 的放中间
        // - 处于 Hidden 的放底部（最前面）
        // 注意：同组内部的相对顺序保留（旧在下/新在上）

        var hidden = new List<RecordUnitView>();
        var history = new List<RecordUnitView>();
        var initial = new List<RecordUnitView>();

        for (int i = 0; i < units.Count; i++) {
            var u = units[i];
            if (!u) continue;
            switch (u.State) {
                case RecordUnitView.ViewState.Hidden:  hidden.Add(u); break;
                case RecordUnitView.ViewState.History: history.Add(u); break;
                case RecordUnitView.ViewState.Initial: initial.Add(u); break;
            }
        }

        // 先设 Hidden（最底）
        for (int i = 0; i < hidden.Count; i++) {
            hidden[i].transform.SetSiblingIndex(i);
        }
        int baseIndex = hidden.Count;

        // 再设 History
        for (int i = 0; i < history.Count; i++) {
            history[i].transform.SetSiblingIndex(baseIndex + i);
        }
        baseIndex += history.Count;

        // 最后设 Initial（最上）
        for (int i = 0; i < initial.Count; i++) {
            initial[i].transform.SetSiblingIndex(baseIndex + i);
        }
    }
}
