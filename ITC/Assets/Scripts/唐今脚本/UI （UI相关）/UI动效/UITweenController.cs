// MIT License
// Jin Tang – Goal-Driven UI Tween (Bézier via pass-through point)
// Requires DOTween (Demigiant)

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UITweenController : MonoBehaviour
{
    [Header("Playback")]
    [Tooltip("总时长（秒）。")]
    public float duration = 0.6f;
    [Tooltip("起始延时（秒）。")]
    public float delay = 0f;
    [Tooltip("循环次数；-1 为无限循环。")]
    public int loops = 0;
    [Tooltip("循环类型。")]
    public LoopType loopType = LoopType.Restart;
    [Tooltip("是否忽略 Time.timeScale。")]
    public bool unscaledTime = true;

    [Header("Easing")]
    [Tooltip("是否使用自定义曲线作为整体节奏（若启用，将覆盖 EaseType）。")]
    public bool useCustomCurve = false;
    public AnimationCurve customCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public Ease easeType = Ease.OutCubic;

    [Header("Target B（最终目标状态）")]
    [Tooltip("RectTransform.anchoredPosition 的目标值（父本地）。")]
    public Vector2 targetAnchoredPosition;
    [Tooltip("RectTransform.sizeDelta 的目标值。")]
    public Vector2 targetSizeDelta;
    [Tooltip("RectTransform.pivot 的目标值（可选）。")]
    public Vector2 targetPivot = new Vector2(0.5f, 0.5f);
    [Tooltip("旋转欧拉角 Z（UI 常用），其余两个分量保留现状。")]
    public float targetEulerZ = 0f;
    [Tooltip("最终透明度。优先使用 CanvasGroup；若无则写入 Graphic.color.a。")]
    [Range(0f,1f)] public float targetAlpha = 1f;
    [Tooltip("最终颜色（若有 Graphic）。")]
    public Color targetColor = Color.white;

    [Header("Pass-Through C（途中必经点）")]
    [Tooltip("保证在 t* 时刻（0~1）经过的父本地方向坐标。")]
    public Vector2 passThroughPointC;
    [Tooltip("保证经过 C 的归一化时间点 t*，避免 0 或 1。")]
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
    CanvasGroup _canvasGroup; // 优先用于 alpha
    Graphic _graphic;         // 用于颜色 / 退路 alpha

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

        // 给个默认必经点：当前与目标中点
        passThroughPointC = parent ? 0.5f * ((Vector2)_rt.anchoredPosition + targetAnchoredPosition) : Vector2.zero;
        passTStar = 0.5f;
    }

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null && _graphic == null) _graphic = GetComponent<Graphic>();
    }

    // ---------------------- Public API ----------------------

    /// <summary>
    /// 以“当前实时状态”为起点，播放到“目标 B”，并在 passTStar 时严格经过 C。
    /// </summary>
    public Tween Play()
    {
        var seq = CreateAnimationSequence();
        seq.Play();
        return seq;
    }

    /// <summary>
    /// 仅创建 Tween 序列（不自动 Play），便于编辑器预览或外部拼接。
    /// </summary>
    public Sequence CreateAnimationSequence()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null && _graphic == null) _graphic = GetComponent<Graphic>();

        var seq = DOTween.Sequence().SetDelay(delay).SetUpdate(unscaledTime);

        // 统一节奏
        ApplyEaseTo(seq);

        // 起点 A：播放一刻的实时状态
        Vector2 A_pos = _rt.anchoredPosition;
        Vector2 B_pos = targetAnchoredPosition;
        Vector2 C_pos = passThroughPointC;
        float tStar = Mathf.Clamp(passTStar, 0.05f, 0.95f);

        // 反解控制点 P（仅用于“位置曲线”）
        Vector2 P = SolveQuadraticControlPoint(A_pos, B_pos, C_pos, tStar);

        // ---- Position on Quadratic Bézier ----
        if (animatePosition)
        {
            // 使用 DOVirtual.Float 驱动 0->1，按贝塞尔更新 anchoredPosition
            Tween posTween = DOVirtual.Float(0f, 1f, duration, (t) =>
            {
                _rt.anchoredPosition = QuadBezier(A_pos, P, B_pos, t);
            });
            ApplyEaseTo(posTween);
            seq.Join(posTween);
        }

        // ---- SizeDelta ----
        if (animateSize)
        {
            Tween sizeTween = _rt.DOSizeDelta(targetSizeDelta, duration);
            ApplyEaseTo(sizeTween);
            seq.Join(sizeTween);
        }

        // ---- Rotation Z ----
        if (animateRotationZ)
        {
            Vector3 e = _rt.eulerAngles;
            Vector3 targetEuler = new Vector3(e.x, e.y, targetEulerZ);
            Tween rotTween = _rt.DORotate(targetEuler, duration, RotateMode.FastBeyond360);
            ApplyEaseTo(rotTween);
            seq.Join(rotTween);
        }

        // ---- Alpha ----
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

        // ---- Color ----
        if (animateColor && _graphic != null)
        {
            Tween col = _graphic.DOColor(targetColor, duration);
            ApplyEaseTo(col);
            seq.Join(col);
        }

        // 循环
        if (loops != 0) seq.SetLoops(loops, loopType);

        return seq;
    }

    // ---------------------- Authoring Helpers ----------------------

    /// <summary>把当前对象状态记录为“目标 B”。（仅写入已暴露的目标字段，不立即应用到对象）</summary>
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

    /// <summary>将必经点 C 设为当前对象位置（父本地）。</summary>
    public void SetPassPointFromCurrent()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        passThroughPointC = _rt.anchoredPosition;
    }

    /// <summary>将必经点 C 置为“当前”与“目标 B”的中点。</summary>
    public void SetPassPointToMidCurrentAndTarget()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        passThroughPointC = 0.5f * (_rt.anchoredPosition + targetAnchoredPosition);
    }

    // ---------------------- Math & Ease Utils ----------------------

    private float ApplyEasing(float s)
    {
        if (!useCustomCurve) return DOVirtual.EasedValue(0f, 1f, s, easeType);
        return Mathf.Clamp01(customCurve.Evaluate(Mathf.Clamp01(s)));
    }

    private void ApplyEaseTo(Tween t)
    {
        if (useCustomCurve) t.SetEase(customCurve);
        else t.SetEase(easeType);
    }

    /// <summary>标准二次 Bézier：A, P(控制点), B。</summary>
    public static Vector2 QuadBezier(in Vector2 A, in Vector2 P, in Vector2 B, float t)
    {
        float u = 1f - t;
        return u*u*A + 2f*u*t*P + t*t*B;
    }

    /// <summary>
    /// 由 A、B 与“要求在 t* 经过 C”反解二次 Bézier 的控制点 P。
    /// 退化保护：当 t*→0 或 →1 时，回落到 (A+B)/2。
    /// </summary>
    public static Vector2 SolveQuadraticControlPoint(in Vector2 A, in Vector2 B, in Vector2 C, float tStar)
    {
        float u = 1f - tStar;
        float denom = 2f * u * tStar;
        if (denom < 1e-6f) return 0.5f * (A + B);
        return (C - (u*u)*A - (tStar*tStar)*B) / denom;
    }

    // 供编辑器读取显示（Scene 里画 Gizmo / Handle）
    public Vector2 TargetPos => targetAnchoredPosition;
    public Vector2 PassPointC => passThroughPointC;
    public float PassTStar => passTStar;
}
