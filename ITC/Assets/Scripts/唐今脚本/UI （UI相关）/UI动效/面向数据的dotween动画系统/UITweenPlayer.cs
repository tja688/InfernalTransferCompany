// MIT License
// Goal-Driven UI Tween Player (multi-preset, name-based play)
// [MODIFIED] Upgraded rotation system to support Vector3 (X, Y, Z) and seamless relative looping via SetRelative().
// [MODIFIED] Secondary tweens' absolute rotation now correctly supports Vector3 targets.
// [MODIFIED] Baseline struct now stores a Vector3 for eulerAngles.

using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UITweenPlayer : MonoBehaviour
{
    [Header("Sources")]
    public List<UITweenPreset> presets = new List<UITweenPreset>();
    public List<UITweenPresetLibrary> libraries = new List<UITweenPresetLibrary>();

    [Header("Events")]
    public UnityEvent onPlay;
    public UnityEvent onComplete;

    RectTransform _rt;
    CanvasGroup _cg;
    Graphic _gfx;
    Tween _active;

    public enum BaselineCaptureMode
    {
        CurrentState = 0,
        FunctionalState = 1
    }

    public event Action<UITweenPreset, bool, Sequence> SequencePrepared;

    public UITweenPreset LastPreparedPreset { get; private set; }
    public bool LastPreparedReversed { get; private set; }
    public Sequence LastPreparedSequence { get; private set; }

    struct Baseline {
        public Vector2 pos;
        public Vector2 size;
        public Vector3 eulerAngles; // MODIFIED: Was float eulerZ
        public float? alpha;
        public Color? color;
        public Vector2 pivot;
    }
    readonly Dictionary<UITweenPreset, Baseline> _baselines = new();
    Baseline _functionalBaseline;
    bool _functionalBaselineInitialized;
    
    private bool _isLocked = false;
    private string _pendingMonitorKillReason;

    public bool IsLocked => _isLocked;
    public void Lock() => _isLocked = true;
    public void Unlock() => _isLocked = false;

    private void PrepareMonitorKillReason(string reason)
    {
        _pendingMonitorKillReason = reason;
    }

    private static string DescribePlayRequest(UITweenPreset preset, bool reversed, bool master)
    {
        string name = preset != null ? preset.presetName : "<null>";
        if (master)
        {
            return reversed ? $"MasterReversed({name})" : $"MasterPlay({name})";
        }
        return reversed ? $"PlayReversed({name})" : $"Play({name})";
    }

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _gfx = GetComponent<Graphic>();
        InitializeFunctionalBaseline();
    }

    public void PlayMaster_Event(UITweenPreset preset) { PlayMaster(preset); }
    public void PlayMasterByName_Event(string presetName) { PlayMasterByName(presetName); }
    public void PlayMasterByIndex_Event(int index) { PlayMasterByIndex(index); }
    public Tween PlayMaster(UITweenPreset preset, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState) { return PlayMasterCore(preset, false, baselineMode); }
    public Tween PlayMasterByName(string presetName, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState) { return PlayMasterCore(FindPreset(presetName), false, baselineMode); }
    public Tween PlayMasterByIndex(int index, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState)
    {
        if (index < 0 || index >= presets.Count) return null;
        return PlayMasterCore(presets[index], false, baselineMode);
    }

    // --- 新增的主控反向播放方法 ---
    public void PlayMasterReversed_Event(UITweenPreset preset) { PlayMasterReversed(preset); }
    public void PlayMasterReversedByName_Event(string presetName) { PlayMasterReversedByName(presetName); }
    public void PlayMasterReversedByIndex_Event(int index) { PlayMasterReversedByIndex(index); }
    public Tween PlayMasterReversed(UITweenPreset preset, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState) { return PlayMasterCore(preset, true, baselineMode); }
    public Tween PlayMasterReversedByName(string presetName, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState) { return PlayMasterCore(FindPreset(presetName), true, baselineMode); }
    public Tween PlayMasterReversedByIndex(int index, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState)
    {
        if (index < 0 || index >= presets.Count) return null;
        return PlayMasterCore(presets[index], true, baselineMode);
    }

    public void Kill(bool complete = false)
    {
        if (_active != null && _active.IsActive())
        {
            if (string.IsNullOrEmpty(_pendingMonitorKillReason))
            {
                _pendingMonitorKillReason = complete ? "Kill (complete)" : "Kill (manual)";
            }
            _active.Kill(complete);
            _active = null;
        }
        else
        {
            _pendingMonitorKillReason = null;
        }
        LastPreparedSequence = null;
    }

    // 在 UITweenPlayer.cs 中添加这个新方法
    
    /// <summary>
    /// 优雅地停止当前动画，通过将动画的TimeScale平滑地Tween到0来实现减速效果。
    /// </summary>
    /// <param name="stopDuration">减速到完全停止所需的时间</param>
    /// <param name="stopEase">减速时使用的缓动曲线（建议使用 Out 类型，如 OutQuad）</param>
    public void StopGracefully(float stopDuration = 0.5f)
    {
        if (_active == null || !_active.IsActive())
        {
            // 动画已经不在播放
            return;
        }

        // 检查是否已经有一个 "DOTimeScale" 动画正在作用于 _active
        // (防止连续调用 StopGracefully)
        if (DOTween.IsTweening(_active))
        {
            return;
        }

        // 准备一个监控理由，以便在动画最终被 Kill 时记录
        PrepareMonitorKillReason($"StopGracefully (ease out over {stopDuration}s)");

        // 关键：获取当前动画的 unscaledTime 设置
        // 从 LastPreparedPreset 中获取，如果没有则默认使用 false（遵循 timeScale）
        bool isUnscaled = LastPreparedPreset != null ? LastPreparedPreset.unscaledTime : false;

        // 创建一个新的 Tween，它的目标是 _active 本身
        // 我们将 _active 的 TimeScale 从当前值 (通常是1) 动画到 0
        _active.DOTimeScale(0f, stopDuration)
               .SetEase(Ease.OutQuad)
               .SetUpdate(isUnscaled) // 确保这个"减速"动画本身遵循正确的 Update 模式
               .OnComplete(() =>
               {
                   // 当 timescale 降到 0 (动画已完全停止) 时，
                   // 我们再安全地 Kill 它，以完成清理。
                   // Kill 的理由已经通过 PrepareMonitorKillReason 准备好了。
                   Kill(false);
               });
    }

    public void Play(int index, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState)
    {
        if (index < 0 || index >= presets.Count) return;
        PlayCore(presets[index], false, baselineMode);
    }
    public void PlayByName(string presetName, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState) { PlayCore(FindPreset(presetName), false, baselineMode); }
    public void Play(UITweenPreset preset, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState) { PlayCore(preset, false, baselineMode); }
    public void PlayReversed(int index, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState)
    {
        if (index < 0 || index >= presets.Count) return;
        PlayCore(presets[index], true, baselineMode);
    }
    public void PlayReversedByName(string presetName, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState) { PlayCore(FindPreset(presetName), true, baselineMode); }
    public void PlayReversed(UITweenPreset preset, BaselineCaptureMode baselineMode = BaselineCaptureMode.CurrentState) { PlayCore(preset, true, baselineMode); }
    
    
    private Tween PlayMasterCore(UITweenPreset preset, bool reversed, BaselineCaptureMode baselineMode)
    {
        PrepareMonitorKillReason($"Superseded by {DescribePlayRequest(preset, reversed, true)}");
        Kill(false);
        Lock();

        var seq = CreateAnimationSequence(preset, reversed, true, baselineMode);
        if (seq != null)
        {
            _active = seq.Play();
            return _active;
        }

        Unlock();
        return null;
    }

    private void PlayCore(UITweenPreset preset, bool reversed, BaselineCaptureMode baselineMode)
    {
        if (IsLocked) return;
        PrepareMonitorKillReason($"Superseded by {DescribePlayRequest(preset, reversed, false)}");
        Kill(false);

        var seq = CreateAnimationSequence(preset, reversed, false, baselineMode);
        if (seq != null)
        {
            _active = seq.Play();
        }
    }
    
    private Baseline CaptureBaselineNow()
    {
        float? baseAlpha = null; Color? baseColor = null;
        if (_cg != null) baseAlpha = _cg.alpha;
        else if (_gfx != null) { baseAlpha = _gfx.color.a; baseColor = _gfx.color; }
        return new Baseline {
            pos = _rt.anchoredPosition,
            size = _rt.sizeDelta,
            eulerAngles = _rt.eulerAngles, // MODIFIED
            alpha = baseAlpha,
            color = baseColor,
            pivot = _rt.pivot
        };
    }

    private void InitializeFunctionalBaseline()
    {
        if (_rt == null) return;
        _functionalBaseline = CaptureBaselineNow();
        _functionalBaselineInitialized = true;
    }

    private Baseline GetFunctionalBaselineSnapshot()
    {
        if (!_functionalBaselineInitialized)
        {
            _functionalBaseline = CaptureBaselineNow();
            _functionalBaselineInitialized = true;
        }
        return _functionalBaseline;
    }

    private void SetFunctionalBaseline(Baseline baseline)
    {
        _functionalBaseline = baseline;
        _functionalBaselineInitialized = true;
    }

    private Baseline GetOrCaptureBaselineFunctional(UITweenPreset p, Baseline fallback)
    {
        if (p == null) return fallback;
        if (_baselines.TryGetValue(p, out var existing)) return existing;
        _baselines[p] = fallback;
        return fallback;
    }

    private Baseline ComputeFunctionalStateAfterPlay(Baseline baseline, UITweenPreset preset, bool reversed)
    {
        if (reversed || preset == null) return baseline;

        var result = baseline;

        if (preset.animatePosition)
        {
            result.pos = preset.useRelativeMode
                ? baseline.pos + preset.targetAnchoredPosition
                : preset.targetAnchoredPosition;
        }

        if (preset.animateSize)
        {
            result.size = preset.useRelativeMode
                ? baseline.size + preset.targetSizeDelta
                : preset.targetSizeDelta;
        }

        if (preset.animateRotation)
        {
            result.eulerAngles = preset.useRelativeMode
                ? baseline.eulerAngles + preset.targetEulerAngles
                : preset.targetEulerAngles;
        }

        if (preset.animateAlpha)
        {
            float baseAlpha = baseline.alpha
                ?? baseline.color?.a
                ?? (_cg != null ? _cg.alpha : (_gfx != null ? _gfx.color.a : 1f));
            float targetAlpha = preset.useRelativeMode ? baseAlpha + preset.targetAlpha : preset.targetAlpha;
            targetAlpha = Mathf.Clamp01(targetAlpha);
            result.alpha = targetAlpha;
            if (result.color.HasValue)
            {
                var c = result.color.Value;
                c.a = targetAlpha;
                result.color = c;
            }
        }

        if (preset.animateColor)
        {
            result.color = preset.targetColor;
            result.alpha = preset.targetColor.a;
        }

        if (preset.animatePivot)
        {
            result.pivot = preset.targetPivot;
        }

        return result;
    }

    private Sequence CreateAnimationSequence(UITweenPreset preset, bool reversed, bool master, BaselineCaptureMode baselineMode)
    {
        if (preset == null || _rt == null) return null;

        Baseline? functionalBefore = null;
        Baseline baseL;

        if (baselineMode == BaselineCaptureMode.FunctionalState)
        {
            functionalBefore = GetFunctionalBaselineSnapshot();
            if (preset.useRelativeMode && preset.relativeBaselineMode == RelativeBaselineMode.RebaseAtInterrupt)
            {
                if (reversed)
                {
                    if (_baselines.TryGetValue(preset, out var existing))
                    {
                        baseL = existing;
                    }
                    else
                    {
                        baseL = functionalBefore.Value;
                        _baselines[preset] = baseL;
                    }
                }
                else
                {
                    baseL = functionalBefore.Value;
                    _baselines[preset] = baseL;
                }
            }
            else
            {
                baseL = GetOrCaptureBaselineFunctional(preset, functionalBefore.Value);
            }
        }
        else
        {
            if (preset.useRelativeMode && preset.relativeBaselineMode == RelativeBaselineMode.RebaseAtInterrupt)
            {
                baseL = CaptureBaselineNow();
            }
            else
            {
                baseL = GetOrCaptureBaseline(preset);
            }
        }

        Baseline? functionalAfter = baselineMode == BaselineCaptureMode.FunctionalState
            ? ComputeFunctionalStateAfterPlay(baseL, preset, reversed)
            : (Baseline?)null;

        var seq = DOTween.Sequence();
        float dur = Mathf.Max(0.0001f, preset.duration);

        // Position
        if (preset.animatePosition)
        {
            // Position logic remains the same
            if (!preset.useRelativeMode && preset.useBezierPath)
            {
                Vector2 A_design = baseL.pos;
                Vector2 B_design = preset.targetAnchoredPosition;
                float tStar = Mathf.Clamp(preset.passTStar, 0.05f, 0.95f);
                Vector2 M = preset.passThroughPointC;

                if (!reversed)
                {
                    Vector2 APrime = _rt.anchoredPosition;
                    Vector2 P = SolveQuadraticControlPoint(APrime, B_design, M, tStar);
                    var posTween = DOVirtual.Float(0f, 1f, dur, t => _rt.anchoredPosition = QuadBezier(APrime, P, B_design, t));
                    preset.ApplyTweenSettings(posTween);
                    seq.Join(posTween);
                }
                else
                {
                    Vector2 CPrime = _rt.anchoredPosition;
                    Vector2 P = SolveQuadraticControlPoint(CPrime, A_design, M, 1f - tStar);
                    var posTween = DOVirtual.Float(0f, 1f, dur, t => _rt.anchoredPosition = QuadBezier(CPrime, P, A_design, t));
                    preset.ApplyTweenSettings(posTween);
                    seq.Join(posTween);
                }
            }
            else
            {
                Vector2 target = preset.useRelativeMode
                    ? (reversed ? baseL.pos : baseL.pos + preset.targetAnchoredPosition)
                    : (reversed ? baseL.pos : preset.targetAnchoredPosition);
                var posTween = _rt.DOAnchorPos(target, dur);
                preset.ApplyTweenSettings(posTween);
                seq.Join(posTween);
            }
        }

        // Size
        if (preset.animateSize)
        {
            Vector2 target = preset.useRelativeMode
                ? (reversed ? baseL.size : baseL.size + preset.targetSizeDelta)
                : (reversed ? baseL.size : preset.targetSizeDelta);
            var s = _rt.DOSizeDelta(target, dur);
            preset.ApplyTweenSettings(s);
            seq.Join(s);
        }

        // ===== ROTATION LOGIC: REBUILT =====
        if (preset.animateRotation)
        {
            Tweener rotTween;
            if (preset.useRelativeMode)
            {
                // In relative mode, the target is the CHANGE per loop. SetRelative() makes it additive.
                // This is the key for seamless looping.
                Vector3 relativeChange = reversed ? -preset.targetEulerAngles : preset.targetEulerAngles;
                rotTween = _rt.DORotate(relativeChange, dur, RotateMode.FastBeyond360).SetRelative();
            }
            else // Absolute Mode
            {
                // In absolute mode, tween TO a specific target rotation.
                Vector3 targetAngles = reversed ? baseL.eulerAngles : preset.targetEulerAngles;
                rotTween = _rt.DORotate(targetAngles, dur, RotateMode.Fast);
            }
            preset.ApplyTweenSettings(rotTween);
            seq.Join(rotTween);
        }
        // ===== END OF REBUILT ROTATION LOGIC =====


        // Alpha
        if (preset.animateAlpha)
        {
            Tweener alphaTween = null;
            if (_cg != null)
            {
                float a0 = baseL.alpha ?? _cg.alpha;
                float aT = preset.useRelativeMode ? (reversed ? a0 : a0 + preset.targetAlpha) : (reversed ? a0 : preset.targetAlpha);
                alphaTween = _cg.DOFade(aT, dur);
            }
            else if (_gfx != null)
            {
                float a0 = baseL.alpha ?? _gfx.color.a;
                float aT = preset.useRelativeMode ? (reversed ? a0 : a0 + preset.targetAlpha) : (reversed ? a0 : preset.targetAlpha);
                alphaTween = _gfx.DOFade(aT, dur);
            }
            if (alphaTween != null)
            {
                preset.ApplyTweenSettings(alphaTween);
                seq.Join(alphaTween);
            }
        }

        // Color
        if (preset.animateColor && _gfx != null)
        {
            Color baseC = baseL.color ?? _gfx.color;
            Color targetC = reversed ? baseC : preset.targetColor;
            var col = _gfx.DOColor(targetC, dur);
            preset.ApplyTweenSettings(col);
            seq.Join(col);
        }

        if (preset.secondaryTweens != null)
        {
            foreach (var secondary in preset.secondaryTweens)
            {
                var subTween = BuildSecondaryTweener(secondary);
                if (subTween == null) continue;

                float insertTime = ResolveInsertTime(secondary.startTime, dur, reversed, secondary.duration);
                seq.Insert(insertTime, subTween);
            }
        }

        if (preset.timelineEvents != null)
        {
            foreach (var timelineEvent in preset.timelineEvents)
            {
                float insertTime = ResolveInsertTime(timelineEvent.fireTime, dur, reversed, 0f);
                seq.InsertCallback(insertTime, () => ExecuteTimelineEvent(timelineEvent));
            }
        }

        preset.ApplySequenceSettings(seq);

        LastPreparedPreset = preset;
        LastPreparedReversed = reversed;
        LastPreparedSequence = seq;
        SequencePrepared?.Invoke(preset, reversed, seq);

        var context = UITweenCallContext.CaptureOrDefault(this, "UITweenPlayer", gameObject != null ? gameObject.name : null);
        context = context.WithDetails(DescribePlayRequest(preset, reversed, master), append: true);
        var monitor = UITweenMonitor.Instance;
        var requestId = monitor.Register(this, preset, reversed, seq, context);

        bool completed = false; 

        seq.OnStart(() =>
        {
            monitor.MarkStarted(requestId);
            onPlay?.Invoke();
        });

        seq.OnComplete(() =>
        {
            completed = true; 
            if (functionalAfter.HasValue)
            {
                SetFunctionalBaseline(functionalAfter.Value);
            }
            monitor.MarkCompleted(requestId);
            _pendingMonitorKillReason = null;
            if (master)
            {
                Unlock();
            }
            onComplete?.Invoke();
        });

        seq.OnKill(() =>
        {
            if (!completed)
            {
                var reason = string.IsNullOrEmpty(_pendingMonitorKillReason) ? "Killed" : _pendingMonitorKillReason;
                monitor.MarkInterrupted(requestId, reason);
                if (master)
                {
                    Unlock();
                }
            }
            _pendingMonitorKillReason = null;
        });

        return seq;
    }

    private float ResolveInsertTime(float requestedTime, float totalDuration, bool reversed, float span)
    {
        if (!reversed) return Mathf.Max(0f, requestedTime);

        float mirrored = totalDuration - requestedTime - span;
        if (float.IsNaN(mirrored) || float.IsInfinity(mirrored)) return 0f;
        return Mathf.Clamp(mirrored, 0f, Mathf.Max(totalDuration, 0f));
    }

    private Tweener BuildSecondaryTweener(SecondaryTween secondary)
    {
        if (secondary == null || secondary.duration <= 0f) return null;

        Tweener tween = null;
        switch (secondary.propertyType)
        {
            case SecondaryTweenType.Rotation:
            {
                // MODIFIED: Absolute rotation now respects the full Vector3 target
                Vector3 target = secondary.targetValue;
                var mode = secondary.isRelative ? RotateMode.FastBeyond360 : RotateMode.Fast;
                tween = _rt.DORotate(target, secondary.duration, mode);
                if (secondary.isRelative) tween.SetRelative();
                break;
            }
            case SecondaryTweenType.Scale:
            {
                Vector3 target = secondary.targetValue;
                if (!secondary.isRelative && Mathf.Approximately(target.z, 0f))
                {
                    target.z = _rt.localScale.z;
                }
                tween = _rt.DOScale(target, secondary.duration);
                if (secondary.isRelative) tween.SetRelative();
                break;
            }
            case SecondaryTweenType.AnchoredPosition:
            {
                var target = new Vector2(secondary.targetValue.x, secondary.targetValue.y);
                tween = _rt.DOAnchorPos(target, secondary.duration);
                if (secondary.isRelative) tween.SetRelative();
                break;
            }
            case SecondaryTweenType.Alpha:
            {
                float value = secondary.targetValue.x;
                if (_cg != null)
                {
                    float baseAlpha = _cg.alpha;
                    float finalAlpha = Mathf.Clamp01(secondary.isRelative ? baseAlpha + value : value);
                    tween = _cg.DOFade(finalAlpha, secondary.duration);
                }
                else if (_gfx != null)
                {
                    float baseAlpha = _gfx.color.a;
                    float finalAlpha = Mathf.Clamp01(secondary.isRelative ? baseAlpha + value : value);
                    tween = _gfx.DOFade(finalAlpha, secondary.duration);
                }
                break;
            }
            case SecondaryTweenType.Color:
            {
                if (_gfx != null)
                {
                    Color baseColor = _gfx.color;
                    Color delta = secondary.targetColor;
                    Color finalColor = secondary.isRelative ? baseColor + delta : delta;
                    finalColor.r = Mathf.Clamp01(finalColor.r);
                    finalColor.g = Mathf.Clamp01(finalColor.g);
                    finalColor.b = Mathf.Clamp01(finalColor.b);
                    finalColor.a = Mathf.Clamp01(finalColor.a);
                    tween = _gfx.DOColor(finalColor, secondary.duration);
                }
                break;
            }
        }

        if (tween != null)
        {
            tween.SetEase(secondary.easeType);
        }
        return tween;
    }

    private void ExecuteTimelineEvent(TimelineEvent timelineEvent)
    {
        if (timelineEvent == null) return;

        string sourceName = gameObject != null ? gameObject.name : name;
        using (UITweenCallContext.BeginScope(this, "TimelineEvent", sourceName, timelineEvent.name))
        {
            switch (timelineEvent.eventType)
            {
                case TimelineEventType.CustomCallback:
                    timelineEvent.customCallback?.Invoke();
                    break;
                case TimelineEventType.PlayAudio:
                    if (timelineEvent.audioClip != null)
                    {
                        var source = GetComponent<AudioSource>();
                        if (source != null)
                        {
                            source.PlayOneShot(timelineEvent.audioClip);
                        }
                        else
                        {
                            AudioSource.PlayClipAtPoint(timelineEvent.audioClip, transform.position);
                        }
                    }
                    break;
                case TimelineEventType.ChangeSprite:
                {
                    var image = timelineEvent.targetImage;
                    if (image == null) image = _gfx as Image;
                    if (image != null && timelineEvent.newSprite != null)
                    {
                        image.sprite = timelineEvent.newSprite;
                    }
                    break;
                }
                case TimelineEventType.BroadcastMessage:
                    if (!string.IsNullOrEmpty(timelineEvent.messageName))
                    {
                        if (!string.IsNullOrEmpty(timelineEvent.messageParameter))
                        {
                            BroadcastMessage(timelineEvent.messageName, timelineEvent.messageParameter, SendMessageOptions.DontRequireReceiver);
                        }
                        else
                        {
                            BroadcastMessage(timelineEvent.messageName, SendMessageOptions.DontRequireReceiver);
                        }
                    }
                    break;
            }
        }
    }

    private UITweenPreset FindPreset(string presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return null;
        foreach (var p in presets)
            if (p != null && p.presetName == presetName) return p;
        foreach (var lib in libraries)
            if (lib != null && lib.TryGet(presetName, out var p)) return p;
        return null;
    }

    public bool TryGetBaseline(UITweenPreset p, out Vector2 pos, out Vector2 size, out Vector3 eulerAngles, out float? alpha, out Color? color, out Vector2 pivot)
    {
        pos = Vector2.zero;
        size = Vector2.zero;
        eulerAngles = Vector3.zero; // MODIFIED
        alpha = null;
        color = null;
        pivot = Vector2.zero;
        if (p == null) return false;

        var baseLine = GetOrCaptureBaseline(p);
        pos = baseLine.pos;
        size = baseLine.size;
        eulerAngles = baseLine.eulerAngles; // MODIFIED
        alpha = baseLine.alpha;
        color = baseLine.color;
        pivot = baseLine.pivot;
        return true;
    }
    
    private Baseline GetOrCaptureBaseline(UITweenPreset p)
    {
        if (_baselines.TryGetValue(p, out var b)) return b;
        float? baseAlpha = null;
        Color? baseColor = null;
        if (_cg != null) baseAlpha = _cg.alpha;
        else if (_gfx != null) { baseAlpha = _gfx.color.a; baseColor = _gfx.color; }
        b = new Baseline {
            pos = _rt.anchoredPosition,
            size = _rt.sizeDelta,
            eulerAngles = _rt.eulerAngles, // MODIFIED
            alpha = baseAlpha,
            color = baseColor,
            pivot = _rt.pivot
        };
        _baselines[p] = b;
        return b;
    }
    static Vector2 QuadBezier(in Vector2 A, in Vector2 P, in Vector2 B, float t)
    {
        float u = 1f - t;
        return u * u * A + 2f * u * t * P + t * t * B;
    }
    static Vector2 SolveQuadraticControlPoint(in Vector2 A, in Vector2 B, in Vector2 C, float tStar)
    {
        float u = 1f - tStar;
        float denom = 2f * u * tStar;
        if (denom < 1e-6f) return 0.5f * (A + B);
        return (C - (u * u) * A - (tStar * tStar) * B) / denom;
    }
}
