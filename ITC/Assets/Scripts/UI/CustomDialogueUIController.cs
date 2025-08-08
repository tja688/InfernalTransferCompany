using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 自定义对话 UI 总控（预设动效版）
/// - 字幕(Text Panel)：将要出选项时上提 → 选项关闭时落回；可选预设曲线
/// - 选项：从下入场（错峰）、点击任一项后全部退场；可选预设曲线
/// - 不干涉层级/背景；字幕初始位置自动记录
///
/// UnityEvents：
///   Standard UI Menu Panel:
///     - On Open()            -> OnResponseMenuOpen()
///     - On Close()           -> OnResponseMenuClose()
///     - On Content Changed() -> OnResponsesContentChanged()
/// </summary>
public class CustomDialogueUIControllerSimple : MonoBehaviour
{
    // ---------- 预设 ----------
    public enum EasePreset
    {
        EaseOutCubic,
        EaseOutQuint,
        EaseOutSine,
        EaseOutBack,     // 轻微过冲（更有弹性）
        EaseInCubic,
        EaseInQuint,
        EaseInSine,
        Linear,
        Custom
    }

    // ---------- 引用 ----------
    [Header("References")]
    [SerializeField] GameObject menuPanel;          // Response Menu Panel
    [SerializeField] Transform content;             // Response Button Panel/Viewport/Content
    [SerializeField] RectTransform subtitleRoot;    // Text Panel（整体移动它）

    // ---------- 字幕（上提/落回） ----------
    [Header("Subtitle Lift")]
    [Tooltip("字幕上提距离（像素，正值=向上）")]
    [SerializeField] float liftDistance = 120f;
    [Tooltip("字幕移动时长(秒)")]
    [SerializeField] float subtitleMoveTime = 0.22f;
    [Tooltip("字幕动效预设")]
    [SerializeField] EasePreset subtitleEase = EasePreset.EaseOutCubic;
    [Tooltip("当选择 Custom 时使用这条曲线")]
    [SerializeField] AnimationCurve subtitleCustomCurve =
        new AnimationCurve(new Keyframe(0,0,0,3), new Keyframe(1,1,0,0));
    [Tooltip("字幕到位后，延迟多少秒再让选项入场")]
    [SerializeField] float waitAfterLiftBeforeResponses = 0.02f;

    // ---------- 选项 入场 ----------
    [Header("Response Entrance (下→上入场)")]
    [SerializeField] float enterOffsetY = -60f;     // 起点相对目标的向下偏移
    [SerializeField] float enterDuration = 0.28f;
    [SerializeField] float itemStagger = 0.035f;
    [SerializeField] EasePreset enterEase = EasePreset.EaseOutCubic;
    [SerializeField] AnimationCurve enterCustomCurve =
        new AnimationCurve(new Keyframe(0,0,0,3), new Keyframe(1,1,0,0));
    [SerializeField] bool lockInteractableDuringEnter = true;

    // ---------- 选项 退场 ----------
    [Header("Response Exit (统一退场)")]
    [SerializeField] float exitDuration = 0.18f;
    [SerializeField] float exitSlideY = 40f;        // 向下滑出
    [SerializeField] EasePreset exitEase = EasePreset.EaseInCubic;
    [SerializeField] AnimationCurve exitCustomCurve =
        new AnimationCurve(new Keyframe(0,0,3,3), new Keyframe(1,1,0,0));
    [SerializeField] bool lockInteractableOnExit = true;

    // ---------- 内部 ----------
    Vector2 dockedPos;     // 自动记录的字幕初始位置
    bool dockedCaptured;
    Coroutine playingEnter;
    int playToken;

    void Awake()
    {
        if (subtitleRoot)
        {
            dockedPos = subtitleRoot.anchoredPosition; // 自动记录
            dockedCaptured = true;
        }
    }

    // ========== UnityEvent：将要显示选项 ==========
    public void OnResponseMenuOpen()
    {
        StartCoroutine(CoLiftThenEnter());
    }

    // ========== UnityEvent：选项关闭 ==========
    public void OnResponseMenuClose()
    {
        if (!subtitleRoot || !dockedCaptured) return;
        StopCoroutine(nameof(CoMoveSubtitle));
        StartCoroutine(CoMoveSubtitle(GetPinnedPos(), dockedPos, subtitleMoveTime, GetCurve(subtitleEase, subtitleCustomCurve)));
    }

