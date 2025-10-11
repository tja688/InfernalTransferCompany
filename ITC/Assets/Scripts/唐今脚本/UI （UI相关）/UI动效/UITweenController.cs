// MIT License
// Jin Tang – Goal-Driven UI Tween (Bézier via pass-through point)
// + Preset binding & autosave

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UITweenController : MonoBehaviour
{
    [Header("Preset Binding")]
    [Tooltip("绑定一个 ScriptableObject 作为保存载体。")]
    public UITweenPreset boundPreset;
    [Tooltip("勾选后，每次参数改动都会自动写入 boundPreset（若已绑定）。")]
    public bool autoSaveToPreset = false;

    [Header("Playback")]
    public float duration = 0.6f;
    public float delay = 0f;
    public int loops = 0;
    public LoopType loopType = LoopType.Restart;
    public bool unscaledTime = true;

    [Header("Easing")]
    public bool useCustomCurve = false;
    public AnimationCurve customCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public Ease easeType = Ease.OutCubic;

    [Header("Target B（最终目标状态）")]
    public Vector2 targetAnchoredPosition;
    public Vector2 targetSizeDelta;
    public Vector2 targetPivot = new Vector2(0.5f, 0.5f);
    public float targetEulerZ = 0f;
    [Range(0f,1f)] public float targetAlpha = 1f;
    public Color targetColor = Color.white;

    [Header("Pass-Through C（途中必经点）")]
    public Vector2 passThroughPointC;
    [Range(0.05f, 0.95f)] public float passTStar = 0.5f;

    [Header("What to Animate")]
    public bool animatePosition = true;
    public bool animateSize = false;
    public bool animateRotationZ = false;
    public bool animateAlpha = false;
    public bool animateColor = false;

    [Header("Gizmos & Preview (Editor Only)")]
    public bool showPathGizmos = true;

    // caches
    RectTransform _rt;
    CanvasGroup _canvasGroup;
    Graphic _graphic;

    void Reset()
    {
        _rt = GetComponent<RectTransform>();
        var parent = _rt && _rt.parent ? _rt.parent as RectTransform : null;
        targetAnchoredPosition = _rt ? _rt.anchoredPosition : Vector2.zero;
        targetSizeDelta = _rt ? _rt.sizeDelta : Vector2.zero;
        targetPivot = _rt ? _rt.pivot : new Vector2(0.5f, 0.5f);
        targetEulerZ = _rt ? _rt.eulerAngles.z : 0f;

        _canvasGroup = GetComponent<CanvasGroup>();
        _graphic = GetComponent<Graphic>();
        if (_canvasGroup != null) targetAlpha = _canvasGroup.alpha;
        else if (_graphic != null) targetAlpha = _graphic.color.a;

        if (_graphic != null) targetColor = _graphic.color;

        passThroughPointC = parent ? 0.5f * ((Vector2)_rt.anchoredPosition + targetAnchoredPosition) : Vector2.zero;
        passTStar = 0.5f;
    }

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null && _graphic == null) _graphic = GetComponent<Graphic>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 自动保存到 SO（仅编辑器）
        if (autoSaveToPreset && boundPreset != null)
        {
            SaveToPreset(boundPreset, keepPresetName:true);
        }
    }
