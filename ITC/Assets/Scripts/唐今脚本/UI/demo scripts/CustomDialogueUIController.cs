using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 自定义对话 UI 总控（预设动效版）
/// - Subtitle面板与响应按钮：UnityEvent 外部关联
/// - Subtitle image：字幕上提时启用，落回完成后禁用
/// - 背景板：会话生命周期回调（Dialogue System 自动调用），带升降动效
/// </summary>
public class CustomDialogueUIControllerSimple : MonoBehaviour
{
    public enum EasePreset
    {
        EaseOutCubic,
        EaseOutQuint,
        EaseOutSine,
        EaseOutBack,
        EaseInCubic,
        EaseInQuint,
        EaseInSine,
        Linear,
        Custom
    }

    [Header("References")]
    [SerializeField] GameObject menuPanel;
    [SerializeField] Transform content;
    [SerializeField] RectTransform subtitleRoot;

    [Header("Subtitle Image Toggle")]
    [SerializeField] GameObject subtitleImageObject;
    private Image subtitleImage;

    [Header("Subtitle Lift")]
    [SerializeField] float liftDistance = 120f;
    [SerializeField] float subtitleMoveTime = 0.22f;
    [SerializeField] EasePreset subtitleEase = EasePreset.EaseOutCubic;
    [SerializeField] AnimationCurve subtitleCustomCurve =
        new AnimationCurve(new Keyframe(0, 0, 0, 3), new Keyframe(1, 1, 0, 0));
    [SerializeField] float waitAfterLiftBeforeResponses = 0.02f;

    [Header("Response Entrance")]
    [SerializeField] float enterOffsetY = -60f;
    [SerializeField] float enterDuration = 0.28f;
    [SerializeField] float itemStagger = 0.035f;
    [SerializeField] EasePreset enterEase = EasePreset.EaseOutCubic;
    [SerializeField] AnimationCurve enterCustomCurve =
        new AnimationCurve(new Keyframe(0, 0, 0, 3), new Keyframe(1, 1, 0, 0));
    [SerializeField] bool lockInteractableDuringEnter = true;

    [Header("Response Exit")]
    [SerializeField] float exitDuration = 0.18f;
    [SerializeField] float exitSlideY = 40f;
    [SerializeField] EasePreset exitEase = EasePreset.EaseInCubic;
    [SerializeField] AnimationCurve exitCustomCurve =
        new AnimationCurve(new Keyframe(0, 0, 3, 3), new Keyframe(1, 1, 0, 0));
    [SerializeField] bool lockInteractableOnExit = true;

    [Header("Background Panel")]
    [SerializeField] RectTransform background;
    [SerializeField] Vector2 bgShownPos = Vector2.zero;
    [SerializeField] Vector2 bgHiddenPos = new Vector2(0, -800f);
    [SerializeField] float bgShowTime = 0.25f;
    [SerializeField] float bgHideTime = 0.20f;
    [SerializeField] EasePreset bgEase = EasePreset.EaseOutCubic;
    [SerializeField] AnimationCurve bgCustomCurve =
        new AnimationCurve(new Keyframe(0, 0, 0, 3), new Keyframe(1, 1, 0, 0));

    // 内部
    private Vector2 dockedPos;
    private bool dockedCaptured;
    private Coroutine playingEnter;
    private int playToken;

    void Awake()
    {
        if (subtitleRoot)
        {
            dockedPos = subtitleRoot.anchoredPosition;
            dockedCaptured = true;
        }
        if (subtitleImageObject)
        {
            subtitleImage = subtitleImageObject.GetComponent<Image>();
            if (subtitleImage) subtitleImage.enabled = false;
        }
        if (background)
        {
            background.gameObject.SetActive(false);
            background.anchoredPosition = bgHiddenPos;
        }
    }

    // ==== 背景板：会话生命周期回调 ====
    public void OnConversationStart(Transform actor)
    {
        if (background)
        {
            background.gameObject.SetActive(true);
            StopCoroutine(nameof(CoMoveBG));
            StartCoroutine(CoMoveBG(bgHiddenPos, bgShownPos, bgShowTime, GetCurve(bgEase, bgCustomCurve)));
        }
    }

