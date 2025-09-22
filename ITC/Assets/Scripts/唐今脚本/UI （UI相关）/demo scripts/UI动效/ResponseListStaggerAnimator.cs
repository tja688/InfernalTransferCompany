using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResponseListStaggerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject menuPanel;              // Response Menu Panel（可能被隐藏/激活）
    [SerializeField] Transform content;                 // ScrollRect Content
    [SerializeField] VerticalLayoutGroup layout;        // Content上的VLG（可选，用于重建布局）

    [Header("Entrance Motion")]
    [Tooltip("从目标位置向下偏移多少像素作为起点（负值=从下方飞入）")]
    [SerializeField] float enterOffsetY = -60f;
    [Tooltip("单个条目入场时长（秒）")]
    [SerializeField] float enterDuration = 0.28f;
    [Tooltip("相邻条目错峰延迟（秒）")]
    [SerializeField] float itemStagger = 0.035f;

    public enum EasePreset { EaseOutCubic, EaseOutQuint, EaseOutSine, Linear }
    [Header("Easing Preset")]
    [SerializeField] EasePreset ease = EasePreset.EaseOutCubic;

    [Header("Interaction")]
    [SerializeField] bool lockInteractableDuringAnim = true;

    // 内部
    Coroutine playing;
    int playId;

    // 供 Standard UI Menu Panel → On Content Changed() 调用
    public void Play()
    {
        playId++;

        // 面板可能还没激活；等它激活 + 子项生成完
        if (playing != null) StopCoroutine(playing);
        playing = StartCoroutine(CoPlayWhenReady(playId));
    }

    IEnumerator CoPlayWhenReady(int id)
    {
        // 等面板激活
        if (menuPanel)
            yield return new WaitUntil(() => menuPanel.activeInHierarchy);

        // 等一两帧让布局/克隆完成
        yield return new WaitForEndOfFrame();
        if (content.childCount == 0) yield return new WaitForEndOfFrame();

        // 如果这期间触发了新一轮，旧的直接退出
        if (id != playId) yield break;

        // 重建一次布局，拿到稳定的锚点位置
        if (layout) LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());

        // 收集项并初始化
        var items = new List<RectTransform>(content.childCount);
        for (int i = 0; i < content.childCount; i++)
        {
            var rt = content.GetChild(i) as RectTransform;
            if (!rt) continue;
            items.Add(rt);

            var animRoot = FindAnimRoot(rt);
            var cg = animRoot.GetComponent<CanvasGroup>() ?? animRoot.gameObject.AddComponent<CanvasGroup>();

            // 初始化：透明 + 下偏移；缩放保持1（不做抖动）
            cg.alpha = 0f;
            var lp = animRoot.anchoredPosition;
            animRoot.anchoredPosition = new Vector2(lp.x, lp.y + enterOffsetY);
            animRoot.localScale = Vector3.one;

            if (lockInteractableDuringAnim)
            {
                var btn = rt.GetComponent<Button>();
                if (btn) btn.interactable = false;
            }
        }

        // 逐项错峰入场
        for (int i = 0; i < items.Count; i++)
        {
            if (id != playId) yield break; // 新一轮触发则停止旧动画
            StartCoroutine(CoEnterOne(FindAnimRoot(items[i])));
            yield return new WaitForSecondsRealtime(itemStagger);
        }

        // 等最后一项
        yield return new WaitForSecondsRealtime(enterDuration + 0.02f);

        // 恢复交互
        if (lockInteractableDuringAnim)
        {
            foreach (var rt in items)
            {
                var btn = rt.GetComponent<Button>();
                if (btn) btn.interactable = true;
            }
        }

        playing = null;
    }

    IEnumerator CoEnterOne(RectTransform animRoot)
    {
        var cg = animRoot.GetComponent<CanvasGroup>();
        Vector2 startPos = animRoot.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, 0f);

        var curve = GetCurve(ease);

        float t = 0f;
        while (t < enterDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / enterDuration);
            float e = curve.Evaluate(k);

            animRoot.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, e);
            cg.alpha = e; // 简单跟随（可按需加延迟）
            yield return null;
        }
        // 精确归位
        animRoot.anchoredPosition = endPos;
        cg.alpha = 1f;
        animRoot.localScale = Vector3.one;
    }

    AnimationCurve GetCurve(EasePreset p)
    {
        switch (p)
        {
            case EasePreset.EaseOutQuint:
                // y = 1 - (1-x)^5
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 5f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );
            case EasePreset.EaseOutSine:
                // y = sin(x * PI/2)
                // 近似曲线
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 1.57f, 1.57f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );
            case EasePreset.Linear:
                return AnimationCurve.Linear(0, 0, 1, 1);
            case EasePreset.EaseOutCubic:
            default:
                // y = 1 - (1-x)^3
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 3f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );
        }
    }

    RectTransform FindAnimRoot(RectTransform item)
    {
        // 优先找名为 AnimRoot 的子
        var t = item.Find("AnimRoot") as RectTransform;
        if (t) return t;
        // 次选：第一个子
        if (item.childCount > 0 && item.GetChild(0) is RectTransform c) return c;
        // 兜底：自己（不推荐，但防止空引用）
        return item;
    }
}
