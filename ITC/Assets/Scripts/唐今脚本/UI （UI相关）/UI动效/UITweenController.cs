using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// (最終完整版) 一個強大的UI動效控制器，支持在編輯器中設置初始/目標狀態，並提供實時預覽。
/// 支持RectTransform、旋轉、顏色/透明度的動畫，並允許使用自訂的AnimationCurve和二次貝茲曲線路徑。
/// </summary>
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("UI/Tween Controller (Path Master)")]
public class UITweenController : MonoBehaviour
{
    // --- 动画设定 ---
    [Header("Animation Settings")]
    [Tooltip("動畫持續時間（秒）")]
    public float duration = 1f;
    
    [Tooltip("勾選後，將使用下方的自訂曲線，而非預設的Ease類型。")]
    public bool useAnimationCurve = false;

    [Tooltip("動畫使用的緩動曲線")]
    public Ease easeType = Ease.OutQuad;

    [Tooltip("自訂的動畫曲線")]
    public AnimationCurve customEaseCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Tooltip("啟用後，才會對顏色或透明度進行動畫")]
    public bool animateColor = false; 

    // --- 路徑設定 ---
    [Header("Path Settings")]
    [Tooltip("啟用後，將沿著貝茲曲線運動，而非直線運動")]
    public bool usePath = false;
    
    // 儲存烘焙後的控制點位置（相對於Canvas的局部空間）
    [SerializeField] private Vector2 pathControlPoint;

    // --- 状态数据 ---
    [Header("State Data")]
    [SerializeField] private Vector2 startAnchoredPosition;
    [SerializeField] private Vector2 targetAnchoredPosition;
    [SerializeField] private Vector2 startSizeDelta;
    [SerializeField] private Vector2 targetSizeDelta;
    [SerializeField] private Vector3 startRotation;
    [SerializeField] private Vector3 targetRotation;
    [SerializeField] private Color startColor;
    [SerializeField] private Color targetColor;
    
    // --- 组件缓存 ---
    private RectTransform _rectTransform;
    private Graphic _graphic;
    private CanvasGroup _canvasGroup;
    public RectTransform RectTransform => _rectTransform ?? (_rectTransform = GetComponent<RectTransform>());

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _graphic = GetComponent<Graphic>();
        }
    }

    #region Public Methods for Editor
    
    public Vector2 GetStartPos() => startAnchoredPosition;
    public Vector2 GetTargetPos() => targetAnchoredPosition;
    public Vector2 GetControlPoint() => pathControlPoint;

    public void SetControlPoint(Vector2 newPoint)
    {
        pathControlPoint = newPoint;
    }

    public void ResetControlPoint()
    {
        pathControlPoint = (startAnchoredPosition + targetAnchoredPosition) * 0.5f;
    }

    public void RecordInitialState()
    {
        startAnchoredPosition = RectTransform.anchoredPosition;
        startSizeDelta = RectTransform.sizeDelta;
        startRotation = RectTransform.eulerAngles;

        if (_canvasGroup != null) startColor = new Color(1, 1, 1, _canvasGroup.alpha);
        else if (_graphic != null) startColor = _graphic.color;
        
        // 每次記錄初始點時，都先重置路徑為直線，確保從一個乾淨的狀態開始
        ResetControlPoint();
    }
    
    public void RecordTargetState()
    {
        targetAnchoredPosition = RectTransform.anchoredPosition;
        targetSizeDelta = RectTransform.sizeDelta;
        targetRotation = RectTransform.eulerAngles;
        
        if (_canvasGroup != null) targetColor = new Color(1, 1, 1, _canvasGroup.alpha);
        else if (_graphic != null) targetColor = _graphic.color;
    }

    public void RevertToInitialState()
    {
        RectTransform.anchoredPosition = startAnchoredPosition;
        RectTransform.sizeDelta = startSizeDelta;
        RectTransform.eulerAngles = startRotation;

        if(animateColor) 
        {
            if (_canvasGroup != null) _canvasGroup.alpha = startColor.a;
            else if (_graphic != null) _graphic.color = startColor;
        }
    }
    
    #endregion

    #region Animation Playback

    public Sequence CreateAnimationSequence()
    {
        Sequence seq = DOTween.Sequence();
        
        // 位置動畫邏輯
        if (usePath)
        {
            Tween pathTween = DOTween.To(
                () => 0f, 
                t => {
                    Vector2 newPos = GetPointOnQuadraticBezierCurve(startAnchoredPosition, pathControlPoint, targetAnchoredPosition, t);
                    RectTransform.anchoredPosition = newPos;
                },
                1f,
                duration
            );
            seq.Join(pathTween);
        }
        else
        {
            seq.Join(RectTransform.DOAnchorPos(targetAnchoredPosition, duration));
        }

        // 其他動畫屬性
        seq.Join(RectTransform.DOSizeDelta(targetSizeDelta, duration));
        seq.Join(RectTransform.DORotate(targetRotation, duration, RotateMode.Fast));

        if (animateColor)
        {
            if (_canvasGroup != null) seq.Join(_canvasGroup.DOFade(targetColor.a, duration));
            else if (_graphic != null) seq.Join(_graphic.DOColor(targetColor, duration));
        }

        // Ease 設定
        if (useAnimationCurve)
        {
            seq.SetEase(customEaseCurve);
        }
        else
        {
            seq.SetEase(easeType);
        }
        
        seq.Pause();
        seq.SetTarget(this);
        return seq;
    }

    public void Play()
    {
        CreateAnimationSequence().Play();
    }
    #endregion

    private static Vector2 GetPointOnQuadraticBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return (oneMinusT * oneMinusT * p0) + (2f * oneMinusT * t * p1) + (t * t * p2);
    }
}