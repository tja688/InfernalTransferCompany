// MIT License
// Goal-Driven UI Tween Player (multi-preset, name-based play)

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

    private UITweenPreset FindPreset(string presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return null;
        foreach (var p in presets)
            if (p != null && p.presetName == presetName) return p;
        foreach (var lib in libraries)
            if (lib != null && lib.TryGet(presetName, out var p)) return p;
        // ... (Resources.LoadAll fallback can be added here if needed)
        Debug.LogWarning($"[UITweenPlayer] Preset not found: {presetName}", this);
        return null;
    }
    
    private void PlayCore(UITweenPreset preset, bool reversed)
    {
        if (preset == null || _rt == null) return;
        Kill();

        var seq = DOTween.Sequence();
        
        if (preset.animatePosition)
        {
            Tweener posTween; // Use Tweener for .From() compatibility
            if (!preset.useRelativeMode && preset.useBezierPath)
            {
                Vector2 A = _rt.anchoredPosition;
                Vector2 B = preset.targetAnchoredPosition;
                Vector2 C = preset.passThroughPointC;
                float tStar = Mathf.Clamp(preset.passTStar, 0.05f, 0.95f);
                Vector2 P = SolveQuadraticControlPoint(A, B, C, tStar);
                
                // DOVirtual returns a Tweener
                posTween = DOVirtual.Float(0f, 1f, preset.duration, t => {
                    _rt.anchoredPosition = QuadBezier(A, P, B, t);
                });
            }
            else
            {
                Vector2 finalPos = preset.useRelativeMode ? _rt.anchoredPosition + preset.targetAnchoredPosition : preset.targetAnchoredPosition;
                posTween = _rt.DOAnchorPos(finalPos, preset.duration);
            }
            
            if (reversed) posTween.From();
            ApplyEaseFromPreset(preset, posTween);
            seq.Join(posTween);
        }

        if (preset.animateSize)
        {
            var s = _rt.DOSizeDelta(preset.useRelativeMode ? _rt.sizeDelta + preset.targetSizeDelta : preset.targetSizeDelta, preset.duration);
            if (reversed) s.From();
            ApplyEaseFromPreset(preset, s);
            seq.Join(s);
        }

        if (preset.animateRotationZ)
        {
            var e = _rt.eulerAngles;
            float finalEulerZ = preset.useRelativeMode ? e.z + preset.targetEulerZ : preset.targetEulerZ;
            var r = _rt.DORotate(new Vector3(e.x, e.y, finalEulerZ), preset.duration, RotateMode.FastBeyond360);
            if (reversed) r.From();
            ApplyEaseFromPreset(preset, r);
            seq.Join(r);
        }
        
        if (preset.animateAlpha)
        {
            // ==================== 錯誤修正點 ====================
            // 將變數類型從 Tween 改為 var 或 Tweener，確保 .From() 可以被調用
            Tweener alphaTween = null; 
            if (_cg != null) alphaTween = _cg.DOFade(preset.targetAlpha, preset.duration);
            else if (_gfx != null) alphaTween = _gfx.DOFade(preset.targetAlpha, preset.duration);
            
            if(alphaTween != null)
            {
                if(reversed) alphaTween.From();
                ApplyEaseFromPreset(preset, alphaTween);
                seq.Join(alphaTween);
            }
        }
        if (preset.animateColor && _gfx != null)
        {
            // 'var' 會自動推斷為 Tweener 類型，這是安全的
            var col = _gfx.DOColor(preset.targetColor, preset.duration);
            if(reversed) col.From();
            ApplyEaseFromPreset(preset, col);
            seq.Join(col);
        }

        preset.ApplyEaseTo(seq);
        seq.OnStart(() => onPlay?.Invoke()).OnComplete(() => onComplete?.Invoke());
        _active = seq.Play();
    }

    static Vector2 QuadBezier(in Vector2 A, in Vector2 P, in Vector2 B, float t) { float u = 1f - t; return u * u * A + 2f * u * t * P + t * t * B; }
    static Vector2 SolveQuadraticControlPoint(in Vector2 A, in Vector2 B, in Vector2 C, float tStar) { float u = 1f - tStar; float denom = 2f * u * tStar; if (denom < 1e-6f) return 0.5f * (A + B); return (C - (u * u) * A - (tStar * tStar) * B) / denom; }
    static void ApplyEaseFromPreset(UITweenPreset p, Tween t)
    {
        if (p.useCustomCurve && p.customCurve != null) t.SetEase(p.customCurve);
        else t.SetEase(p.easeType);
        t.SetUpdate(p.unscaledTime);
    }
}