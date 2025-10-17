// MIT License
// [MODIFIED] Upgraded rotation from float (Z-axis only) to Vector3 (X, Y, Z support).
// [MODIFIED] Renamed animateRotationZ to animateRotation.
// [MODIFIED] Reworked CreateAnimationSequence to use SetRelative() for seamless looping.
// [MODIFIED] Updated Invisibility Detection to sample the main rotation track.

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    public Vector3 targetEulerAngles = Vector3.zero; // MODIFIED
    [Range(0f,1f)] public float targetAlpha = 1f;
    public Color targetColor = Color.white;

    [Header("Pass-Through C（途中必經點）")]
    public Vector2 passThroughPointC;
    [Range(0.05f, 0.95f)] public float passTStar = 0.5f;

    [Header("What to Animate")]
    public bool animatePosition = true;
    public bool animateSize = true;
    public bool animateRotation = true; // MODIFIED
    public bool animateAlpha = true;
    public bool animateColor = true;

    [Header("Secondary Animations")]
    [Tooltip("在主动画播放期间叠加的次级动画轨道")]
    public List<SecondaryTween> secondaryTweens = new List<SecondaryTween>();

    [Header("Timeline Events")]
    [Tooltip("在时间轴特定时间点触发的模块化事件")]
    public List<TimelineEvent> timelineEvents = new List<TimelineEvent>();

    [Header("Gizmos & Preview (Editor Only)")]
    public bool showPathGizmos = true;

    // ================== Invisible Instants Detection (Config) ==================
    [Header("Invisibility Detection")]
    [Tooltip("启用 alpha<=阈值 的不可见检测")]
    public bool detectAlpha = true;
    [Range(0f, 0.2f)]
    public float invisAlphaThreshold = 0.01f;

    [Tooltip("启用 scale/size 很小的不可见检测")]
    public bool detectScaleOrSize = true;
    [Tooltip("当 localScale 的 min(x,y) ≤ 此阈值时判定为不可见（若无Scale轨，则按SizeDelta判定）")]
    [Range(0f, 0.2f)]
    public float invisScaleThreshold = 0.02f;
    [Tooltip("当 sizeDelta 的 min(w,h) ≤ 此阈值时判定为不可见（px）")]
    public float invisSizeThreshold = 1f;

    [Tooltip("启用 Y 轴转到“只剩一条缝”的不可见检测（≈90°、270°…）")]
    public bool detectRotationY = true;
    [Tooltip("与 90°(mod 180°) 的角度容差（度）")]
    [Range(0.1f, 15f)]
    public float invisAngleToleranceDeg = 1.0f;

    [Tooltip("检测采样数量（越大越精细，性能线性增加）")]
    [Range(30, 720)]
    public int detectionSamples = 240;

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
        animateRotation = false; // MODIFIED
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

        // ===== MODIFIED ROTATION LOGIC =====
        if (animateRotation)
        {
            Tweener rotTween;
            if (useRelativeMode)
            {
                // For seamless looping, use SetRelative. The target is the *change* per loop.
                Vector3 relativeChange = reversed ? -targetEulerAngles : targetEulerAngles;
                rotTween = _rt.DORotate(relativeChange, duration, RotateMode.FastBeyond360).SetRelative();
            }
            else // Absolute mode
            {
                Vector3 startEuler = _rt.localEulerAngles;
                Vector3 finalEuler = reversed ? startEuler : targetEulerAngles;
                 // Use .From() for reversible absolute tweens, but need to capture start state.
                rotTween = _rt.DORotate(finalEuler, duration, RotateMode.Fast);
                if (reversed)
                {
                    // In editor preview, we manually reset. .From() is more for runtime.
                    // Let's emulate .From() by tweening from target to current.
                    Vector3 tempTarget = _rt.localEulerAngles;
                    _rt.localEulerAngles = targetEulerAngles;
                    rotTween = _rt.DORotate(tempTarget, duration, RotateMode.Fast);
                }
            }
            ApplyEaseTo(rotTween);
            seq.Join(rotTween);
        }
        // ===== END MODIFIED ROTATION LOGIC =====


        if (animateAlpha)
        {
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

        if (secondaryTweens != null)
        {
            foreach (var secondary in secondaryTweens)
            {
                var subTween = BuildSecondaryTweener(secondary);
                if (subTween == null) continue;

                float insertTime = ResolveInsertTime(secondary.startTime, duration, reversed, secondary.duration);
                seq.Insert(insertTime, subTween);
            }
        }

        if (timelineEvents != null)
        {
            foreach (var timelineEvent in timelineEvents)
            {
                float insertTime = ResolveInsertTime(timelineEvent.fireTime, duration, reversed, 0f);
                seq.InsertCallback(insertTime, () => ExecuteTimelineEvent(timelineEvent));
            }
        }

        if (loops != 0) seq.SetLoops(loops, loopType);
        return seq;
    }

    public List<float> ComputeInvisibilityTimes()
    {
        var result = new List<float>();
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_rt == null) return result;

        float startAlpha = 1f;
        if (_canvasGroup != null) startAlpha = _canvasGroup.alpha;
        else if (_graphic != null) startAlpha = _graphic.color.a;

        Vector2 startSize = _rt.sizeDelta;
        Vector3 startEuler = _rt.localEulerAngles;

        float mainAlphaTarget = animateAlpha ? targetAlpha : startAlpha;
        Vector2 mainSizeTarget = animateSize ? (useRelativeMode ? (startSize + targetSizeDelta) : targetSizeDelta) : startSize;
        Vector3 mainEulerTarget = animateRotation ? (useRelativeMode ? (startEuler + targetEulerAngles) : targetEulerAngles) : startEuler; // MODIFIED
        
        float EvaluateMain01(float t01)
        {
            t01 = Mathf.Clamp01(t01);
            if (useCustomCurve && customCurve != null) return Mathf.Clamp01(customCurve.Evaluate(t01));
            return Mathf.Clamp01(DOVirtual.EasedValue(0f, 1f, t01, easeType));
        }

        bool IsAlphaInvisible(float a) => detectAlpha && (a <= invisAlphaThreshold);
        bool IsSizeInvisible(Vector2 sz) => detectScaleOrSize && (Mathf.Min(Mathf.Abs(sz.x), Mathf.Abs(sz.y)) <= invisSizeThreshold);

        int N = Mathf.Max(30, detectionSamples);
        Vector3 prevEuler = startEuler; // MODIFIED: Track previous euler for interval checking
        
        for (int i = 0; i <= N; i++)
        {
            float t01 = (i / (float)N);
            float tSec = t01 * Mathf.Max(0.0001f, duration);
            float k = EvaluateMain01(t01);

            float alphaNow = Mathf.LerpUnclamped(startAlpha, mainAlphaTarget, k);
            Vector2 sizeNow = Vector2.LerpUnclamped(startSize, mainSizeTarget, k);
            
            // --- MODIFICATION START for main track rotation detection ---
            Vector3 eulerNow = Vector3.LerpUnclamped(startEuler, mainEulerTarget, k);
            if (i > 0) // Check interval from previous sample to current
            {
                float prevTimeSec = ((i - 1) / (float)N) * duration;
                FindCrossings(prevEuler.y, eulerNow.y, prevTimeSec, tSec, result);
            }
            prevEuler = eulerNow;
            // --- MODIFICATION END ---
            
            if (IsAlphaInvisible(alphaNow) || IsSizeInvisible(sizeNow))
            {
                result.Add(tSec);
            }
        }
        
        // ... (Secondary tweens sampling remains the same)
        if (secondaryTweens != null)
        {
            foreach (var sec in secondaryTweens)
            {
                if (sec == null || sec.duration <= 0f || sec.propertyType != SecondaryTweenType.Rotation || !detectRotationY) continue;

                int n = Mathf.Clamp(N / 2, 30, 360);
                Vector3 baseE = startEuler;
                Vector3 tgt = sec.targetValue;
                Vector3 prevE = baseE;

                for (int i = 1; i <= n; i++)
                {
                    float local01 = i / (float)n;
                    float eased = DOVirtual.EasedValue(0f, 1f, local01, sec.easeType);
                    
                    Vector3 currE = sec.isRelative 
                        ? (baseE + tgt * eased) 
                        : Vector3.LerpUnclamped(baseE, new Vector3(baseE.x, tgt.y, baseE.z), eased);

                    float prevTimeSec = sec.startTime + sec.duration * ((i - 1) / (float)n);
                    float currentTimeSec = sec.startTime + sec.duration * local01;

                    FindCrossings(prevE.y, currE.y, prevTimeSec, currentTimeSec, result);
                    prevE = currE;
                }
            }
        }

        result.Sort();
        const float MERGE_EPS = 1f / 1000f;
        if (result.Count == 0) return result;

        var merged = new List<float> { result[0] };
        for (int i = 1; i < result.Count; i++)
        {
            if (Mathf.Abs(result[i] - merged[merged.Count - 1]) > MERGE_EPS)
                merged.Add(result[i]);
        }
        return merged;
    }
    
    private void FindCrossings(float startAngle, float endAngle, float startTime, float endTime, List<float> result)
    {
        float startCos = Mathf.Cos(startAngle * Mathf.Deg2Rad);
        float endCos = Mathf.Cos(endAngle * Mathf.Deg2Rad);
        if (Mathf.Sign(startCos) != Mathf.Sign(endCos))
        {
            float t = -startCos / (endCos - startCos);
            t = Mathf.Clamp01(t);
            float crossingTime = Mathf.Lerp(startTime, endTime, t);
            result.Add(crossingTime);
        }
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

    private float ResolveInsertTime(float requestedTime, float totalDuration, bool reversed, float span)
    {
        if (!reversed) return Mathf.Max(0f, requestedTime);
        float mirrored = totalDuration - requestedTime - span;
        if (float.IsNaN(mirrored) || float.IsInfinity(mirrored)) return 0f;
        return Mathf.Clamp(mirrored, 0f, Mathf.Max(totalDuration, 0f));
    }

    private Tweener BuildSecondaryTweener(SecondaryTween secondary)
    {
        if (secondary == null || secondary.duration <= 0f || _rt == null) return null;

        Tweener tween = null;
        switch (secondary.propertyType)
        {
            case SecondaryTweenType.Rotation:
            {
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
                if (_canvasGroup != null)
                {
                    float baseAlpha = _canvasGroup.alpha;
                    float finalAlpha = Mathf.Clamp01(secondary.isRelative ? baseAlpha + value : value);
                    tween = _canvasGroup.DOFade(finalAlpha, secondary.duration);
                }
                else if (_graphic != null)
                {
                    float baseAlpha = _graphic.color.a;
                    float finalAlpha = Mathf.Clamp01(secondary.isRelative ? baseAlpha + value : value);
                    tween = _graphic.DOFade(finalAlpha, secondary.duration);
                }
                break;
            }
            case SecondaryTweenType.Color:
            {
                if (_graphic != null)
                {
                    Color baseColor = _graphic.color;
                    Color delta = secondary.targetColor;
                    Color finalColor = secondary.isRelative ? baseColor + delta : delta;
                    finalColor.r = Mathf.Clamp01(finalColor.r);
                    finalColor.g = Mathf.Clamp01(finalColor.g);
                    finalColor.b = Mathf.Clamp01(finalColor.b);
                    finalColor.a = Mathf.Clamp01(finalColor.a);
                    tween = _graphic.DOColor(finalColor, secondary.duration);
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
                if (image == null) image = _graphic as Image;
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

    private List<SecondaryTween> CloneSecondaryTweens(List<SecondaryTween> source)
    {
        var list = new List<SecondaryTween>();
        if (source == null) return list;
        foreach (var item in source)
        {
            if (item == null) continue;
            var clone = new SecondaryTween
            {
                name = item.name,
                propertyType = item.propertyType,
                startTime = item.startTime,
                duration = item.duration,
                targetValue = item.targetValue,
                easeType = item.easeType,
                isRelative = item.isRelative,
                targetColor = item.targetColor
            };
            list.Add(clone);
        }
        return list;
    }

    private List<TimelineEvent> CloneTimelineEvents(List<TimelineEvent> source)
    {
        var list = new List<TimelineEvent>();
        if (source == null) return list;
        foreach (var item in source)
        {
            if (item == null) continue;
            var clone = new TimelineEvent
            {
                name = item.name,
                fireTime = item.fireTime,
                eventType = item.eventType,
                audioClip = item.audioClip,
                newSprite = item.newSprite,
                targetImage = item.targetImage,
                messageName = item.messageName,
                messageParameter = item.messageParameter,
                customCallback = item.customCallback
            };
            list.Add(clone);
        }
        return list;
    }
    
    public void SaveToPreset(UITweenPreset preset, bool keepPresetName = false)
    {
        if (preset == null) return;

        #if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(preset, "Save UI Tween Preset");
        #endif

        var t = preset.GetType();

        void SetFieldOrProp<T>(string name, T value)
        {
            var f = t.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (f != null && f.FieldType.IsAssignableFrom(typeof(T))) { f.SetValue(preset, value); return; }

            var p = t.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (p != null && p.CanWrite && p.PropertyType.IsAssignableFrom(typeof(T))) { p.SetValue(preset, value, null); }
        }

        SetFieldOrProp("useRelativeMode", useRelativeMode);
        SetFieldOrProp("useBezierPath", useBezierPath);
        SetFieldOrProp("duration", duration);
        SetFieldOrProp("delay", delay);
        SetFieldOrProp("loops", loops);
        SetFieldOrProp("loopType", loopType);
        SetFieldOrProp("unscaledTime", unscaledTime);
        SetFieldOrProp("useCustomCurve", useCustomCurve);
        SetFieldOrProp("customCurve", customCurve);
        SetFieldOrProp("easeType", easeType);
        SetFieldOrProp("targetAnchoredPosition", targetAnchoredPosition);
        SetFieldOrProp("targetSizeDelta", targetSizeDelta);
        SetFieldOrProp("targetPivot", targetPivot);
        SetFieldOrProp("targetEulerAngles", targetEulerAngles); // MODIFIED
        SetFieldOrProp("targetAlpha", targetAlpha);
        SetFieldOrProp("targetColor", targetColor);
        SetFieldOrProp("passThroughPointC", passThroughPointC);
        SetFieldOrProp("passTStar", passTStar);
        SetFieldOrProp("animatePosition", animatePosition);
        SetFieldOrProp("animateSize", animateSize);
        SetFieldOrProp("animateRotation", animateRotation); // MODIFIED
        SetFieldOrProp("animateAlpha", animateAlpha);
        SetFieldOrProp("animateColor", animateColor);
        SetFieldOrProp("secondaryTweens", CloneSecondaryTweens(secondaryTweens));
        SetFieldOrProp("timelineEvents",  CloneTimelineEvents(timelineEvents));

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(preset);
        #endif
    }

    public void LoadFromPreset(UITweenPreset preset)
    {
        if (preset == null) return;

        var t = preset.GetType();

        T GetFieldOrProp<T>(string name, T fallback)
        {
            var f = t.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (f != null && typeof(T).IsAssignableFrom(f.FieldType))
            {
                object v = f.GetValue(preset);
                if (v is T tv) return tv;
            }
            var p = t.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (p != null && p.CanRead && typeof(T).IsAssignableFrom(p.PropertyType))
            {
                object v = p.GetValue(preset, null);
                if (v is T tv) return tv;
            }
            return fallback;
        }

        #if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Load UI Tween From Preset");
        #endif
        
        useRelativeMode = GetFieldOrProp("useRelativeMode", useRelativeMode);
        useBezierPath   = GetFieldOrProp("useBezierPath",   useBezierPath);
        duration    = GetFieldOrProp("duration",    duration);
        delay       = GetFieldOrProp("delay",       delay);
        loops       = GetFieldOrProp("loops",       loops);
        loopType    = GetFieldOrProp("loopType",    loopType);
        unscaledTime= GetFieldOrProp("unscaledTime",unscaledTime);
        useCustomCurve = GetFieldOrProp("useCustomCurve", useCustomCurve);
        customCurve    = GetFieldOrProp("customCurve",    customCurve);
        easeType       = GetFieldOrProp("easeType",       easeType);
        targetAnchoredPosition = GetFieldOrProp("targetAnchoredPosition", targetAnchoredPosition);
        targetSizeDelta        = GetFieldOrProp("targetSizeDelta",        targetSizeDelta);
        targetPivot            = GetFieldOrProp("targetPivot",            targetPivot);
        targetEulerAngles      = GetFieldOrProp("targetEulerAngles",      targetEulerAngles); // MODIFIED
        targetAlpha            = GetFieldOrProp("targetAlpha",            targetAlpha);
        targetColor            = GetFieldOrProp("targetColor",            targetColor);
        passThroughPointC = GetFieldOrProp("passThroughPointC", passThroughPointC);
        passTStar         = GetFieldOrProp("passTStar",         passTStar);
        animatePosition  = GetFieldOrProp("animatePosition",  animatePosition);
        animateSize      = GetFieldOrProp("animateSize",      animateSize);
        animateRotation = GetFieldOrProp("animateRotation", animateRotation); // MODIFIED
        animateAlpha     = GetFieldOrProp("animateAlpha",     animateAlpha);
        animateColor     = GetFieldOrProp("animateColor",     animateColor);
        secondaryTweens = CloneSecondaryTweens(GetFieldOrProp("secondaryTweens", secondaryTweens));
        timelineEvents = CloneTimelineEvents(GetFieldOrProp("timelineEvents", timelineEvents));

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    [ContextMenu("Capture Target From Current")]
    public void CaptureTargetFromCurrent()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_rt == null) return;
        
        targetAnchoredPosition = _rt.anchoredPosition;
        targetSizeDelta        = _rt.sizeDelta;
        targetPivot            = _rt.pivot;
        targetEulerAngles      = _rt.localEulerAngles; // MODIFIED

        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_graphic == null) _graphic = GetComponent<UnityEngine.UI.Graphic>();

        if (_canvasGroup != null) targetAlpha = _canvasGroup.alpha;
        else if (_graphic != null) targetAlpha = _graphic.color.a;

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    [ContextMenu("Set Pass-Through C From Current")]
    public void SetPassPointFromCurrent()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_rt == null) return;

        passThroughPointC = _rt.anchoredPosition;

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    [ContextMenu("Set Pass-Through C To Mid(Current, Target)")]
    public void SetPassPointToMidCurrentAndTarget()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_rt == null) return;

        passThroughPointC = 0.5f * (_rt.anchoredPosition + targetAnchoredPosition);

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}

