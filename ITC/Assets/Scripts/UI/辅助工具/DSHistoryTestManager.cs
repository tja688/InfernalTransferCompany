using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using PixelCrushers.DialogueSystem;
using PrimeTween;

/// <summary>
/// 槽位式历史对话管理器（A/B/C + 升起点 D）
/// 规则：
/// - A：较新槽；新创建的条目首次进入 A 才打字机；
/// - B：次新槽；
/// - C：隐藏槽；
/// - D：升起点，任何“从隐藏进 A”都会先到 D 做准备，再 D→A 升起；
/// - 打字进行中禁止滚动；
/// - 统一移动与缓动；
/// - 层级：A 顶层 > B 次顶 > 其余（C & D）底部；
/// 特殊隐藏规则：
/// - B→C：移动过程中保持可见，抵达后再隐藏；
/// - C→B：直接显示 B（可位置移动，但不淡入）；
/// - 其它所有进入 C：立即隐藏，然后移动到 C（移动期间保持隐藏）。
/// </summary>
public class HistoryDialogueManager_SlotsWithD : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IScrollHandler {
    [Header("Prefab & Hierarchy")]
    public RecordUnitView unitPrefab;
    public RectTransform listParent;
    public RectTransform scrollArea;

    [Header("Slot Positions (anchoredPosition)")]
    public Vector2 posA = new(0,   0);   // A 槽
    public Vector2 posB = new(0, -60);   // B 槽
    public Vector2 posC = new(0,-200);   // C 槽（隐藏）
    public Vector2 posD = new(0,-120);   // D 槽（升起点）

    [Header("Unified Move Style")]
    public float moveDuration = 0.24f;
    public Ease  moveEase     = Ease.InOutSine;
    public float fadeDuration = 0.18f;

    [Header("Scroll")]
    public bool   enableScroll     = true;
    public float  scrollNotch      = 1f;
    public double scrollThrottle   = 0.12;

    [Header("Guards & Filters")]
    public bool   onlyShowNPC      = true;
    public double dedupeWindow     = 0.10;
    public bool   debugLog         = false;

    // 时间顺序：旧 -> 新
    readonly List<RecordUnitView> units = new();

    // 正在执行 A->D（随后 D->C）过渡的条目集合；这些条目在层级上应压在最顶
    readonly HashSet<RecordUnitView> exitingAToD = new();

    // 最新条目的索引（units 内）
    int topIndex = -1;

    // 浏览顶索引：-1=跟随最新；>=0 表示窗口 A 指向的条目索引
    int focusedTopIndex = -1;

    // 槽位枚举 & 记录每条所处槽位
    enum Slot { A, B, C, D }
    readonly Dictionary<RecordUnitView, Slot> currentSlots = new();

    // 滚轮
    bool   pointerInside;
    float  scrollAccum;
    double lastScrollTime;

    // 去重
    string _lastKey; double _lastKeyTime;

    bool AnyTyping {
        get {
            for (int i = 0; i < units.Count; i++) {
                var u = units[i];
                if (u && u.IsTyping) return true;
            }
            return false;
        }
    }

    void OnEnable() {
        DSGlobalMessageBridge.OnConvLine  += OnConversationLine;
        DSGlobalMessageBridge.OnConvStart += OnConversationStart;
        DSGlobalMessageBridge.OnConvEnd   += OnConversationEnd;
    }
    void OnDisable() {
        DSGlobalMessageBridge.OnConvLine  -= OnConversationLine;
        DSGlobalMessageBridge.OnConvStart -= OnConversationStart;
        DSGlobalMessageBridge.OnConvEnd   -= OnConversationEnd;
    }

    void OnConversationStart(Transform actor) {
        if (debugLog) Debug.Log("[SlotsMgr+D] ConversationStart");
    }

    void OnConversationEnd(Transform actor) {
        if (debugLog) Debug.Log("[SlotsMgr+D] ConversationEnd");
    }