    public void OnConversationEnd(Transform actor)
    {
        HideBackground();
    }

    public void OnConversationCancelled(Transform actor)
    {
        HideBackground();
    }

    private void HideBackground()
    {
        if (background)
        {
            StopCoroutine(nameof(CoMoveBG));
            StartCoroutine(CoMoveBGThenDisable(bgShownPos, bgHiddenPos, bgHideTime, GetCurve(bgEase, bgCustomCurve)));
        }
    }

    // ==== Subtitle & Responses（UnityEvent绑定）====
    public void OnResponseMenuOpen()
    {
        if (subtitleImage) subtitleImage.enabled = true;
        StartCoroutine(CoLiftThenEnter());
    }

    public void OnResponseMenuClose()
    {
        if (!subtitleRoot || !dockedCaptured) return;
        StopCoroutine(nameof(CoMoveSubtitle));
        StartCoroutine(CoMoveSubtitleDownThenDisable(GetPinnedPos(), dockedPos, subtitleMoveTime, GetCurve(subtitleEase, subtitleCustomCurve)));
    }

    public void OnResponsesContentChanged()
    {
        foreach (Transform child in content)
        {
            var btn = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>();
            if (!btn) continue;
            btn.onClick.RemoveListener(OnAnyResponseClicked);
            btn.onClick.AddListener(OnAnyResponseClicked);
        }
    }

    private IEnumerator CoLiftThenEnter()
    {
        if (!subtitleRoot || !dockedCaptured) yield break;
        yield return CoMoveSubtitle(dockedPos, GetPinnedPos(), subtitleMoveTime, GetCurve(subtitleEase, subtitleCustomCurve));
        yield return new WaitForSecondsRealtime(waitAfterLiftBeforeResponses);
        PlayResponsesEntrance();
    }

    private Vector2 GetPinnedPos() => dockedPos + new Vector2(0f, liftDistance);

