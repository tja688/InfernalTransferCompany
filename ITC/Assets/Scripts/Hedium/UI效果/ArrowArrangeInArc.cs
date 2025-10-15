using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArrowArrangeInArc : MonoBehaviour
{
    public GameObject uiPrefab;
    public float radius = 182f;
    public float startAngle = 135f;
    public float endAngle = 45f;

    // 动画参数
    public float fadeDuration = 1f; // 淡入持续时间
    public float startDelay = 0f;   // 每个元素的延迟间隔

    public List<GameObject> spawnedItems = new List<GameObject>();
    private Dictionary<int, float> arrowKeyMap = new Dictionary<int, float>()
    {
        {0,0f},   // 上
        {1,180f}, // 下
        {2,90f},  // 左
        {3,270f}  // 右
    };


    public void ArrangeInArc(List<int> list, float delay)
    {
        StartCoroutine(CopFunc(list, delay));
    }
    private IEnumerator CopFunc(List<int> list,float delay)
    {
        

        //yield return new WaitForSeconds(delay);
        ArrangeInArc_A(list);

        yield return null;

    }
    private void ArrangeInArc_A(List<int> list)
    {
        // 清除现有元素
        foreach (var item in spawnedItems)
        {
            if (item != null)
                Destroy(item);
        }
        spawnedItems.Clear();

        var itemCount = list.Count;
        if (uiPrefab == null || itemCount <= 0)
            return;

        float totalAngle = endAngle - startAngle;
        float angleStep = itemCount > 1 ? totalAngle / (itemCount - 1) : 0;
        Vector2 centerPos = Vector2.zero;

        for (int i = 0; i < itemCount; i++)
        {
            // 计算位置角度
            float currentAngle = startAngle + (i * angleStep);
            float rad = currentAngle * Mathf.Deg2Rad;

            // 计算坐标
            float x = radius * Mathf.Cos(rad);
            float y = radius * Mathf.Sin(rad);
            Vector2 pos = centerPos + new Vector2(x, y);

            // 实例化UI元素
            GameObject item = Instantiate(uiPrefab, transform);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.anchoredPosition = pos;
                itemRect.localEulerAngles = new Vector3(0, 0, arrowKeyMap[list[i]]);
            }

            // 初始化淡入组件并执行淡入
            SetupFadeComponent(item, i);

            spawnedItems.Add(item);
        }
    }

    private void SetupFadeComponent(GameObject uiItem, int index)
    {
        // 获取或添加CanvasGroup
        CanvasGroup canvasGroup = uiItem.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiItem.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0; 
        canvasGroup.interactable = false;

        // 获取或添加FadeUI组件
        FadeUI fadeUI = uiItem.GetComponent<FadeUI>();
        if (fadeUI == null)
        {
            fadeUI = uiItem.AddComponent<FadeUI>();
        }
        fadeUI.canvasGroup = canvasGroup;
        fadeUI.fadeDuration = fadeDuration;

        // 延迟启动淡入
        float delay = index * startDelay;
        StartFadeAfterDelay(fadeUI, delay);
    }

    private void StartFadeAfterDelay(FadeUI fadeUI, float delay)
    {
        
        fadeUI.FadeIn(); // 直接调用淡入方法，而非Toggle
    }
}

// 修正后的FadeUI脚本
public class FadeUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 1.0f;

    // 执行淡入动画
    public void FadeIn()
    {
        StartCoroutine(Fade(1.0f, true));
    }

    // 执行淡出动画
    public void FadeOut()
    {
        StartCoroutine(Fade(0.0f, false));
    }

    // 通用淡入淡出协程
    private IEnumerator Fade(float targetAlpha, bool isFadeIn)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        // 淡入完成后可交互，淡出后不可交互
        canvasGroup.interactable = isFadeIn;
        canvasGroup.blocksRaycasts = isFadeIn;
        EmitToEnableChooseRune();
    }

    private void EmitToEnableChooseRune()
    {
        SlotCenter.Instance.trigger_event(HeEventNames.EnableChooseRuneEvent);
    }
}