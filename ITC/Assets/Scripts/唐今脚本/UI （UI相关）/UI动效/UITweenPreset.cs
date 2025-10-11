// MIT License
// ScriptableObject preset for Goal-Driven UI Tween

using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "NewUITweenPreset", menuName = "UI Tween/Goal-Driven Preset", order = 1000)]
public class UITweenPreset : ScriptableObject
{
    [Header("Identity")]
    public string presetName = "MyTween";

    [Header("Mode")]
    [Tooltip("勾選後，位置、尺寸、旋轉將作為基於初始狀態的【偏移量】，而非絕對目標值。此模式下為直線運動。")]
    public bool useRelativeMode = false;
    [Tooltip("僅在【絕對模式】下生效。勾選後，將啟用二次貝塞爾曲線路徑，可通過“途中必經點”進行調節。")]
    public bool useBezierPath = false;

    [Header("Playback")]
    public float duration = 0.6f;
    public float delay = 0f;
    public int loops = 0;
    public LoopType loopType = LoopType.Restart;
    public bool unscaledTime = true;

    [Header("Easing")]
    public bool useCustomCurve = false;
    public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public Ease easeType = Ease.OutCubic;

    [Header("Target B（最終目標）")]
    [Tooltip("在【相對模式】下，此值為位置偏移量。")]
    public Vector2 targetAnchoredPosition;
    [Tooltip("在【相對模式】下，此值為尺寸增量。")]
    public Vector2 targetSizeDelta;
    public Vector2 targetPivot = new Vector2(0.5f, 0.5f);
    [Tooltip("在【相對模式】下，此值為旋轉角度增量。")]
    public float targetEulerZ = 0f;
    [Range(0, 1)] public float targetAlpha = 1f;
    public Color targetColor = Color.white;

    [Header("Pass-Through C（途中必經 - 僅絕對模式+曲線路徑）")]
    public Vector2 passThroughPointC;
    [Range(0.05f, 0.95f)] public float passTStar = 0.5f;

    [Header("What to Animate")]
    public bool animatePosition = true;
    public bool animateSize = true;
    public bool animateRotationZ = false;
    public bool animateAlpha = false;
    public bool animateColor = false;

    public void ApplyEaseTo(Tween t)
    {
        if (useCustomCurve && customCurve != null) t.SetEase(customCurve);
        else t.SetEase(easeType);
        t.SetUpdate(unscaledTime);
        if (loops != 0) t.SetLoops(loops, loopType);
        if (delay > 0) t.SetDelay(delay);
    }
}