#endif

    // ---------------------- Public API ----------------------

    public Tween Play()
    {
        var seq = CreateAnimationSequence();
        seq.Play();
        return seq;
    }

    public Sequence CreateAnimationSequence()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null && _graphic == null) _graphic = GetComponent<Graphic>();

        var seq = DOTween.Sequence().SetDelay(delay).SetUpdate(unscaledTime);
        ApplyEaseTo(seq);

        Vector2 A_pos = _rt.anchoredPosition;
        Vector2 B_pos = targetAnchoredPosition;
        Vector2 C_pos = passThroughPointC;
        float tStar = Mathf.Clamp(passTStar, 0.05f, 0.95f);

        Vector2 P = SolveQuadraticControlPoint(A_pos, B_pos, C_pos, tStar);

        if (animatePosition)
        {
            Tween posTween = DOVirtual.Float(0f, 1f, duration, (t) =>
            {
                _rt.anchoredPosition = QuadBezier(A_pos, P, B_pos, t);
            });
            ApplyEaseTo(posTween);
            seq.Join(posTween);
        }

        if (animateSize)
        {
            Tween sizeTween = _rt.DOSizeDelta(targetSizeDelta, duration);
            ApplyEaseTo(sizeTween);
            seq.Join(sizeTween);
        }

        if (animateRotationZ)
        {
            Vector3 e = _rt.eulerAngles;
            Vector3 targetEuler = new Vector3(e.x, e.y, targetEulerZ);
            Tween rotTween = _rt.DORotate(targetEuler, duration, RotateMode.FastBeyond360);
            ApplyEaseTo(rotTween);
            seq.Join(rotTween);
        }

        if (animateAlpha)
        {
            if (_canvasGroup != null)
            {
                Tween a = _canvasGroup.DOFade(targetAlpha, duration);
                ApplyEaseTo(a);
                seq.Join(a);
            }
            else if (_graphic != null)
            {
                Color c = _graphic.color;
                Tween a = DOTween.To(() => c.a, v =>
                {
                    c.a = v;
                    _graphic.color = c;
                }, targetAlpha, duration);
                ApplyEaseTo(a);
                seq.Join(a);
            }
        }

        if (animateColor && _graphic != null)
        {
            Tween col = _graphic.DOColor(targetColor, duration);
            ApplyEaseTo(col);
            seq.Join(col);
        }

        if (loops != 0) seq.SetLoops(loops, loopType);
        return seq;
    }

    // ---------------------- Authoring Helpers ----------------------

    public void CaptureTargetFromCurrent()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        targetAnchoredPosition = _rt.anchoredPosition;
        targetSizeDelta = _rt.sizeDelta;
        targetPivot = _rt.pivot;
        targetEulerZ = _rt.eulerAngles.z;

        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup != null) targetAlpha = _canvasGroup.alpha;
        else
        {
            if (_graphic == null) _graphic = GetComponent<Graphic>();
            if (_graphic != null) { var c = _graphic.color; targetAlpha = c.a; targetColor = c; }
        }
    }

    public void SetPassPointFromCurrent()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        passThroughPointC = _rt.anchoredPosition;
    }

    public void SetPassPointToMidCurrentAndTarget()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        passThroughPointC = 0.5f * (_rt.anchoredPosition + targetAnchoredPosition);
    }

    // —— Preset I/O —— //
    public void SaveToPreset(UITweenPreset p, bool keepPresetName = false)
    {
        if (p == null) return;
        // Identity
        if (!keepPresetName && string.IsNullOrEmpty(p.presetName))
            p.presetName = name + "_Preset";

        // Playback
        p.duration = duration; p.delay = delay;
        p.loops = loops; p.loopType = loopType; p.unscaledTime = unscaledTime;

        // Easing
        p.useCustomCurve = useCustomCurve; p.customCurve = customCurve; p.easeType = easeType;

        // Target
        p.targetAnchoredPosition = targetAnchoredPosition;
        p.targetSizeDelta = targetSizeDelta;
        p.targetPivot = targetPivot;
        p.targetEulerZ = targetEulerZ;
        p.targetAlpha = targetAlpha;
        p.targetColor = targetColor;

        // Pass-through
        p.passThroughPointC = passThroughPointC;
        p.passTStar = passTStar;

        // Channels
        p.animatePosition = animatePosition;
        p.animateSize = animateSize;
        p.animateRotationZ = animateRotationZ;
        p.animateAlpha = animateAlpha;
        p.animateColor = animateColor;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(p);
#endif
    }

    public void LoadFromPreset(UITweenPreset p)
    {
        if (p == null) return;

        duration = p.duration; delay = p.delay; loops = p.loops;
        loopType = p.loopType; unscaledTime = p.unscaledTime;

        useCustomCurve = p.useCustomCurve; customCurve = p.customCurve; easeType = p.easeType;

        targetAnchoredPosition = p.targetAnchoredPosition;
        targetSizeDelta = p.targetSizeDelta;
        targetPivot = p.targetPivot;
        targetEulerZ = p.targetEulerZ;
        targetAlpha = p.targetAlpha;
        targetColor = p.targetColor;

        passThroughPointC = p.passThroughPointC;
        passTStar = p.passTStar;

        animatePosition = p.animatePosition;
        animateSize = p.animateSize;
        animateRotationZ = p.animateRotationZ;
        animateAlpha = p.animateAlpha;
        animateColor = p.animateColor;
    }

    // ---------------------- Math & Ease Utils ----------------------

    private void ApplyEaseTo(Tween t)
    {
        if (useCustomCurve) t.SetEase(customCurve);
        else t.SetEase(easeType);
    }

    public static Vector2 QuadBezier(in Vector2 A, in Vector2 P, in Vector2 B, float t)
    {
        float u = 1f - t;
        return u*u*A + 2f*u*t*P + t*t*B;
    }

    public static Vector2 SolveQuadraticControlPoint(in Vector2 A, in Vector2 B, in Vector2 C, float tStar)
    {
        float u = 1f - tStar;
        float denom = 2f * u * tStar;
        if (denom < 1e-6f) return 0.5f * (A + B);
        return (C - (u*u)*A - (tStar*tStar)*B) / denom;
    }

    // 给 Editor 可读
    public Vector2 TargetPos => targetAnchoredPosition;
    public Vector2 PassPointC => passThroughPointC;
    public float PassTStar => passTStar;
}
