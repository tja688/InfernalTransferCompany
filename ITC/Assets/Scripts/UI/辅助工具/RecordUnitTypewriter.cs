using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrimeTween;

public class RecordUnitView : MonoBehaviour {
    public enum ViewState { Initial, History, Hidden }

    [Header("Refs")]
    public Image bgImage;             // 背景图（Simple 模式）
    public TMP_Text label;            // 文本
    [Tooltip("整体显隐控制（自动添加）")]
    public CanvasGroup group;         // 若未赋值会自动 AddComponent

    [Header("Sprites (背景两套样式)")]
    public Sprite spriteA;            // 初始展示使用
    public Sprite spriteB;            // 历史展示使用

    [Header("Typewriter")]
    public bool   showSpeakerPrefix = false;
    public float  charactersPerSecond = 35f;
    public float  punctuationPause     = 0.12f;
    public string punctuationChars     = ".,;!?，。；！？…";

    [Header("Debug")]
    public bool debugLog = false;

    // 只读
    public bool IsTyping => _state == ViewState.Initial && _typingCoroutine != null;
    public ViewState State => _state;

    // 运行时
    RectTransform _rt;
    ViewState _state = ViewState.Hidden;
    Coroutine _typingCoroutine;
    Tween _moveTween, _fadeTween;

    public System.Action<RecordUnitView> OnTypingFinished;

    void Awake() {
        _rt = GetComponent<RectTransform>();
        if (group == null) group = gameObject.GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        // 初始：隐藏
        group.alpha = 0f;
    }

    // ============ 绑定与状态 ============

    public void BindSprites(Sprite a, Sprite b) {
        spriteA = a; spriteB = b;
    }

    public void SetTextInstant(string speaker, string content) {
        string full = showSpeakerPrefix && !string.IsNullOrEmpty(speaker) ? $"{speaker}：{content}" : (content ?? "");
        if (label) label.text = full;
    }

    public void SetStateInitialInstant(Vector2 pos) {
        StopTyping();
        _state = ViewState.Initial;
        if (_moveTween.isAlive) _moveTween.Stop();
        _rt.anchoredPosition = pos;

        if (bgImage) bgImage.sprite = spriteA;
        if (label)  label.text = "";
        group.alpha = 1f;
    }

    public void SetStateHistoryInstant(Vector2 pos) {
        StopTyping();
        _state = ViewState.History;
        if (_moveTween.isAlive) _moveTween.Stop();
        _rt.anchoredPosition = pos;

        if (bgImage) bgImage.sprite = spriteB;
        group.alpha = 1f;
    }

    public void SetStateHiddenInstant(Vector2 pos) {
        StopTyping();
        _state = ViewState.Hidden;
        if (_moveTween.isAlive) _moveTween.Stop();
        _rt.anchoredPosition = pos;
        group.alpha = 0f;
    }

    // ============ 动画接口（统一移动） ============

    public void MoveTo(Vector2 pos, float duration, Ease ease) {
        if (_moveTween.isAlive) _moveTween.Stop();
        var from = _rt.anchoredPosition;
        if ((from - pos).sqrMagnitude < 0.0001f) { _rt.anchoredPosition = pos; return; }
        _moveTween = Tween.Custom(from, pos, duration, v => _rt.anchoredPosition = v, ease: ease);
    }

    public void FadeTo(float alpha, float duration, Ease ease) {
        if (_fadeTween.isAlive) _fadeTween.Stop();
        _fadeTween = Tween.Custom(group.alpha, alpha, duration, a => group.alpha = a, ease: ease);
    }

    /// <summary>历史态时切背景：A->B 或 B->A（淡出->换图->淡入）</summary>
    public void CrossFadeSprite(Sprite target, float duration, Ease ease) {
        if (bgImage == null || target == null) return;
        // 先半程淡出
        Tween.Custom(1f, 0f, duration * 0.5f, a => SetBgAlpha(a), ease: ease)
             .OnComplete(() => {
                 bgImage.sprite = target;
                 Tween.Custom(0f, 1f, duration * 0.5f, a => SetBgAlpha(a), ease: ease);
             });
    }

    void SetBgAlpha(float a) {
        if (bgImage == null) return;
        var c = bgImage.color;
        c.a = a;
        bgImage.color = c;
    }

    // ============ 打字机 ============

    public void PlayTypewriter(string speaker, string content) {
        if (_state != ViewState.Initial) {
            if (debugLog) Debug.Log($"[Unit#{GetInstanceID()}] PlayTypewriter ignored: state={_state}");
            return;
        }
        StopTyping();
        _typingCoroutine = StartCoroutine(TypeRoutine(speaker, content));
    }

    IEnumerator TypeRoutine(string speaker, string content) {
        string full = showSpeakerPrefix && !string.IsNullOrEmpty(speaker) ? $"{speaker}：{content}" : (content ?? "");
        if (label == null) yield break;

        label.text = "";
        if (string.IsNullOrEmpty(full)) {
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
