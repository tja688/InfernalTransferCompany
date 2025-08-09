using System.Collections;
using UnityEngine;

public class DSBackgroundSlide : MonoBehaviour
{
    [Header("Target")]
    public RectTransform background;                  // 你的 Dialogue Background

    [Header("Anchored Positions")]
    public Vector2 shownPos = Vector2.zero;           // 预设位置
    public Vector2 hiddenPos = new Vector2(0, -800f); // 屏幕下方，按UI尺寸调

    [Header("Timing")]
    public float showDuration = 0.25f;
    public float hideDuration = 0.20f;

    bool _visible = false;

    void Awake()
    {
        if (background == null) background = GetComponent<RectTransform>();
        background.gameObject.SetActive(false);
        background.anchoredPosition = hiddenPos;
    }

    // —— 会话生命周期回调（由 Dialogue System 自动调用）——
    public void OnConversationStart(Transform actor)
    {
        // 会在第一句字幕出现前触发
        ShowBackground();
    }

    public void OnConversationEnd(Transform actor)
    {
        HideBackground();
    }

    // 如果中途被脚本/切场景强制中断，会回调这里
    public void OnConversationCancelled(Transform actor)
    {
        HideBackground();
    }

    // —— 对外方法，也可被 UnityEvent 调 —— 
    public void ShowBackground()
    {
        if (_visible) return;
        _visible = true;
        background.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(SlideTo(shownPos, showDuration));
    }

    public void HideBackground()
    {
        if (!_visible) return;
        _visible = false;
        StopAllCoroutines();
        StartCoroutine(SlideOutThenDisable());
    }

    IEnumerator SlideTo(Vector2 target, float dur)
    {
        float t0 = Time.unscaledTime;
        Vector2 start = background.anchoredPosition;
        while (Time.unscaledTime - t0 < dur)
        {
            float t = (Time.unscaledTime - t0) / dur;
            background.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }
        background.anchoredPosition = target;
    }

    IEnumerator SlideOutThenDisable()
    {
        yield return SlideTo(hiddenPos, hideDuration);
        background.gameObject.SetActive(false);
    }
}
