using UnityEngine;
using UnityEngine.EventSystems;
using PrimeTween;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class MapIconController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
    [Header("References")]
    public Transform mapTransform;

    [Header("Positions (Local by default)")]
    public bool useLocalPosition = false;              // 你现在用世界坐标，保持 false
    public Vector2 closedPos = new Vector2(0f, 5.4f);
    public Vector2 openPos   = new Vector2(0f, -5.4f);

    [Header("Map Motion")]
    public float mapDuration = 0.6f;
    public EasePreset mapEasePreset = EasePreset.FastInSlowOut;

    [Header("Hover Scale (Icon)")]
    public float hoverScaleFactor = 1.08f;
    public float hoverDuration = 0.12f;
    public Ease hoverEase = Ease.OutQuad;

    [Header("Click Feedback")]
    public bool  clickBounce = true;
    public float clickBounceScale = 1.12f;
    public float clickBounceDuration = 0.08f;

    [Header("Input")]
    [Tooltip("仅当需要不用 EventSystem 时才启用 OnMouse 备选路径")]
    public bool useOnMouseFallback = false;    // 默认不用 OnMouse，避免双触发

    bool isOpen;
    bool isAnimating;                          // 动画锁，防止连点/双触发
    Vector3 baseScale;
    Tween hoverTween, mapTween, clickTween;

    // 在类里加：
    [Header("Events")]
    public UnityEvent onMapOpened;
    public UnityEvent onMapClosed;
    public bool IsOpen => isOpen;   // 若外部想读状态



    void Awake() {
        baseScale = transform.localScale;

        if (!mapTransform) { Debug.LogError("[MapIconController] 未绑定 mapTransform。"); enabled = false; return; }

        // 初始化地图到收起位置
        if (useLocalPosition) {
            var p = mapTransform.localPosition;
            mapTransform.localPosition = new Vector3(closedPos.x, closedPos.y, p.z);
        } else {
            var p = mapTransform.position;
            mapTransform.position = new Vector3(closedPos.x, closedPos.y, p.z);
        }
        isOpen = false;
    }

    // -------- Pointer 事件（推荐） --------
    public void OnPointerEnter(PointerEventData _) => DoHoverEnter();
    public void OnPointerExit(PointerEventData _)  => DoHoverExit();
    public void OnPointerClick(PointerEventData _) => DoClick();

    // -------- OnMouse 备选（仅当 useOnMouseFallback = true 时启用） --------
    void OnMouseEnter() { if (useOnMouseFallback) DoHoverEnter(); }
    void OnMouseExit()  { if (useOnMouseFallback) DoHoverExit(); }
    void OnMouseDown()  { if (useOnMouseFallback) DoClick(); }

    void DoHoverEnter() {
        hoverTween.Stop();
        Tween.StopAll(transform);
        hoverTween = Tween.Scale(transform, baseScale * hoverScaleFactor, hoverDuration, hoverEase);
    }

    void DoHoverExit() {
        hoverTween.Stop();
        Tween.StopAll(transform);
        hoverTween = Tween.Scale(transform, baseScale, hoverDuration, hoverEase);
    }

    void DoClick() {
        if (isAnimating) return;  // 动画进行中忽略点击

        if (clickBounce) {
            clickTween.Stop();
            clickTween = Tween.Scale(transform, baseScale * clickBounceScale, clickBounceDuration, Ease.OutQuad)
                .OnComplete(() => Tween.Scale(transform, baseScale * hoverScaleFactor, clickBounceDuration, Ease.OutQuad));
        }
        ToggleMap();
    }

    // —— 对外接口 ——
    public void ToggleMap() {
        if (isOpen) HideMap(); else ShowMap();
    }

    public void ShowMap() {
        if (isAnimating) return;
        isAnimating = true;

        mapTween.Stop();
        Tween.StopAll(mapTransform);
        var ease = ToPrimeEase(mapEasePreset);

        if (useLocalPosition) {
            var p = mapTransform.localPosition;
            mapTween = Tween.LocalPosition(mapTransform, new Vector3(openPos.x, openPos.y, p.z), mapDuration, ease)
                .OnComplete(() => { isAnimating = false; isOpen = true; });
        } else {
            var p = mapTransform.position;
            mapTween = Tween.Position(mapTransform, new Vector3(openPos.x, openPos.y, p.z), mapDuration, ease)
                .OnComplete(() => { isAnimating = false; isOpen = true; });
        }
        // 在 ShowMap() 的 OnComplete 回调里最后加：
        onMapOpened?.Invoke();
    }

    public void HideMap() {
        if (isAnimating) return;
        isAnimating = true;

        mapTween.Stop();
        Tween.StopAll(mapTransform);
        var ease = ToPrimeEase(mapEasePreset);

        if (useLocalPosition) {
            var p = mapTransform.localPosition;
            mapTween = Tween.LocalPosition(mapTransform, new Vector3(closedPos.x, closedPos.y, p.z), mapDuration, ease)
                .OnComplete(() => { isAnimating = false; isOpen = false; });
        } else {
            var p = mapTransform.position;
            mapTween = Tween.Position(mapTransform, new Vector3(closedPos.x, closedPos.y, p.z), mapDuration, ease)
                .OnComplete(() => { isAnimating = false; isOpen = false; });
        }
// 在 HideMap() 的 OnComplete 回调里最后加：
        onMapClosed?.Invoke();
    }

    // —— 动效预设 —— 
    public enum EasePreset { Linear, FastInSlowOut, SlowInFastOut, Smooth, Snappy, Overshoot, Glide, Springy }
    static Ease ToPrimeEase(EasePreset preset) {
        switch (preset) {
            case EasePreset.Linear:        return Ease.Linear;
            case EasePreset.FastInSlowOut: return Ease.OutCubic;
            case EasePreset.SlowInFastOut: return Ease.InCubic;
            case EasePreset.Smooth:        return Ease.InOutCubic;
            case EasePreset.Snappy:        return Ease.OutQuint;
            case EasePreset.Overshoot:     return Ease.OutBack;
            case EasePreset.Glide:         return Ease.OutExpo;
            case EasePreset.Springy:       return Ease.OutElastic;
            default:                       return Ease.OutCubic;
        }
    }
}
