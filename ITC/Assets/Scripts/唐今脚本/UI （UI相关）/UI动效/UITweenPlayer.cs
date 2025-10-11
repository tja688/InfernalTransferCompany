// MIT License
// Goal-Driven UI Tween Player (multi-preset, name-based play)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UITweenPlayer : MonoBehaviour
{
    [Header("Sources")]
    [Tooltip("本对象直连的 Preset 列表（就近优先）。")]
    public List<UITweenPreset> presets = new List<UITweenPreset>();

    [Tooltip("额外的库（可一个或多个），按名字检索补充。")]
    public List<UITweenPresetLibrary> libraries = new List<UITweenPresetLibrary>();

    [Header("Events")]
    public UnityEvent onPlay;
    public UnityEvent onComplete;

    RectTransform _rt;
    CanvasGroup _cg;
    Graphic _gfx;
    Tween _active;   // 当前激活的序列

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _gfx = GetComponent<Graphic>();
    }

    public void Kill(bool complete = false)
    {
        if (_active != null && _active.IsActive())
        {
            _active.Kill(complete);
            _active = null;
        }
    }

    // —— 对外 API —— //

    public void Play(int index)
    {
        if (index < 0 || index >= presets.Count || presets[index] == null) return;
        Play(presets[index]);
    }

    public void PlayByName(string presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return;

        // 本地列表优先
        foreach (var p in presets)
        {
            if (p != null && p.presetName == presetName) { Play(p); return; }
        }

        // 库检索
        foreach (var lib in libraries)
        {
            if (lib != null && lib.TryGet(presetName, out var p)) { Play(p); return; }
        }

        // 兜底：尝试 Resources 全局扫描一次（可选，避免频繁调用）
        var all = Resources.LoadAll<UITweenPreset>("");
        foreach (var p in all)
        {
            if (p != null && p.presetName == presetName) { Play(p); return; }
        }

        Debug.LogWarning($"[UITweenPlayer] Preset not found: {presetName}", this);
    }

    public void Play(UITweenPreset preset)
    {
        if (preset == null || _rt == null) return;

        Kill(); // 先中止现有

        // —— 起点 A：当前即时状态 —— //
        Vector2 A = _rt.anchoredPosition;
        Vector2 B = preset.targetAnchoredPosition;
        Vector2 C = preset.passThroughPointC;
        float tStar = Mathf.Clamp(preset.passTStar, 0.05f, 0.95f);

        Vector2 P = SolveQuadraticControlPoint(A, B, C, tStar);

        var seq = DOTween.Sequence();

        // 位置
        if (preset.animatePosition)
        {
            var pos = DOVirtual.Float(0f, 1f, preset.duration, t =>
            {
                _rt.anchoredPosition = QuadBezier(A, P, B, t);
            });
            ApplyEaseFromPreset(preset, pos);
            seq.Join(pos);
        }

        // 尺寸
        if (preset.animateSize)
        {
            var s = _rt.DOSizeDelta(preset.targetSizeDelta, preset.duration);
            ApplyEaseFromPreset(preset, s); seq.Join(s);
        }

        // 旋转 Z
        if (preset.animateRotationZ)
        {
            var e = _rt.eulerAngles;
            var r = _rt.DORotate(new Vector3(e.x, e.y, preset.targetEulerZ), preset.duration, RotateMode.FastBeyond360);
            ApplyEaseFromPreset(preset, r); seq.Join(r);
        }

        // 透明度
        if (preset.animateAlpha)
        {
            if (_cg != null)
            {
                var a = _cg.DOFade(preset.targetAlpha, preset.duration);
                ApplyEaseFromPreset(preset, a); seq.Join(a);
            }
            else if (_gfx != null)
            {
                Color c = _gfx.color;
                var a = DOTween.To(() => c.a, v => { c.a = v; _gfx.color = c; }, preset.targetAlpha, preset.duration);
                ApplyEaseFromPreset(preset, a); seq.Join(a);
            }
        }

        // 颜色
        if (preset.animateColor && _gfx != null)
        {
            var col = _gfx.DOColor(preset.targetColor, preset.duration);
            ApplyEaseFromPreset(preset, col); seq.Join(col);
        }

        // 应用公共参数（loops/delay/unscaled）
        ApplyEaseFromPreset(preset, seq);

        // 事件
        seq.OnStart(() => onPlay?.Invoke())
           .OnComplete(() => onComplete?.Invoke());

        _active = seq.Play();
    }

    public void PlayAll()
    {
        foreach (var p in presets) if (p != null) Play(p);
    }

    // —— Helpers —— //

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
    static void ApplyEaseFromPreset(UITweenPreset p, Tween t)
    {
        if (p.useCustomCurve && p.customCurve != null) t.SetEase(p.customCurve);
        else t.SetEase(p.easeType);
        t.SetUpdate(p.unscaledTime);
    }
}
