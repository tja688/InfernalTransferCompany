// MIT License
// Goal-Driven UI Tween Player (multi-preset, name-based play)
// - [MODIFIED] Added Animation Lock system for priority control.
// - Features Master API (PlayMaster*) to lock and play high-priority tweens.
// - Standard Play API now respects the lock, preventing animation snatching.
// - Core logic remains: baseline caching, no .From(), stable relative reversed playback.

using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using System.Collections.Generic;
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

    struct Baseline {
        public Vector2 pos;
        public Vector2 size;
        public float eulerZ;
        public float? alpha;
        public Color? color;
        public Vector2 pivot;
    }
    readonly Dictionary<UITweenPreset, Baseline> _baselines = new();
    
    private bool _isLocked = false;

    public bool IsLocked => _isLocked;
    public void Lock() => _isLocked = true;
    public void Unlock() => _isLocked = false;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _gfx = GetComponent<Graphic>();
    }

    public void PlayMaster_Event(UITweenPreset preset) { PlayMaster(preset); }
    public void PlayMasterByName_Event(string presetName) { PlayMasterByName(presetName); }
    public void PlayMasterByIndex_Event(int index) { PlayMasterByIndex(index); }
    public Tween PlayMaster(UITweenPreset preset) { return PlayMasterCore(preset, false); }
    public Tween PlayMasterByName(string presetName) { return PlayMasterCore(FindPreset(presetName), false); }
    public Tween PlayMasterByIndex(int index)
    {
        if (index < 0 || index >= presets.Count) return null;
        return PlayMasterCore(presets[index], false);
    }

    public void Kill(bool complete = false)
    {
        if (_active != null && _active.IsActive())
        {
            _active.Kill(complete);
            _active = null;
        }
    }

    public void Play(int index)
    {
        if (index < 0 || index >= presets.Count) return;
        PlayCore(presets[index], false);
    }
    public void PlayByName(string presetName) { PlayCore(FindPreset(presetName), false); }
    public void Play(UITweenPreset preset) { PlayCore(preset, false); }
    public void PlayReversed(int index)
    {
        if (index < 0 || index >= presets.Count) return;
        PlayCore(presets[index], true);
    }
    public void PlayReversedByName(string presetName) { PlayCore(FindPreset(presetName), true); }
    public void PlayReversed(UITweenPreset preset) { PlayCore(preset, true); }
    
    
    private Tween PlayMasterCore(UITweenPreset preset, bool reversed)
    {
        Kill(false);
        Lock();

        var seq = CreateAnimationSequence(preset, reversed);
        if (seq != null)
        {
            seq.OnComplete(Unlock); 
            _active = seq.Play();
            return _active;
        }
        
        Unlock();
        return null;
    }

    private void PlayCore(UITweenPreset preset, bool reversed)
    {
        if (IsLocked) return;
        Kill(false);

        var seq = CreateAnimationSequence(preset, reversed);
        if (seq != null)
        {
            _active = seq.Play();
        }
    }

    private Sequence CreateAnimationSequence(UITweenPreset preset, bool reversed)
    {
        if (preset == null || _rt == null) return null;

        var baseL = GetOrCaptureBaseline(preset);
        var seq = DOTween.Sequence();
        float dur = Mathf.Max(0.0001f, preset.duration);

        // Position
        if (preset.animatePosition)
        {
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
            preset.ApplyTweenSettings(s); // [MODIFIED]
            seq.Join(s);
        }

        // Rotation Z
        if (preset.animateRotationZ)
        {
            float targetZ = preset.useRelativeMode
                ? (reversed ? baseL.eulerZ : baseL.eulerZ + preset.targetEulerZ)
                : (reversed ? baseL.eulerZ : preset.targetEulerZ);
            var e = _rt.eulerAngles;
            var r = _rt.DORotate(new Vector3(e.x, e.y, targetZ), dur, RotateMode.Fast);
            preset.ApplyTweenSettings(r); // [MODIFIED]
            seq.Join(r);
        }

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
                preset.ApplyTweenSettings(alphaTween); // [MODIFIED]
                seq.Join(alphaTween);
            }
        }

        // Color
        if (preset.animateColor && _gfx != null)
        {
            Color baseC = baseL.color ?? _gfx.color;
            Color targetC = reversed ? baseC : preset.targetColor;
            var col = _gfx.DOColor(targetC, dur);
            preset.ApplyTweenSettings(col); // [MODIFIED]
            seq.Join(col);
        }

        preset.ApplySequenceSettings(seq);
        
        seq.OnStart(() => onPlay?.Invoke()).OnComplete(() => onComplete?.Invoke());

        return seq;
    }

    private UITweenPreset FindPreset(string presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return null;
        foreach (var p in presets)
            if (p != null && p.presetName == presetName) return p;
        foreach (var lib in libraries)
            if (lib != null && lib.TryGet(presetName, out var p)) return p;
        Debug.LogWarning($"[UITweenPlayer] Preset not found: {presetName}", this);
        return null;
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
            eulerZ = _rt.eulerAngles.z,
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