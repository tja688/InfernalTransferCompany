using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrimeTween;

public class RecordUnitView : MonoBehaviour {
    public enum ViewState { A, B, C } // A=初始槽视觉, B=历史槽视觉, C=隐藏

    [Header("Refs")]
    public Image bgImage;
    public TMP_Text label;
    public CanvasGroup group;

    [Header("Sprites")]
    public Sprite spriteA; // A 槽视觉
    public Sprite spriteB; // B 槽视觉

    [Header("Typewriter")]
    public bool   showSpeakerPrefix = false;
    public float  charactersPerSecond = 35f;
    public float  punctuationPause     = 0.12f;
    public string punctuationChars     = ".,;!?，。；！？…";

    [Header("Debug")]
    public bool debugLog = false;

    // 只读
    public bool IsTyping => _typingCoroutine != null;
    public ViewState State { get; private set; } = ViewState.C;

    RectTransform _rt;
    Coroutine _typingCoroutine;
    Tween _moveTween, _fadeTween;

    public System.Action<RecordUnitView> OnTypingFinished;

    void Awake() {
        _rt = GetComponent<RectTransform>();
        if (!group) group = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (bgImage && bgImage.sprite == null) bgImage.sprite = spriteB; // 默认随便给一张
        group.alpha = 0f; // 初始默认隐藏
    }

    // ---------- 槽位外观（不改文字） ----------
    public void ApplySlotAVisual() {
        State = ViewState.A;
        if (bgImage && spriteA) bgImage.sprite = spriteA;
        if (group) group.alpha = 1f;
    }

    public void ApplySlotBVisual() {
        State = ViewState.B;
        if (bgImage && spriteB) bgImage.sprite = spriteB;
        if (group) group.alpha = 1f;
    }

    public void ApplySlotCVisual() {
        State = ViewState.C;
        if (group) group.alpha = 0f;
    }

    // 放置位置（不涉及视觉）
    public void SetAnchoredPositionInstant(Vector2 pos) {
        _rt.anchoredPosition = pos;
    }

    // 移动/显隐
    public void MoveTo(Vector2 pos, float duration, Ease ease) {
        if (_moveTween.isAlive) _moveTween.Stop();
        var from = _rt.anchoredPosition;
        if ((from - pos).sqrMagnitude < 0.0001f) { _rt.anchoredPosition = pos; return; }
        _moveTween = Tween.Custom(from, pos, duration, v => _rt.anchoredPosition = v, ease: ease);
    }

    public void FadeTo(float alpha, float duration, Ease ease) {
        if (!group) return;
        if (_fadeTween.isAlive) _fadeTween.Stop();
        _fadeTween = Tween.Custom(group.alpha, alpha, duration, a => group.alpha = a, ease: ease);
    }

    // ---------- 文本 ----------
    public void SetTextInstant(string speaker, string content) {
        if (!label) return;
        string full = showSpeakerPrefix && !string.IsNullOrEmpty(speaker) ? $"{speaker}：{content}" : (content ?? "");
        label.text = full;
    }

    // 仅用于“新建条目并在 A 槽首次展示时”的打字机
    public void BeginNewLineAtASlotAndType(string speaker, string content) {
        StopTyping();
        ApplySlotAVisual(); // 切到 A 槽视觉（不会清文字）
        if (!label) return;
        label.text = ""; // 仅在“首次打字机”前清空
        _typingCoroutine = StartCoroutine(TypeRoutine(speaker, content));
    }

    IEnumerator TypeRoutine(string speaker, string content) {
        string full = showSpeakerPrefix && !string.IsNullOrEmpty(speaker) ? $"{speaker}：{content}" : (content ?? "");
        if (string.IsNullOrEmpty(full) || !label) {
            _typingCoroutine = null;
            OnTypingFinished?.Invoke(this);
            yield break;
        }

        float cps = charactersPerSecond > 0 ? charactersPerSecond : 30f;
        float per = 1f / cps;

        for (int i = 0; i < full.Length; i++) {
            label.text = full.Substring(0, i + 1);
            yield return new WaitForSecondsRealtime(per);
            char ch = full[i];
            if (!char.IsWhiteSpace(ch) && punctuationChars.IndexOf(ch) >= 0) {
                yield return new WaitForSecondsRealtime(punctuationPause);
            }
        }
        _typingCoroutine = null;
        OnTypingFinished?.Invoke(this);
    }

    public void StopTyping() {
        if (_typingCoroutine != null) {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
    }
}
