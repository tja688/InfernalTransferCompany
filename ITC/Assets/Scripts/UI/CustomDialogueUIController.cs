using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 极简自定义对话UI总控：
/// - 选项从下飞入；点击任意选项→所有选项统一退场（包含被点的）
/// - 字幕(Text Panel)在“将要出选项”时上提 liftDistance，收起时落回初始位置
/// - 不干涉任何层级/背景；初始位置自动记录
/// 事件：
///   Standard UI Menu Panel:
///     - On Open()            -> OnResponseMenuOpen()
///     - On Close()           -> OnResponseMenuClose()
///     - On Content Changed() -> OnResponsesContentChanged()
/// </summary>
public class CustomDialogueUIControllerSimple : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject menuPanel;   // Response Menu Panel
    [SerializeField] Transform content;      // Response Button Panel/Viewport/Content
    [SerializeField] RectTransform subtitleRoot; // Text Panel（整体移动它）

    [Header("Subtitle Lift")]
    [Tooltip("字幕上提距离（像素，正值=向上）")]
    [SerializeField] float liftDistance = 120f;
    [Tooltip("字幕移动时长(秒)")]
    [SerializeField] float subtitleMoveTime = 0.22f;
    [Tooltip("字幕移动曲线（默认 EaseOutCubic）")]
    [SerializeField] AnimationCurve subtitleCurve =
        new AnimationCurve(new Keyframe(0,0,0,3), new Keyframe(1,1,0,0));
    [Tooltip("字幕到位后，延迟多少秒再让选项入场")]
    [SerializeField] float waitAfterLiftBeforeResponses = 0.02f;

    [Header("Response Entrance (下→上入场)")]
    [SerializeField] float enterOffsetY = -60f;     // 起点相对目标的向下偏移
    [SerializeField] float enterDuration = 0.28f;
    [SerializeField] float itemStagger = 0.035f;
    [SerializeField] AnimationCurve enterCurve =
        new AnimationCurve(new Keyframe(0,0,0,3), new Keyframe(1,1,0,0));
    [SerializeField] bool lockInteractableDuringEnter = true;

    [Header("Response Exit (统一退场)")]
    [SerializeField] float exitDuration = 0.18f;
    [SerializeField] float exitSlideY = 40f;        // 向下滑出
    [SerializeField] AnimationCurve exitCurve =
        new AnimationCurve(new Keyframe(0,0,3,3), new Keyframe(1,1,0,0));
    [SerializeField] bool lockInteractableOnExit = true;

    // internal
    Vector2 dockedPos;     // 自动记录的字幕初始位置
    bool dockedCaptured;
    Coroutine playingEnter;
    int playToken;

    void Awake()
    {
        // 自动记录字幕初始位置
        if (subtitleRoot)
        {
            dockedPos = subtitleRoot.anchoredPosition;
            dockedCaptured = true;
        }
    }

    // ---------- UnityEvent：将要显示选项 ----------
    public void OnResponseMenuOpen()
    {
        // 上提字幕 -> 等一丢丢 -> 让选项入场
        StartCoroutine(CoLiftThenEnter());
    }

    // ---------- UnityEvent：选项关闭 ----------
    public void OnResponseMenuClose()
    {
        if (!subtitleRoot || !dockedCaptured) return;
        StopCoroutine(nameof(CoMoveSubtitle));
        StartCoroutine(CoMoveSubtitle(GetPinnedPos(), dockedPos, subtitleMoveTime, subtitleCurve));
    }

    // ---------- UnityEvent：选项生成完成 ----------
    public void OnResponsesContentChanged()
    {
        // 给每个按钮绑“统一退场”
        foreach (Transform child in content)
        {
            var btn = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>();
            if (!btn) continue;
            btn.onClick.RemoveListener(OnAnyResponseClicked);
            btn.onClick.AddListener(OnAnyResponseClicked);
        }
    }

    // ================= 实现 =================

    IEnumerator CoLiftThenEnter()
    {
        if (!subtitleRoot || !dockedCaptured) yield break;

        // 字幕从 docked -> pinned
        yield return CoMoveSubtitle(dockedPos, GetPinnedPos(), subtitleMoveTime, subtitleCurve);

        // 稍等再入场
        yield return new WaitForSecondsRealtime(waitAfterLiftBeforeResponses);
        PlayResponsesEntrance();
    }

    Vector2 GetPinnedPos() => dockedPos + new Vector2(0f, liftDistance);

    IEnumerator CoMoveSubtitle(Vector2 from, Vector2 to, float duration, AnimationCurve curve)
    {
        // 立即设起点（避免重入残留）
        subtitleRoot.anchoredPosition = from;

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

        for (int i = 0; i < content.childCount; i++)
        {
            var item = content.GetChild(i) as RectTransform;
            if (!item) continue;

            var animRoot = FindAnimRoot(item);
            var cg = animRoot.GetComponent<CanvasGroup>() ?? animRoot.gameObject.AddComponent<CanvasGroup>();
            var btn = item.GetComponent<Button>() ?? item.GetComponentInChildren<Button>();

            // 初始在下方 & 透明
            var now = animRoot.anchoredPosition;
            animRoot.anchoredPosition = new Vector2(now.x, now.y + enterOffsetY);
            cg.alpha = 0f;

            if (lockInteractableDuringEnter && btn) btn.interactable = false;

            items.Add((animRoot, cg, animRoot.anchoredPosition, new Vector2(now.x, 0f), btn));
        }

        // 逐项错峰
        for (int i = 0; i < items.Count; i++)
        {
            if (token != playToken) yield break;
            StartCoroutine(CoEnterOne(items[i].rt, items[i].cg));
            yield return new WaitForSecondsRealtime(itemStagger);
        }

        // 等最后一项
        yield return new WaitForSecondsRealtime(enterDuration + 0.02f);

        // 放开交互
        foreach (var it in items) if (it.btn) it.btn.interactable = true;

        playingEnter = null;
    }

    IEnumerator CoEnterOne(RectTransform animRoot, CanvasGroup cg)
    {
        Vector2 start = animRoot.anchoredPosition;
        Vector2 end   = new Vector2(start.x, 0f);

        float t = 0f;
        while (t < enterDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / enterDuration);
            float e = enterCurve.Evaluate(k);

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
        // 不拦 DS 的正常跳转
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
            float e = exitCurve.Evaluate(k);

            foreach (var it in items)
            {
                it.rt.anchoredPosition = Vector2.LerpUnclamped(it.start, it.end, e);
                it.cg.alpha = 1f - e;
            }
            yield return null;
        }
    }

    // 找到模板里的“动画容器”
    RectTransform FindAnimRoot(RectTransform item)
    {
        var t = item.Find("Animator Root") as RectTransform; // 你的命名
        if (t) return t;
        t = item.Find("AnimRoot") as RectTransform;
        if (t) return t;
        if (item.childCount > 0 && item.GetChild(0) is RectTransform c) return c;
        return item;
    }
}
