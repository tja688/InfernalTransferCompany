// MIT License
// Goal-Driven UI Tween Player (multi-preset, name-based play)
// - 保留原有极简 API：Play(int/name/preset) 与 PlayReversed*
// - 核心修复：基线缓存（per preset per object）、移除 .From()
// - 相对模式反放稳定回“初始基线”；绝对+贝塞尔用 0↔1 参数播放，打断时重解曲线
// - 仍支持 onPlay / onComplete 事件

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

    // ===== Baseline per (preset, this RectTransform) =====
    struct Baseline {
        public Vector2 pos;
        public Vector2 size;
        public float eulerZ;
        public float? alpha;
        public Color? color;
        public Vector2 pivot;
    }
    readonly Dictionary<UITweenPreset, Baseline> _baselines = new();

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _gfx = GetComponent<Graphic>();
    }

    // ----------------- Public API（保持你原有调用习惯） -----------------

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

    public void PlayByName(string presetName)
    {
        PlayCore(FindPreset(presetName), false);
    }

    public void Play(UITweenPreset preset)
    {
        PlayCore(preset, false);
    }

    public void PlayReversed(int index)
    {
        if (index < 0 || index >= presets.Count) return;
        PlayCore(presets[index], true);
    }

    public void PlayReversedByName(string presetName)
    {
        PlayCore(FindPreset(presetName), true);
    }

    public void PlayReversed(UITweenPreset preset)
    {
        PlayCore(preset, true);
    }

    // ----------------- Internal -----------------

    private UITweenPreset FindPreset(string presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return null;

        foreach (var p in presets)
            if (p != null && p.presetName == presetName) return p;

        foreach (var lib in libraries)
        {
            if (lib != null && lib.TryGet(presetName, out var p)) return p;
        }
        Debug.LogWarning($"[UITweenPlayer] Preset not found: {presetName}", this);
        return null;
    }

    private Baseline GetOrCaptureBaseline(UITweenPreset p)
    {
        if (_baselines.TryGetValue(p, out var b)) return b;

        var e = _rt.eulerAngles;
        float? baseAlpha = null;
        Color? baseColor = null;
        if (_cg != null) baseAlpha = _cg.alpha;
        else if (_gfx != null) { baseAlpha = _gfx.color.a; baseColor = _gfx.color; }

        b = new Baseline {
            pos = _rt.anchoredPosition,
            size = _rt.sizeDelta,
            eulerZ = e.z,
            alpha = baseAlpha,
            color = baseColor,
            pivot = _rt.pivot
        };
        _baselines[p] = b;
        return b;
    }

    private void ApplyEaseFromPreset(UITweenPreset p, Tween t)
    {
        if (p.useCustomCurve && p.customCurve != null) t.SetEase(p.customCurve);
        else t.SetEase(p.easeType);
        t.SetUpdate(p.unscaledTime);
    }

    private void PlayCore(UITweenPreset preset, bool reversed)
    {
        if (preset == null || _rt == null) return;

        // 1) 结束旧动画，保留当前值（打断接管）
        Kill(false);

        // 2) 捕捉/获取基线（本次播用的“设计起点”）
        var baseL = GetOrCaptureBaseline(preset);

        // 3) 构建 Sequence
        var seq = DOTween.Sequence();

        float dur = Mathf.Max(0.0001f, preset.duration);

        // ---------- Position ----------
        if (preset.animatePosition)
        {
            if (!preset.useRelativeMode && preset.useBezierPath)
            {
                // 绝对+贝塞尔：使用 0↔1 参数播放；打断时以“当前值”为新起点重解曲线
                Vector2 A_design = baseL.pos;
                Vector2 B_design = preset.targetAnchoredPosition;
                float tStar = Mathf.Clamp(preset.passTStar, 0.05f, 0.95f);
                Vector2 M = preset.passThroughPointC;

                if (!reversed)
                {
                    // 正向：从“当前点 A'”重解到 B
                    Vector2 APrime = _rt.anchoredPosition;
                    Vector2 P = SolveQuadraticControlPoint(APrime, B_design, M, tStar);

                    var posTween = DOVirtual.Float(0f, 1f, dur, t =>
                    {
                        _rt.anchoredPosition = QuadBezier(APrime, P, B_design, t);
                    });
                    ApplyEaseFromPreset(preset, posTween);
                    seq.Join(posTween);
                }
                else
                {
                    // 反向：从“当前点 C'”回到“基线 A”；对称重解
                    Vector2 CPrime = _rt.anchoredPosition;
                    Vector2 P = SolveQuadraticControlPoint(CPrime, A_design, M, 1f - tStar);

                    var posTween = DOVirtual.Float(0f, 1f, dur, t =>
                    {
                        _rt.anchoredPosition = QuadBezier(CPrime, P, A_design, t);
                    });
                    ApplyEaseFromPreset(preset, posTween);
                    seq.Join(posTween);
                }
            }
            else
            {
                // 线性：相对/绝对统一“面向目标”，不用 .From()
                Vector2 target = preset.useRelativeMode
                    ? (reversed ? baseL.pos : baseL.pos + preset.targetAnchoredPosition)
                    : (reversed ? baseL.pos : preset.targetAnchoredPosition);

                var posTween = _rt.DOAnchorPos(target, dur);
                ApplyEaseFromPreset(preset, posTween);
                seq.Join(posTween);
            }
        }

        // ---------- Size ----------
        if (preset.animateSize)
        {
            Vector2 target = preset.useRelativeMode
                ? (reversed ? baseL.size : baseL.size + preset.targetSizeDelta)
                : (reversed ? baseL.size : preset.targetSizeDelta);

            var s = _rt.DOSizeDelta(target, dur);
            ApplyEaseFromPreset(preset, s);
            seq.Join(s);
        }

        // ---------- Rotation Z ----------
        if (preset.animateRotationZ)
        {
            float targetZ = preset.useRelativeMode
                ? (reversed ? baseL.eulerZ : baseL.eulerZ + preset.targetEulerZ)
                : (reversed ? baseL.eulerZ : preset.targetEulerZ);

            var e = _rt.eulerAngles;
            var r = _rt.DORotate(new Vector3(e.x, e.y, targetZ), dur, RotateMode.Fast);
            ApplyEaseFromPreset(preset, r);
            seq.Join(r);
        }

        // ---------- Alpha ----------
        if (preset.animateAlpha)
        {
            float? baseA = baseL.alpha;
            Tweener alphaTween = null;

            if (_cg != null)
            {
                float a0 = baseA ?? _cg.alpha;
                float aT = preset.useRelativeMode
                    ? (reversed ? a0 : a0 + preset.targetAlpha)
                    : (reversed ? a0 : preset.targetAlpha);
                alphaTween = _cg.DOFade(aT, dur);
            }
            else if (_gfx != null)
            {
                float a0 = baseA ?? _gfx.color.a;
                float aT = preset.useRelativeMode
                    ? (reversed ? a0 : a0 + preset.targetAlpha)
                    : (reversed ? a0 : preset.targetAlpha);
                alphaTween = _gfx.DOFade(aT, dur);
            }

            if (alphaTween != null)
            {
                ApplyEaseFromPreset(preset, alphaTween);
                seq.Join(alphaTween);
            }
        }

        // ---------- Color ----------
        if (preset.animateColor && _gfx != null)
        {
            Color baseC = baseL.color ?? _gfx.color;
            Color targetC = reversed ? baseC : preset.targetColor;

            var col = _gfx.DOColor(targetC, dur);
            ApplyEaseFromPreset(preset, col);
            seq.Join(col);
        }

        // 套用 preset 的延迟/循环/时间缩放等到 sequence（若你的 UITweenPreset 提供该方法）
        // 若无该方法，可自行改为：seq.SetDelay(preset.delay).SetLoops(preset.loops, preset.loopType).SetUpdate(preset.unscaledTime);
        if (preset != null)
        {
            // 兼容：若存在 ApplyEaseTo(Tween)（上一版我给过的实现）
            try { preset.ApplyEaseTo(seq); } catch { /* ignore if method not present */ }
        }

        _active = seq.OnStart(() => onPlay?.Invoke())
                     .OnComplete(() => onComplete?.Invoke())
                     .Play();
    }

    // ================= Bezier helpers =================
    static Vector2 QuadBezier(in Vector2 A, in Vector2 P, in Vector2 B, float t)
    {
        float u = 1f - t;
        return u * u * A + 2f * u * t * P + t * t * B;
    }

    // 由必经点 C 与 t* 解出二次贝塞尔控制点 P
    static Vector2 SolveQuadraticControlPoint(in Vector2 A, in Vector2 B, in Vector2 C, float tStar)
    {
        float u = 1f - tStar;
        float denom = 2f * u * tStar;           // 2(1−t)t
        if (denom < 1e-6f) return 0.5f * (A + B);  // 极端保护：退化为中点
        return (C - (u * u) * A - (tStar * tStar) * B) / denom;
    }
}
