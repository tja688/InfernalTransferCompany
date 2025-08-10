using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using TMPro;

public class DSHistoryTestManager : MonoBehaviour {
    [Header("Record Unit Prefab & Parent")]
    public RecordUnitTypewriter recordUnitPrefab;
    public RectTransform listParent;

    [Header("Layout")]
    public Vector2 positionA = Vector2.zero;
    public float lineStep = 50f;

    [Header("Debug")]
    [SerializeField] bool debugLog = true;
    public double dedupeWindow = 0.10;

    readonly List<RecordUnitTypewriter> units = new(); // 从旧到新
    string _lastLineKey;
    double _lastLineTime;
    int _lastCreateFrame;
    int _createdThisFrame;

    void OnEnable() {
        if (debugLog) Debug.Log($"[Manager#{GetInstanceID()}] OnEnable subscribe", this);
        DSGlobalMessageBridge.OnConvStart += OnConversationStart;
        DSGlobalMessageBridge.OnConvLine  += OnConversationLine;
        DSGlobalMessageBridge.OnConvEnd   += OnConversationEnd;
    }

    void OnDisable() {
        if (debugLog) Debug.Log($"[Manager#{GetInstanceID()}] OnDisable unsubscribe", this);
        DSGlobalMessageBridge.OnConvStart -= OnConversationStart;
        DSGlobalMessageBridge.OnConvLine  -= OnConversationLine;
        DSGlobalMessageBridge.OnConvEnd   -= OnConversationEnd;
    }

    public void OnConversationStart(Transform actor) {
        if (debugLog) Debug.Log($"[Manager#{GetInstanceID()}] OnConversationStart from={actor?.name} frame={Time.frameCount}", this);
        // 不在这里创建；统一在 OnConversationLine 处理
    }

    public void OnConversationLine(Subtitle s) {
        if (s == null) return;
        
        bool isPlayer = s.speakerInfo != null && s.speakerInfo.isPlayer;
        if (isPlayer) {
            if (debugLog) Debug.Log($"[Manager#{GetInstanceID()}] Skip player line from {s.speakerInfo.Name}", this);
            return;
        }


        int convId  = s.dialogueEntry?.conversationID ?? -1;
        int entryId = s.dialogueEntry?.id ?? -1;
        string speaker = s.speakerInfo?.Name;
        string text = s.formattedText?.text ?? string.Empty;

        // 过滤空文本（玩家空行等）
        if (string.IsNullOrWhiteSpace(text)) {
            if (debugLog) Debug.Log($"[Manager#{GetInstanceID()}] IGNORE empty text conv={convId} entry={entryId} speaker={speaker} frame={Time.frameCount}", this);
            return;
        }

        // 短时去重
        string key = $"{convId}:{entryId}:{text}";
        if ((_lastLineKey == key) && (Time.timeAsDouble - _lastLineTime) < dedupeWindow) {
            if (debugLog) Debug.Log($"[Manager#{GetInstanceID()}] DUP suppressed key={key}", this);
            return;
        }
        _lastLineKey = key;
        _lastLineTime = Time.timeAsDouble;

        if (debugLog) Debug.Log($"[Manager#{GetInstanceID()}] APPLY key={key} speaker={speaker} frame={Time.frameCount}", this);

        // —— 只在“空单元(Idle 且文本为空)”上触发打字机 ——
        if (units.Count == 0) {
            var u0 = CreateUnitAtAEmpty();
            if (u0 != null) {
                u0.OnTypewriterFinished -= OnUnitFinished;
                u0.OnTypewriterFinished += OnUnitFinished;
                u0.Play(speaker, text);
            }
            return;
        }

        var last = units[^1];
        if (last != null && last.IsEmpty && last.CurrentState != RecordUnitTypewriter.State.Completed) {
            if (debugLog) Debug.Log($"[Manager#{GetInstanceID()}] Fill last EMPTY unit with typewriter", this);
            last.OnTypewriterFinished -= OnUnitFinished;
            last.OnTypewriterFinished += OnUnitFinished;
            last.Play(speaker, text);
            return;
        }

        // 上一条已经固化完成：上移所有，再创建新的空位播放
        ShiftAllBy(lineStep);
        var nu = CreateUnitAtAEmpty();
        if (nu != null) {
            nu.OnTypewriterFinished -= OnUnitFinished;
            nu.OnTypewriterFinished += OnUnitFinished;
            nu.Play(speaker, text);
        }
    }

    public void OnConversationEnd(Transform actor) {
        if (debugLog) Debug.Log($"[Manager#{GetInstanceID()}] OnConversationEnd from={actor?.name} frame={Time.frameCount}", this);
    }

    RecordUnitTypewriter CreateUnitAtAEmpty() {
        if (recordUnitPrefab == null || listParent == null) {
            Debug.LogWarning("[Manager] Prefab/Parent 未设置。", this);
            return null;
        }

        if (_lastCreateFrame != Time.frameCount) {
            _lastCreateFrame = Time.frameCount;
            _createdThisFrame = 0;
        }
        _createdThisFrame++;

        var inst = Instantiate(recordUnitPrefab, listParent);
        var rt = inst.GetComponent<RectTransform>();
        rt.anchoredPosition = positionA;

        // 关键修复：不要 Seal、不要强行改状态。
        // 新建出来就是 Idle + 空文本，让 OnConversationLine 来触发 Play。
        if (inst.textLabel) inst.textLabel.text = "";

        units.Add(inst);

        if (debugLog) {
            Debug.Log($"[Manager#{GetInstanceID()}] CreateUnitAtAEmpty frame={Time.frameCount} createdThisFrame={_createdThisFrame} totalUnits={units.Count}", this);
        }
        return inst;
    }

    void OnUnitFinished(RecordUnitTypewriter unit) {
        // 完成后固化（语义化；Typewriter 已置 Completed，这里再次 Seal 不改变文本）
        if (unit != null) {
            unit.Seal();
            if (debugLog) {
                var preview = unit.textLabel != null ? unit.textLabel.text : "(null)";
                if (preview.Length > 30) preview = preview.Substring(0, 30) + "...";
                Debug.Log($"[Manager#{GetInstanceID()}] TypewriterFinished & Sealed text=\"{preview}\"", this);
            }
        }
    }

    void ShiftAllBy(float deltaY) {
        for (int i = 0; i < units.Count; i++) {
            var rt = units[i]?.GetComponent<RectTransform>();
            if (rt == null) continue;
            var p = rt.anchoredPosition;
            p.y += deltaY;
            rt.anchoredPosition = p;
        }
        if (debugLog) Debug.Log($"[Manager#{GetInstanceID()}] ShiftAllBy {deltaY} units={units.Count}", this);
    }
}
