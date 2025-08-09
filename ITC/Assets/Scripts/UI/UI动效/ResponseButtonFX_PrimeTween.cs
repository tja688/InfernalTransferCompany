using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PrimeTween;

/// <summary>
/// 挂在“Response Button Template”（含 Button）的对象上。
/// Icon 放大/提亮 + 文字变色（Hover），点击闪红（Click）。
/// </summary>
public class ResponseButtonFX_PrimeTween : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Refs")]
    public Image icon;                 // 指向 Icon（Image）
    public RectTransform iconRect;     // Icon 的 RectTransform（不填会自动取 icon.rectTransform）
    public Graphic label;              // 文本（Text 或 TMP 都可，填它的 Graphic）

    [Header("Hover")]
    public Color labelHoverColor = new Color(1f, 0.87f, 0.2f); // 指定黄色
    [Range(1f, 1.2f)] public float iconHoverScale = 1.06f;     // 轻微放大
    [Range(0.01f, 0.3f)] public float hoverTweenTime = 0.08f;  // 动画时长
    [Range(0f, 0.5f)] public float iconBrighten = 0.15f;       // 往白色插值的强度

    [Header("Click Flash")]
    public Color labelClickFlashColor = new Color(1f, 0.2f, 0.2f); // 闪红
    [Range(0.02f, 0.2f)] public float clickFlashTime = 0.10f;      // 闪红停留

    // 运行时备份
    Color _labelNormalColor;
    Color _iconNormalColor;
    Vector3 _iconNormalScale;

    // Tween 句柄，便于中断/覆盖
    Tween _tLabelHover, _tIconHover, _tScaleHover, _tClickFlash;

    void Awake() {
        if (iconRect == null && icon != null) iconRect = icon.rectTransform;
        if (label != null) _labelNormalColor = label.color;
        if (icon  != null) _iconNormalColor  = icon.color;
        if (iconRect != null) _iconNormalScale = iconRect.localScale;
    }

    void OnEnable() {
        KillAll();
        ResetVisual();
    }

    void OnDisable() {
        KillAll();
    }

    void KillAll() {
        _tLabelHover.Stop();
        _tIconHover.Stop();
        _tScaleHover.Stop();
        _tClickFlash.Stop();
    }

    void ResetVisual() {
        if (label != null) label.color = _labelNormalColor;
        if (icon  != null) icon.color  = _iconNormalColor;
        if (iconRect != null) iconRect.localScale = _iconNormalScale;
    }

    // ------ Hover (鼠标 & 键盘/手柄) ------
    public void OnPointerEnter(PointerEventData e) => PlayHover(true);
    public void OnPointerExit (PointerEventData e) => PlayHover(false);
    public void OnSelect (BaseEventData e)         => PlayHover(true);
    public void OnDeselect(BaseEventData e)        => PlayHover(false);

    void PlayHover(bool enter) {
        if (label != null) {
            var c0 = label.color;
            var c1 = enter ? labelHoverColor : _labelNormalColor;
            _tLabelHover.Stop();
            // 用 0->1 的插值来做颜色过渡（兼容任何 Graphic）
            _tLabelHover = Tween.Custom(0f, 1f, hoverTweenTime,
                onValueChange: t => label.color = Color.Lerp(c0, c1, t));
        }

        if (icon != null) {
            var from = icon.color;
            var toHover = Color.Lerp(_iconNormalColor, Color.white, iconBrighten);
            var c1 = enter ? toHover : _iconNormalColor;
            _tIconHover.Stop();
            _tIconHover = Tween.Custom(0f, 1f, hoverTweenTime,
                onValueChange: t => icon.color = Color.Lerp(from, c1, t));
        }

        if (iconRect != null) {
            var s0 = iconRect.localScale;
            var s1 = enter ? (_iconNormalScale * iconHoverScale) : _iconNormalScale;
            _tScaleHover.Stop();
            _tScaleHover = Tween.LocalScale(iconRect, s1, hoverTweenTime);
        }
    }

    // ------ Click 闪红（不阻塞对话系统回调） ------
    public void OnPointerDown(PointerEventData e) {
        if (label == null) return;

        _tClickFlash.Stop();

        // 立即到红 -> 等待 -> 回到原色/当前 Hover 色（谁在当前就回谁）
        var afterColor = IsHoveringOrSelected() ? labelHoverColor : _labelNormalColor;

        // 先瞬间到红（0.02 更利落）
        _tClickFlash = Tween.Custom(0f, 1f, 0.02f, onValueChange: t => {
            label.color = Color.Lerp(label.color, labelClickFlashColor, t);
        })
        .OnComplete(() => {
            // 停留一会，再 tween 回去
            Tween.Delay(clickFlashTime).OnComplete(() => {
                _tClickFlash = Tween.Custom(0f, 1f, 0.06f,
                    onValueChange: t => label.color = Color.Lerp(label.color, afterColor, t));
            });
        });
    }

    bool IsHoveringOrSelected() {
        // 简易判断：若当前颜色更靠近 hover 色（或按钮被 EventSystem 选中）
        if (label == null) return false;
        var a = label.color;
        float dHover = ColorDistance(a, labelHoverColor);
        float dNorm  = ColorDistance(a, _labelNormalColor);
        bool closerToHover = dHover < dNorm;
        bool selected = EventSystem.current != null &&
                        EventSystem.current.currentSelectedGameObject == gameObject;
        return closerToHover || selected;
    }

    static float ColorDistance(Color a, Color b) {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
    }
}
