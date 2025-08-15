// EntranceMotion.cs  (PrimeTween + 可选透明渐变 + Gizmos + 调试开关)
using UnityEngine;
using UnityEngine.Events;
using PrimeTween;
using System.Collections;

[DisallowMultipleComponent]
public class EntranceMotion : MonoBehaviour {
    [Header("Space")]
    [Tooltip("使用本地坐标（true）或世界坐标（false）")]
    public bool useLocalPosition = true;

    [Header("Positions")]
    [Tooltip("入场前的起始位置")]
    public Vector3 startPosition = new Vector3(0f, 5.4f, 0f);
    [Tooltip("入场后的停留位置")]
    public Vector3 endPosition   = new Vector3(0f, -5.4f, 0f);

    [Header("In (Show)")]
    [Tooltip("入场时长")]
    public float inDuration = 0.6f;
    [Tooltip("入场动效预设")]
    public EasePreset inEasePreset = EasePreset.FastInSlowOut;
    [Tooltip("入场延迟（秒）")]
    public float inDelay = 0f;

    [Header("Out (Hide)")]
    [Tooltip("退场时长")]
    public float outDuration = 0.6f;
    [Tooltip("退场动效预设")]
    public EasePreset outEasePreset = EasePreset.FastInSlowOut;
    [Tooltip("退场延迟（秒）")]
    public float outDelay = 0f;

