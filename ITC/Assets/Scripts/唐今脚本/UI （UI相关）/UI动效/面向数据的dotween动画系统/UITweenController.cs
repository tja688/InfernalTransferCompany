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
    public UITweenPreset boundPreset;
    public bool autoSaveToPreset = false;
    
    [Header("Mode")]
    [Tooltip("勾選後，位置、尺寸、旋轉將作為基於初始狀態的【偏移量】，而非絕對目標值。此模式下為直線運動。")]
    public bool useRelativeMode = false;
    [Tooltip("僅在【絕對模式】下生效。勾選後，將啟用二次貝塞爾曲線路徑，可通過“途中必經點”進行調節。")]
    public bool useBezierPath = false;

    [Header("Playback")]
    public float duration = 1f;
    public float delay = 0f;
    public int loops = 0;
    public LoopType loopType = LoopType.Restart;
    public bool unscaledTime = true;

    [Header("Easing")]
    public bool useCustomCurve = false;
    public AnimationCurve customCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public Ease easeType = Ease.OutCubic;

    [Header("Target B（最終目標狀態）")]
    public Vector2 targetAnchoredPosition;
    public Vector2 targetSizeDelta;
    public Vector2 targetPivot = new Vector2(0.5f, 0.5f);
    public float targetEulerZ = 0f;
    [Range(0f,1f)] public float targetAlpha = 1f;
    public Color targetColor = Color.white;

    [Header("Pass-Through C（途中必經點）")]
    public Vector2 passThroughPointC;
    [Range(0.05f, 0.95f)] public float passTStar = 0.5f;

    [Header("What to Animate")]
    public bool animatePosition = true;
    public bool animateSize = true;
    public bool animateRotationZ = false;
    public bool animateAlpha = false;
    public bool animateColor = false;

    [Header("Gizmos & Preview (Editor Only)")]
    public bool showPathGizmos = true;

    RectTransform _rt;
    CanvasGroup _canvasGroup;
    Graphic _graphic;

    
    void Reset()
    {
        _rt = GetComponent<RectTransform>();
        targetAnchoredPosition = _rt ? _rt.anchoredPosition : Vector2.zero;
        targetSizeDelta = _rt ? _rt.sizeDelta : new Vector2(100, 100);
        targetPivot = _rt ? _rt.pivot : new Vector2(0.5f, 0.5f);
        
        animatePosition = true;
        animateSize = true;
        animateRotationZ = false;
        animateAlpha = false;
        animateColor = false;
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
        if (autoSaveToPreset && boundPreset != null)
        {
            SaveToPreset(boundPreset, keepPresetName:true);
        }
    }
