// MIT License
// ScriptableObject preset for Goal-Driven UI Tween

using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "NewUITweenPreset", menuName = "UI Tween/Goal-Driven Preset", order = 1000)]
public class UITweenPreset : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("作为调用的唯一凭证。建议与资产名一致，或在库中唯一。")]
    public string presetName = "MyTween";

    [Header("Playback")]
    public float duration = 0.6f;
    public float delay = 0f;
    public int loops = 0;                 // -1 = infinite
    public LoopType loopType = LoopType.Restart;
    public bool unscaledTime = true;

    [Header("Easing")]
    public bool useCustomCurve = false;
    public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public Ease easeType = Ease.OutCubic;

    [Header("Target B（最终目标）")]
    public Vector2 targetAnchoredPosition;
    public Vector2 targetSizeDelta;
    public Vector2 targetPivot = new Vector2(0.5f, 0.5f);
    public float targetEulerZ = 0f;
    [Range(0, 1)] public float targetAlpha = 1f;
    public Color targetColor = Color.white;

    [Header("Pass-Through C（途中必经）")]
    public Vector2 passThroughPointC;
    [Range(0.05f, 0.95f)] public float passTStar = 0.5f;

    [Header("What to Animate")]
    public bool animatePosition = true;
    public bool animateSize = false;
    public bool animateRotationZ = false;
    public bool animateAlpha = false;
    public bool animateColor = false;

    // —— 工具：把此 Preset 的节奏设置到任意 Tween/Sequence
    public void ApplyEaseTo(Tween t)
    {
        if (useCustomCurve && customCurve != null) t.SetEase(customCurve);
        else t.SetEase(easeType);
        t.SetUpdate(unscaledTime);
        if (loops != 0) t.SetLoops(loops, loopType);
        if (delay > 0) t.SetDelay(delay);
    }
}