    [Header("Fade (Optional)")]
    [Tooltip("启用渐入/渐隐（自动获取本物体的 SpriteRenderer）")]
    public bool enableFade = false;
    [Tooltip("目标位置处的透明度（0~1），起始位置永远为 0")]
    [Range(0f, 1f)] public float targetAlpha = 1f;
    [Tooltip("透明度变化曲线（0→1），默认线性")]
    public AnimationCurve fadeCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("手动指定 SpriteRenderer；留空则自动 GetComponent<SpriteRenderer>()")]
    public SpriteRenderer spriteRenderer;

    [Header("Timing")]
    [Tooltip("延迟/渐变是否使用不受 timeScale 影响的时钟")]
    public bool useUnscaledTimeForDelayAndFade = true;

    [Header("Events")]
    public UnityEvent onShowStarted;
    public UnityEvent onShown;
    public UnityEvent onHideStarted;
    public UnityEvent onHidden;

    [Header("Debug Toggle (Play Mode)")]
    [Tooltip("播放时勾上自动入场；取消勾选自动退场（仅 Play 模式触发）")]
    public bool debugToggle = false;

    [Header("Gizmos (Scene View Only)")]
    public bool showGizmos = false;
    public Color gizmoLineColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    public Color gizmoStartColor = new Color(0.2f, 1f, 0.2f, 0.9f);
    public Color gizmoEndColor   = new Color(0.2f, 0.9f, 1f, 0.9f);
    public float gizmoSphereRadius = 0.12f;

    public bool IsShown { get; private set; }
    public bool IsAnimating { get; private set; }

    Tween moveTween;
    Coroutine delayRoutine;
    Coroutine fadeRoutine;
    bool lastDebugToggle;

    void Awake() {
        if (enableFade && !spriteRenderer) {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (!spriteRenderer) {
                Debug.LogWarning("[EntranceMotion] enableFade 已开启，但未找到 SpriteRenderer。");
            }
        }
        SnapToStart(); // 等价于原来的 snapToStartOnAwake
        lastDebugToggle = debugToggle;
    }

    void Start() {
        // 原 playInOnStart 删除，改为 Inspector 切换 debugToggle 触发
    }

    void OnDisable() {
        moveTween.Stop();
        Tween.StopAll(transform);
        CancelDelay();
        StopFade();
        IsAnimating = false;
    }

    // ---------- Inspector 勾选时触发（仅 Play 模式） ----------
    void OnValidate() {
        if (Application.isPlaying && lastDebugToggle != debugToggle) {
            lastDebugToggle = debugToggle;
            if (debugToggle) Show();
            else            Hide();
        }
    }

    // ---------- 外部 API ----------
    public Tween Show() {
        CancelDelay();
        moveTween.Stop();
        Tween.StopAll(transform);

        onShowStarted?.Invoke();
        IsAnimating = true;

        if (enableFade) BeginFade(0f, targetAlpha, inDuration);

        if (inDelay > 0f) {
            delayRoutine = StartCoroutine(Co_Delayed(() => StartShowTween(), inDelay));
            return default;
        } else {
            return StartShowTween();
        }
    }

    public Tween Hide() {
        CancelDelay();
        moveTween.Stop();
        Tween.StopAll(transform);

        onHideStarted?.Invoke();
        IsAnimating = true;

        if (enableFade) BeginFade(targetAlpha, 0f, outDuration);

        if (outDelay > 0f) {
            delayRoutine = StartCoroutine(Co_Delayed(() => StartHideTween(), outDelay));
            return default;
        } else {
            return StartHideTween();
        }
    }

    public void Toggle() {
        if (IsShown) Hide(); else Show();
    }

    public void Configure(Vector3 start, Vector3 end, bool localSpace) {
        startPosition = start;
        endPosition = end;
        useLocalPosition = localSpace;
    }

    // ---------- 内部：开启位置 Tween ----------
    Tween StartShowTween() {
        var ease = ToPrimeEase(inEasePreset);
        if (useLocalPosition) {
            var z = transform.localPosition.z;
            Vector3 target = new Vector3(endPosition.x, endPosition.y, endPosition.z != 0 ? endPosition.z : z);
            moveTween = Tween.LocalPosition(transform, target, inDuration, ease)
                .OnComplete(() => { IsAnimating = false; IsShown = true; onShown?.Invoke(); });
        } else {
            var z = transform.position.z;
            Vector3 target = new Vector3(endPosition.x, endPosition.y, endPosition.z != 0 ? endPosition.z : z);
            moveTween = Tween.Position(transform, target, inDuration, ease)
                .OnComplete(() => { IsAnimating = false; IsShown = true; onShown?.Invoke(); });
        }
        return moveTween;
    }

    Tween StartHideTween() {
        var ease = ToPrimeEase(outEasePreset);
        if (useLocalPosition) {
            var z = transform.localPosition.z;
            Vector3 target = new Vector3(startPosition.x, startPosition.y, startPosition.z != 0 ? startPosition.z : z);
            moveTween = Tween.LocalPosition(transform, target, outDuration, ease)
                .OnComplete(() => { IsAnimating = false; IsShown = false; onHidden?.Invoke(); });
        } else {
            var z = transform.position.z;
            Vector3 target = new Vector3(startPosition.x, startPosition.y, startPosition.z != 0 ? startPosition.z : z);
            moveTween = Tween.Position(transform, target, outDuration, ease)
                .OnComplete(() => { IsAnimating = false; IsShown = false; onHidden?.Invoke(); });
        }
        return moveTween;
    }

    IEnumerator Co_Delayed(System.Action action, float delay) {
        if (useUnscaledTimeForDelayAndFade) yield return new WaitForSecondsRealtime(delay);
        else                                yield return new WaitForSeconds(delay);
        action?.Invoke();
        delayRoutine = null;
    }

    void CancelDelay() {
        if (delayRoutine != null) { StopCoroutine(delayRoutine); delayRoutine = null; }
    }

    // ---------- 渐入/渐隐 ----------
    void BeginFade(float from, float to, float duration) {
        if (!enableFade || !spriteRenderer) return;
        StopFade();
        fadeRoutine = StartCoroutine(Co_Fade(from, to, duration));
    }

    IEnumerator Co_Fade(float from, float to, float duration) {
        SetAlpha(from);
        float t = 0f;
        while (t < duration) {
            t += useUnscaledTimeForDelayAndFade ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = fadeCurve != null ? fadeCurve.Evaluate(k) : k;
            SetAlpha(Mathf.LerpUnclamped(from, to, s));
            yield return null;
        }
        SetAlpha(to);
        fadeRoutine = null;
    }

    void StopFade() {
        if (fadeRoutine != null) {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    void SetAlpha(float a) {
        if (!spriteRenderer) return;
        var c = spriteRenderer.color;
        c.a = a;
        spriteRenderer.color = c;
    }

    // ---------- Inspector 辅助 ----------
    [ContextMenu("Snap To Start")]
    public void SnapToStart() {
        if (useLocalPosition) {
            var z = transform.localPosition.z;
            transform.localPosition = new Vector3(startPosition.x, startPosition.y, startPosition.z != 0 ? startPosition.z : z);
        } else {
            var z = transform.position.z;
            transform.position = new Vector3(startPosition.x, startPosition.y, startPosition.z != 0 ? startPosition.z : z);
        }
        if (enableFade) SetAlpha(0f);
        IsShown = false;
    }

    [ContextMenu("Snap To End")]
    public void SnapToEnd() {
        if (useLocalPosition) {
            var z = transform.localPosition.z;
            transform.localPosition = new Vector3(endPosition.x, endPosition.y, endPosition.z != 0 ? endPosition.z : z);
        } else {
            var z = transform.position.z;
            transform.position = new Vector3(endPosition.x, endPosition.y, endPosition.z != 0 ? endPosition.z : z);
        }
        if (enableFade) SetAlpha(targetAlpha);
        IsShown = true;
    }

    // ---------- 动效预设 ----------
    public enum EasePreset {
        Linear,
        FastInSlowOut,   // OutCubic
        SlowInFastOut,   // InCubic
        Smooth,          // InOutCubic
        Snappy,          // OutQuint
        Overshoot,       // OutBack
        Glide,           // OutExpo
        Springy          // OutElastic
    }
    static Ease ToPrimeEase(EasePreset p) {
        switch (p) {
            case EasePreset.Linear:        return Ease.Linear;
            case EasePreset.FastInSlowOut: return Ease.OutCubic;
            case EasePreset.SlowInFastOut: return Ease.InCubic;
            case EasePreset.Smooth:        return Ease.InOutCubic;
            case EasePreset.Snappy:        return Ease.OutQuint;
            case EasePreset.Overshoot:     return Ease.OutBack;
            case EasePreset.Glide:         return Ease.OutExpo;
            case EasePreset.Springy:       return Ease.OutElastic;
            default:                       return Ease.OutCubic;
        }
    }

    // ---------- Gizmos ----------
    void OnDrawGizmos() {
        if (!showGizmos) return;

        // 目标点（根据空间类型转成世界坐标）
        Vector3 startW = useLocalPosition ? TransformPointRelative(startPosition) : startPosition;
        Vector3 endW   = useLocalPosition ? TransformPointRelative(endPosition)   : endPosition;

        // 当前→目标的连线
        Gizmos.color = gizmoLineColor;
        Gizmos.DrawLine(transform.position, endW);

        // 起点/终点标记
        Gizmos.color = gizmoStartColor; Gizmos.DrawSphere(startW, gizmoSphereRadius);
        Gizmos.color = gizmoEndColor;   Gizmos.DrawSphere(endW,   gizmoSphereRadius);
    }

    Vector3 TransformPointRelative(Vector3 pLocal) {
        var parent = transform.parent;
        return parent ? parent.TransformPoint(pLocal) : pLocal;
    }
}