#endif

    public Tween Play() => CreateAnimationSequence(false)?.Play();
    public Tween PlayReversed() => CreateAnimationSequence(true)?.Play();

    public Sequence CreateAnimationSequence(bool reversed = false)
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null && _graphic == null) _graphic = GetComponent<Graphic>();

        var seq = DOTween.Sequence().SetDelay(delay).SetUpdate(unscaledTime);
        // 注意：這裡的ApplyEaseTo是對整個序列設置，而單個tween的ease會在下面單獨設置
        // preset.ApplyEaseTo(seq) 的邏輯是正確的，因為它包含了 loops/delay
        
        if (animatePosition)
        {
            Tweener posTween;
            if (!useRelativeMode && useBezierPath)
            {
                Vector2 A_pos = _rt.anchoredPosition;
                Vector2 B_pos = targetAnchoredPosition;
                Vector2 C_pos = passThroughPointC;
                float tStar = Mathf.Clamp(passTStar, 0.05f, 0.95f);
                Vector2 P = SolveQuadraticControlPoint(A_pos, B_pos, C_pos, tStar);
                posTween = DOVirtual.Float(0f, 1f, duration, (t) => { _rt.anchoredPosition = QuadBezier(A_pos, P, B_pos, t); });
            }
            else
            {
                Vector2 finalPos = useRelativeMode ? _rt.anchoredPosition + targetAnchoredPosition : targetAnchoredPosition;
                posTween = _rt.DOAnchorPos(finalPos, duration);
            }
            if (reversed) posTween.From();
            ApplyEaseTo(posTween);
            seq.Join(posTween);
        }
        
        if (animateSize)
        {
            Vector2 finalSize = useRelativeMode ? _rt.sizeDelta + targetSizeDelta : targetSizeDelta;
            var sizeTween = _rt.DOSizeDelta(finalSize, duration);
            if (reversed) sizeTween.From();
            ApplyEaseTo(sizeTween);
            seq.Join(sizeTween);
        }

        if (animateRotationZ)
        {
            Vector3 e = _rt.eulerAngles;
            float finalEulerZ = useRelativeMode ? e.z + targetEulerZ : targetEulerZ;
            var rotTween = _rt.DORotate(new Vector3(e.x, e.y, finalEulerZ), duration, RotateMode.FastBeyond360);
            if (reversed) rotTween.From();
            ApplyEaseTo(rotTween);
            seq.Join(rotTween);
        }

        if (animateAlpha)
        {
            // ==================== 同步修正 ====================
            Tweener alphaTween = null;
            if (_canvasGroup != null) alphaTween = _canvasGroup.DOFade(targetAlpha, duration);
            else if (_graphic != null) alphaTween = _graphic.DOFade(targetAlpha, duration);
            
            if(alphaTween != null)
            {
                if(reversed) alphaTween.From();
                ApplyEaseTo(alphaTween);
                seq.Join(alphaTween);
            }
        }
        if (animateColor && _graphic != null)
        {
            var colTween = _graphic.DOColor(targetColor, duration);
            if(reversed) colTween.From();
            ApplyEaseTo(colTween);
            seq.Join(colTween);
        }

        if (loops != 0) seq.SetLoops(loops, loopType);
        return seq;
    }

    // ==================== 修正區域：將缺失的輔助方法加回來 ====================
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
    // ========================================================================

    public void SaveToPreset(UITweenPreset p, bool keepPresetName = false)
    {
        if (p == null) return;
        if (!keepPresetName && string.IsNullOrEmpty(p.presetName)) p.presetName = name + "_Preset";

        p.useRelativeMode = useRelativeMode;
        p.useBezierPath = useBezierPath;
        
        p.duration = duration; p.delay = delay; p.loops = loops; p.loopType = loopType; p.unscaledTime = unscaledTime;
        p.useCustomCurve = useCustomCurve; p.customCurve = customCurve; p.easeType = easeType;
        p.targetAnchoredPosition = targetAnchoredPosition; p.targetSizeDelta = targetSizeDelta; p.targetPivot = targetPivot; p.targetEulerZ = targetEulerZ; p.targetAlpha = targetAlpha; p.targetColor = targetColor;
        p.passThroughPointC = passThroughPointC; p.passTStar = passTStar;
        p.animatePosition = animatePosition; p.animateSize = animateSize; p.animateRotationZ = animateRotationZ; p.animateAlpha = animateAlpha; p.animateColor = animateColor;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(p);
#endif
    }

    public void LoadFromPreset(UITweenPreset p)
    {
        if (p == null) return;

        useRelativeMode = p.useRelativeMode;
        useBezierPath = p.useBezierPath;

        duration = p.duration; delay = p.delay; loops = p.loops; loopType = p.loopType; unscaledTime = p.unscaledTime;
        useCustomCurve = p.useCustomCurve; customCurve = p.customCurve; easeType = p.easeType;
        targetAnchoredPosition = p.targetAnchoredPosition; targetSizeDelta = p.targetSizeDelta; targetPivot = p.targetPivot; targetEulerZ = p.targetEulerZ; targetAlpha = p.targetAlpha; targetColor = p.targetColor;
        passThroughPointC = p.passThroughPointC; passTStar = p.passTStar;
        animatePosition = p.animatePosition; animateSize = p.animateSize; animateRotationZ = p.animateRotationZ; animateAlpha = p.animateAlpha; animateColor = p.animateColor;
    }

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

    public Vector2 TargetPos => targetAnchoredPosition;
    public Vector2 PassPointC => passThroughPointC;
    public float PassTStar => passTStar;
    public bool UseRelativeMode => useRelativeMode;
    public bool UseBezierPath => useBezierPath;
}