    void OnConversationLine(Subtitle s) {
        if (s == null) return;

        // 过滤：只要 NPC，且非空文本，且非短时重复
        bool isPlayer = s.speakerInfo != null && s.speakerInfo.isPlayer;
        if (onlyShowNPC && isPlayer) { if (debugLog) Debug.Log("[SlotsMgr+D] Skip player line"); return; }

        string text = s.formattedText?.text ?? "";
        if (string.IsNullOrWhiteSpace(text)) { if (debugLog) Debug.Log("[SlotsMgr+D] Skip empty text"); return; }

        int conv = s.dialogueEntry?.conversationID ?? -1;
        int id   = s.dialogueEntry?.id ?? -1;
        string key = $"{conv}:{id}:{text}";
        if (_lastKey == key && (Time.timeAsDouble - _lastKeyTime) < dedupeWindow) {
            if (debugLog) Debug.Log("[SlotsMgr+D] Dup suppressed");
            return;
        }
        _lastKey = key; _lastKeyTime = Time.timeAsDouble;

        // 新条目
        var u = Instantiate(unitPrefab, listParent);
        units.Add(u);
        topIndex = units.Count - 1;

        string speaker = s.speakerInfo?.Name;
        bool followLatest = (focusedTopIndex < 0);

        if (followLatest) {
            // 新条：从 D 升到 A，并打字（位置+透明度联动）
            PrepareAtD_AsAVisualHidden(u);
            u.OnTypingFinished += OnUnitTypingFinished;

            var rt = u.GetComponent<RectTransform>();
            Vector2 fromPos = posD;
            Vector2 toPos   = posA;
            float   fromA   = 0f;
            float   toA     = 1f;

            Tween.Custom(0f, 1f, moveDuration, t => {
                rt.anchoredPosition = Vector2.Lerp(fromPos, toPos, t);
                if (u.group) u.group.alpha = Mathf.Lerp(fromA, toA, t);
            }, ease: moveEase);

            currentSlots[u] = Slot.A;

            u.BeginNewLineAtASlotAndType(speaker, text);
            // focusedTopIndex 保持 -1（跟随最新）
        } else {
            // 正在浏览历史：新条进入 C 槽（立即隐藏），固化文本，不打字以免锁滚
            PutToC_DirectHide(u, instantPosition: true);
            u.SetTextInstant(speaker, text);
        }

        ApplyWindowLayout(animated: true);
        ReorderLayers();
    }

    void OnUnitTypingFinished(RecordUnitView u) {
        if (!u) return;
        u.OnTypingFinished -= OnUnitTypingFinished;
        // 打字结束后不改变槽位语义，布局由窗口统一控制
        ReorderLayers();
    }

    // ------------------- 槽位布局（A=focusedTopIndex or topIndex，B=A-1，其余=C） -------------------
    void ApplyWindowLayout(bool animated) {
        if (units.Count == 0) return;

        int aIdx = (focusedTopIndex >= 0) ? focusedTopIndex : topIndex;
        int bIdx = aIdx - 1;

        for (int i = 0; i < units.Count; i++) {
            var item = units[i];
            if (!item) continue;

            var prev = currentSlots.ContainsKey(item) ? currentSlots[item] : Slot.C;

            if (i == aIdx) {
                // 目标 A：如果之前在 C 或 D，则 D→A（位置+透明度联动）；否则直接到 A
                if (prev == Slot.C || prev == Slot.D) {
                    if (animated) {
                        PrepareAtD_AsAVisualHidden(item);
                        var rt = item.GetComponent<RectTransform>();
                        Vector2 fromPos = posD;
                        Vector2 toPos   = posA;
                        float   fromA   = 0f;
                        float   toA     = 1f;
                        Tween.Custom(0f, 1f, moveDuration, t => {
                            rt.anchoredPosition = Vector2.Lerp(fromPos, toPos, t);
                            if (item.group) item.group.alpha = Mathf.Lerp(fromA, toA, t);
                        }, ease: moveEase);
                    } else {
                        PrepareAtD_AsAVisualHidden(item);
                        item.SetAnchoredPositionInstant(posA);
                        if (item.group) item.group.alpha = 1f;
                    }
                } else {
                    // 从 A/B 到 A：直接切 A 视觉并到位
                    item.ApplySlotAVisual();
                    if (animated) {
                        item.MoveTo(posA, moveDuration, moveEase);
                        item.FadeTo(1f, fadeDuration, moveEase);
                    } else {
                        item.SetAnchoredPositionInstant(posA);
                        if (item.group) item.group.alpha = 1f;
                    }
                }
                currentSlots[item] = Slot.A;
            }
            else if (i == bIdx && bIdx >= 0) {
                // 目标 B：特殊处理 C→B（直接显示 B），其它正常到 B
                if (prev == Slot.C) {
                    // C→B：直接显示 B，不做淡入，可带位移动画
                    item.ApplySlotBVisual();
                    if (animated) {
                        // 先立刻可见
                        if (item.group) item.group.alpha = 1f;
                        item.MoveTo(posB, moveDuration, moveEase);
                    } else {
                        item.SetAnchoredPositionInstant(posB);
                        if (item.group) item.group.alpha = 1f;
                    }
                } else {
                    // A/D/B → B：正常到 B
                    item.ApplySlotBVisual();
                    if (animated) {
                        item.MoveTo(posB, moveDuration, moveEase);
                        item.FadeTo(1f, fadeDuration, moveEase);
                    } else {
                        item.SetAnchoredPositionInstant(posB);
                        if (item.group) item.group.alpha = 1f;
                    }
                }
                currentSlots[item] = Slot.B;
            }
            else {
                // 目标 C：
                // - B→C：移动期间保持可见，抵达后再隐藏
                // - A→C：先 A→D（保持可见），到 D 后隐藏，再 D→C（隐藏移动）
                // - 其它（D/C→C 或 A/D→C 非上述情况）：立即隐藏，然后移动到 C
                if (prev == Slot.B) {
                    if (animated) {
                        MoveToC_KeepVisibleThenHide(item);
                    } else {
                        PutToC_DirectHide(item, instantPosition: true);
                    }
                } else if (prev == Slot.A) {
                    // 新增规则：A→D→C 中转
                    AtoC_viaD(item, animated);
                } else {
                    if (animated) {
                        PutToC_DirectHide(item, instantPosition: false);
                    } else {
                        PutToC_DirectHide(item, instantPosition: true);
                    }
                }
                currentSlots[item] = Slot.C;
            }

        }
    }