    private IEnumerator CoMoveSubtitle(Vector2 from, Vector2 to, float duration, AnimationCurve curve)
    {
        subtitleRoot.anchoredPosition = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = curve != null ? curve.Evaluate(k) : k;
            subtitleRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            yield return null;
        }
        subtitleRoot.anchoredPosition = to;
    }

    private IEnumerator CoMoveSubtitleDownThenDisable(Vector2 from, Vector2 to, float duration, AnimationCurve curve)
    {
        yield return CoMoveSubtitle(from, to, duration, curve);
        if (subtitleImage) subtitleImage.enabled = false;
    }

    // 背景板协程
    private IEnumerator CoMoveBG(Vector2 from, Vector2 to, float duration, AnimationCurve curve)
    {
        background.anchoredPosition = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = curve != null ? curve.Evaluate(k) : k;
            background.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            yield return null;
        }
        background.anchoredPosition = to;
    }

    private IEnumerator CoMoveBGThenDisable(Vector2 from, Vector2 to, float duration, AnimationCurve curve)
    {
        yield return CoMoveBG(from, to, duration, curve);
        background.gameObject.SetActive(false);
    }

    private void PlayResponsesEntrance()
    {
        playToken++;
        int token = playToken;
        if (playingEnter != null) StopCoroutine(playingEnter);
        playingEnter = StartCoroutine(CoEntrance(token));
    }

    private IEnumerator CoEntrance(int token)
    {
        var items = new List<(RectTransform rt, CanvasGroup cg, Vector2 start, Vector2 end, Button btn)>(content.childCount);
        var curve = GetCurve(enterEase, enterCustomCurve);

        for (int i = 0; i < content.childCount; i++)
        {
            var item = content.GetChild(i) as RectTransform;
            if (!item) continue;

            var animRoot = FindAnimRoot(item);
            var cg = animRoot.GetComponent<CanvasGroup>() ?? animRoot.gameObject.AddComponent<CanvasGroup>();
            var btn = item.GetComponent<Button>() ?? item.GetComponentInChildren<Button>();

            var now = animRoot.anchoredPosition;
            animRoot.anchoredPosition = new Vector2(now.x, now.y + enterOffsetY);
            cg.alpha = 0f;
            if (lockInteractableDuringEnter && btn) btn.interactable = false;

            items.Add((animRoot, cg, animRoot.anchoredPosition, new Vector2(now.x, 0f), btn));
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (token != playToken) yield break;
            StartCoroutine(CoEnterOne(items[i].rt, items[i].cg, curve));
            yield return new WaitForSecondsRealtime(itemStagger);
        }

        yield return new WaitForSecondsRealtime(enterDuration + 0.02f);
        foreach (var it in items) if (it.btn) it.btn.interactable = true;
        playingEnter = null;
    }

    private IEnumerator CoEnterOne(RectTransform animRoot, CanvasGroup cg, AnimationCurve curve)
    {
        Vector2 start = animRoot.anchoredPosition;
        Vector2 end = new Vector2(start.x, 0f);

        float t = 0f;
        while (t < enterDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / enterDuration);
            float e = curve.Evaluate(k);
            animRoot.anchoredPosition = Vector2.LerpUnclamped(start, end, e);
            cg.alpha = e;
            yield return null;
        }
        animRoot.anchoredPosition = end;
        cg.alpha = 1f;
    }

    private void OnAnyResponseClicked()
    {
        StartCoroutine(CoExitAllResponses());
    }

    private IEnumerator CoExitAllResponses()
    {
        if (lockInteractableOnExit)
        {
            foreach (Transform child in content)
            {
                var b = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>();
                if (b) b.interactable = false;
            }
        }

        var items = new List<(RectTransform rt, CanvasGroup cg, Vector2 start, Vector2 end)>(content.childCount);
        var curve = GetCurve(exitEase, exitCustomCurve);

        foreach (Transform child in content)
        {
            var rt = child as RectTransform;
            if (!rt) continue;
            var animRoot = FindAnimRoot(rt);
            var cg = animRoot.GetComponent<CanvasGroup>() ?? animRoot.gameObject.AddComponent<CanvasGroup>();
            items.Add((animRoot, cg, animRoot.anchoredPosition, animRoot.anchoredPosition - new Vector2(0, exitSlideY)));
        }

        float t = 0f;
        while (t < exitDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / exitDuration);
            float e = curve.Evaluate(k);

            foreach (var it in items)
            {
                it.rt.anchoredPosition = Vector2.LerpUnclamped(it.start, it.end, e);
                it.cg.alpha = 1f - e;
            }
            yield return null;
        }
    }

    private RectTransform FindAnimRoot(RectTransform item)
    {
        var t = item.Find("Animator Root") as RectTransform;
        if (t) return t;
        t = item.Find("AnimRoot") as RectTransform;
        if (t) return t;
        if (item.childCount > 0 && item.GetChild(0) is RectTransform c) return c;
        return item;
    }

    private AnimationCurve GetCurve(EasePreset p, AnimationCurve custom)
    {
        switch (p)
        {
            case EasePreset.EaseOutQuint:
                return new AnimationCurve(new Keyframe(0f, 0f, 0f, 5f), new Keyframe(1f, 1f, 0f, 0f));
            case EasePreset.EaseOutSine:
                return new AnimationCurve(new Keyframe(0f, 0f, 1.57f, 1.57f), new Keyframe(1f, 1f, 0f, 0f));
            case EasePreset.EaseOutBack:
                return new AnimationCurve(new Keyframe(0f, 0f, 0f, 3f), new Keyframe(0.8f, 1.06f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f));
            case EasePreset.EaseInCubic:
                return new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 3f, 0f));
            case EasePreset.EaseInQuint:
                return new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 5f, 0f));
            case EasePreset.EaseInSine:
                return new AnimationCurve(new Keyframe(0f, 0f, 0f, 1.57f), new Keyframe(1f, 1f, 0f, 0f));
            case EasePreset.Linear:
                return AnimationCurve.Linear(0, 0, 1, 1);
            case EasePreset.Custom:
                return custom;
            case EasePreset.EaseOutCubic:
            default:
                return new AnimationCurve(new Keyframe(0f, 0f, 0f, 3f), new Keyframe(1f, 1f, 0f, 0f));
        }
    }
}