    // ========== UnityEvent：选项生成完成（给按钮绑退场） ==========
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

    // ================= 流程实现 =================

    IEnumerator CoLiftThenEnter()
    {
        if (!subtitleRoot || !dockedCaptured) yield break;

        // 字幕从 docked -> pinned
        yield return CoMoveSubtitle(dockedPos, GetPinnedPos(), subtitleMoveTime, GetCurve(subtitleEase, subtitleCustomCurve));

        // 稍等再入场
        yield return new WaitForSecondsRealtime(waitAfterLiftBeforeResponses);
        PlayResponsesEntrance();
    }

    Vector2 GetPinnedPos() => dockedPos + new Vector2(0f, liftDistance);

    IEnumerator CoMoveSubtitle(Vector2 from, Vector2 to, float duration, AnimationCurve curve)
    {
        subtitleRoot.anchoredPosition = from; // 避免重入残留

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = (curve != null) ? curve.Evaluate(k) : k;
            subtitleRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            yield return null;
        }
        subtitleRoot.anchoredPosition = to;
    }

    void PlayResponsesEntrance()
    {
        playToken++;
        int token = playToken;

        if (playingEnter != null) StopCoroutine(playingEnter);
        playingEnter = StartCoroutine(CoEntrance(token));
    }

    IEnumerator CoEntrance(int token)
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

        // 逐项错峰入场
        for (int i = 0; i < items.Count; i++)
        {
            if (token != playToken) yield break;
            StartCoroutine(CoEnterOne(items[i].rt, items[i].cg, curve));
            yield return new WaitForSecondsRealtime(itemStagger);
        }

        // 等最后一项
        yield return new WaitForSecondsRealtime(enterDuration + 0.02f);

        // 放开交互
        foreach (var it in items) if (it.btn) it.btn.interactable = true;

        playingEnter = null;
    }

    IEnumerator CoEnterOne(RectTransform animRoot, CanvasGroup cg, AnimationCurve curve)
    {
        Vector2 start = animRoot.anchoredPosition;
        Vector2 end   = new Vector2(start.x, 0f);

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

    void OnAnyResponseClicked()
    {
        StartCoroutine(CoExitAllResponses());
        // 不拦 DS 默认跳转
    }

    IEnumerator CoExitAllResponses()
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

    // ---------- 工具 ----------
    RectTransform FindAnimRoot(RectTransform item)
    {
        var t = item.Find("Animator Root") as RectTransform; // 你的命名
        if (t) return t;
        t = item.Find("AnimRoot") as RectTransform;
        if (t) return t;
        if (item.childCount > 0 && item.GetChild(0) is RectTransform c) return c;
        return item;
    }

    AnimationCurve GetCurve(EasePreset p, AnimationCurve custom)
    {
        switch (p)
        {
            case EasePreset.EaseOutQuint:
                // y = 1 - (1-x)^5
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 5f),
                    new Keyframe(1f, 1f, 0f, 0f));
            case EasePreset.EaseOutSine:
                // 近似：sin(x * PI/2)
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 1.57f, 1.57f),
                    new Keyframe(1f, 1f, 0f, 0f));
            case EasePreset.EaseOutBack:
                // 轻微过冲：OutBack(s=1.1)
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 3f),
                    new Keyframe(0.8f, 1.06f, 0f, 0f),
                    new Keyframe(1f, 1f, 0f, 0f));
            case EasePreset.EaseInCubic:
                // y = x^3
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(1f, 1f, 3f, 0f));
            case EasePreset.EaseInQuint:
                // y = x^5
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(1f, 1f, 5f, 0f));
            case EasePreset.EaseInSine:
                // 近似：1 - cos(x * PI/2)
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 1.57f),
                    new Keyframe(1f, 1f, 0f, 0f));
            case EasePreset.Linear:
                return AnimationCurve.Linear(0, 0, 1, 1);
            case EasePreset.Custom:
                return custom;
            case EasePreset.EaseOutCubic:
            default:
                // y = 1 - (1-x)^3
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 3f),
                    new Keyframe(1f, 1f, 0f, 0f));
        }
    }
}