    // ------------------- 工具：D/C 放置与特殊过渡 -------------------
    void PrepareAtD_AsAVisualHidden(RecordUnitView u) {
        // 在 D 位准备：切成 A 的视觉，并先隐藏（alpha=0），作为 D→A 的起跳
        u.ApplySlotAVisual();
        u.SetAnchoredPositionInstant(posD);
        if (u.group) u.group.alpha = 0f;
        currentSlots[u] = Slot.D;
    }

// 规则：A→C 走中转：A(可见) → D(可见) → [在D隐藏] → C(隐藏移动)
// 过渡期间将该条目加入 exitingAToD，使其层级永远在最顶，直到抵达 D 并隐藏为止
    void AtoC_viaD(RecordUnitView u, bool animated) {
        var rt = u.GetComponent<RectTransform>();

        // 标记进入“离开 A 的过渡态”，并立即刷新层级（确保它在最顶）
        exitingAToD.Add(u);
        ReorderLayers();

        if (!animated) {
            // 非动画：直接在 D 隐藏再放到 C
            u.SetAnchoredPositionInstant(posD);
            if (u.group) u.group.alpha = 1f; // 可见到达 D
            u.ApplySlotCVisual();            // 在 D 处隐藏（alpha=0）
            u.SetAnchoredPositionInstant(posC);

            // 过渡结束，移除标记并刷新层级
            exitingAToD.Remove(u);
            ReorderLayers();
            return;
        }

        // 1) A → D：保持当前可见度移动
        Vector2 fromPos = rt.anchoredPosition; // A 当前位置
        Vector2 toPos   = posD;
        float keepA     = u.group ? u.group.alpha : 1f;

        Tween.Custom(0f, 1f, moveDuration, t => {
                rt.anchoredPosition = Vector2.Lerp(fromPos, toPos, t);
                if (u.group) u.group.alpha = keepA; // 全程保持可见
            }, ease: moveEase)
            .OnComplete(() => {
                // 2) 到达 D 后立即隐藏（切 C 视觉会把 alpha 设为 0）
                u.ApplySlotCVisual();

                // 3) D → C：隐藏状态下平移到 C（不再改变 alpha）
                Tween.Custom(0f, 1f, moveDuration * 0.6f, t => {
                        rt.anchoredPosition = Vector2.Lerp(posD, posC, t);
                    }, ease: moveEase)
                    .OnComplete(() => {
                        u.SetAnchoredPositionInstant(posC); // 归一化

                        // 过渡结束，移除标记并刷新层级
                        exitingAToD.Remove(u);
                        ReorderLayers();
                    });
            });
    }


