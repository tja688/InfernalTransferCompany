using System.Collections;
using UnityEngine;
using TMPro;

public class RecordUnitTypewriter : MonoBehaviour {
    public enum State { Idle, Typing, Completed }

    [Header("Refs")]
    public TMP_Text textLabel;                         // 文字栏
    [Tooltip("是否在文本前显示说话人前缀，如 'NPC：'")]
    public bool showSpeakerPrefix = false;

    [Header("Typewriter Settings")]
    [Tooltip("每秒打印多少字符")]
    public float charactersPerSecond = 35f;
    [Tooltip("遇到标点短暂停顿（秒）")]
    public float punctuationPause = 0.12f;
    [Tooltip("触发停顿的标点列表")]
    public string punctuationChars = ".,;!?，。；！？…";

    [Header("Debug")]
    public bool debugLog = true;

    // 状态
    public State CurrentState { get; set; } = State.Idle;
    public string FinalText { get; set; } = ""; // 完成后的固化文本（不再变）
    public bool IsEmpty => textLabel == null || string.IsNullOrEmpty(textLabel.text);

    Coroutine typeCoroutine;
    public System.Action<RecordUnitTypewriter> OnTypewriterFinished;

    /// <summary>
    /// 在“空单元”上播放一次打字机；若已 Completed，将不会重播
    /// </summary>
    public void Play(string speaker, string content) {
        if (textLabel == null) return;

        // 已经完成则直接忽略（确保“只打字机一次”）
        if (CurrentState == State.Completed) {
            if (debugLog) Debug.Log($"[Typewriter#{GetInstanceID()}] Play() ignored: already Completed", this);
            return;
        }

        string full = BuildFullText(speaker, content);

        if (debugLog) {
            Debug.Log($"[Typewriter#{GetInstanceID()}] Play speaker={speaker} len={full?.Length} state={CurrentState} frame={Time.frameCount}", this);
        }

        // 如果当前正在打字，被外部重复调用，则先终止并直接显示最终文本（避免“重播”）
        if (CurrentState == State.Typing) {
            StopTyping();
            ShowInstantInternal(full);
            return;
        }

        StopTyping(); // 防守性
        typeCoroutine = StartCoroutine(TypeRoutine(full));
    }

    /// <summary>
    /// 直接瞬间显示并固化（不触发打字机）。可用于外部需要立即定格的情况
    /// </summary>
    public void ShowInstant(string speaker, string content) {
        if (textLabel == null) return;
        string full = BuildFullText(speaker, content);
        if (debugLog) Debug.Log($"[Typewriter#{GetInstanceID()}] ShowInstant len={full?.Length} frame={Time.frameCount}", this);
        StopTyping();
        ShowInstantInternal(full);
    }

    /// <summary>
    /// 手动将当前文本“固化”（不改动文字，仅把状态置为 Completed）
    /// </summary>
    public void Seal() {
        if (textLabel != null) FinalText = textLabel.text;
        CurrentState = State.Completed;
        if (debugLog) Debug.Log($"[Typewriter#{GetInstanceID()}] Seal => Completed", this);
    }

    string BuildFullText(string speaker, string content) {
        if (showSpeakerPrefix && !string.IsNullOrEmpty(speaker)) {
            return $"{speaker}：{content}";
        }
        return content ?? "";
    }

    void ShowInstantInternal(string full) {
        if (textLabel == null) return;
        textLabel.text = full;
        FinalText = full;
        CurrentState = State.Completed;
        OnTypewriterFinished?.Invoke(this);
    }

    void StopTyping() {
        if (typeCoroutine != null) {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }
        // 不清文本、不改 Completed；只停止协程
    }

    IEnumerator TypeRoutine(string fullText) {
        CurrentState = State.Typing;
        textLabel.text = "";
        if (string.IsNullOrEmpty(fullText)) {
            if (debugLog) Debug.Log($"[Typewriter#{GetInstanceID()}] Empty text -> finish immediately", this);
            FinalText = "";
            CurrentState = State.Completed;
            OnTypewriterFinished?.Invoke(this);
            yield break;
        }

        int len = fullText.Length;
        float cps = (charactersPerSecond > 0f) ? charactersPerSecond : 30f;
        float secPerChar = 1f / cps;

        for (int i = 0; i < len; i++) {
            textLabel.text = fullText.Substring(0, i + 1);

            yield return new WaitForSecondsRealtime(secPerChar);

            char ch = fullText[i];
            if (!char.IsWhiteSpace(ch) && punctuationChars.IndexOf(ch) >= 0) {
                yield return new WaitForSecondsRealtime(punctuationPause);
            }
        }

        typeCoroutine = null;
        FinalText = fullText;
        CurrentState = State.Completed;
        if (debugLog) Debug.Log($"[Typewriter#{GetInstanceID()}] Finished len={len} => Completed", this);
        OnTypewriterFinished?.Invoke(this);
    }

    void OnDisable() {
        // 停止打字机，但不清文本；状态保持现状
        StopTyping();
    }
}