    // 规则：B→C 移动保持可见，抵达后再隐藏
    void MoveToC_KeepVisibleThenHide(RecordUnitView u) {
        var rt = u.GetComponent<RectTransform>();
        Vector2 fromPos = rt.anchoredPosition;
        Vector2 toPos   = posC;
        float   keepA   = u.group ? u.group.alpha : 1f;

        // 移动期间维持当前 alpha
        Tween.Custom(0f, 1f, moveDuration, t => {
            rt.anchoredPosition = Vector2.Lerp(fromPos, toPos, t);
            if (u.group) u.group.alpha = keepA;
        }, ease: moveEase)
        .OnComplete(() => {
            // 抵达后再隐藏（切 C 视觉并 alpha=0）
            u.ApplySlotCVisual();
            u.SetAnchoredPositionInstant(posC);
        });
    }

    // 规则：其它进入 C 的情况——立即隐藏，然后移动到 C（移动期间保持隐藏）
    void PutToC_DirectHide(RecordUnitView u, bool instantPosition) {
        u.ApplySlotCVisual(); // 内部应将 alpha 置 0
        if (instantPosition) {
            u.SetAnchoredPositionInstant(posC);
        } else {
            // 移动期间保持隐藏（alpha 已为 0）
            var rt = u.GetComponent<RectTransform>();
            Vector2 fromPos = rt.anchoredPosition;
            Vector2 toPos   = posC;
            Tween.Custom(0f, 1f, moveDuration, t => {
                rt.anchoredPosition = Vector2.Lerp(fromPos, toPos, t);
            }, ease: moveEase);
        }
    }

    // ------------------- 滚轮 -------------------
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
        if (AnyTyping) return; // 打字中禁滚

        if (Time.timeAsDouble - lastScrollTime < scrollThrottle) return;
        scrollAccum += e.scrollDelta.y;

        if (scrollAccum >= scrollNotch) {
            ScrollOlder(); scrollAccum = 0f; lastScrollTime = Time.timeAsDouble;
        } else if (scrollAccum <= -scrollNotch) {
            ScrollNewer(); scrollAccum = 0f; lastScrollTime = Time.timeAsDouble;
        }
    }

    void ScrollOlder() {
        if (units.Count == 0) return;

        if (focusedTopIndex < 0) focusedTopIndex = topIndex; // 从最新开始浏览
        if (focusedTopIndex > 0) focusedTopIndex--;

        ApplyWindowLayout(animated: true);
        ReorderLayers();
    }

    void ScrollNewer() {
        if (units.Count == 0) return;

        if (focusedTopIndex < 0) return; // 已跟随最新
        if (focusedTopIndex < topIndex) focusedTopIndex++;
        else focusedTopIndex = -1; // 回到跟随最新

        ApplyWindowLayout(animated: true);
        ReorderLayers();
    }

// 层级：先把所有单元压底；再按顺序把“需要在上面”的提到顶。
// 顶层顺序：先 B、再 A、最后所有正在 A->D 过渡的条目（它们盖最上）
    void ReorderLayers() {
        int aIdx = (focusedTopIndex >= 0) ? focusedTopIndex : topIndex;
        int bIdx = aIdx - 1;

        // 先把所有条目压到最底，避免夹在父节点其他 UI 中间
        foreach (var u in units) {
            if (!u) continue;
            u.transform.SetSiblingIndex(0);
        }

        // 取 A、B
        Transform tb = (bIdx >= 0 && bIdx < units.Count && units[bIdx]) ? units[bIdx].transform : null;
        Transform ta = (aIdx >= 0 && aIdx < units.Count && units[aIdx]) ? units[aIdx].transform : null;

        // 依次提到顶：B -> A -> 所有“正在 A->D 过渡”的条目（保持这些在最上方）
        if (tb != null) tb.SetAsLastSibling();
        if (ta != null) ta.SetAsLastSibling();

        // 过渡中的条目可能不只一个（极端快速滚动时），全部放到最顶层
        foreach (var u in exitingAToD) {
            if (u) u.transform.SetAsLastSibling();
        }
    }

}
