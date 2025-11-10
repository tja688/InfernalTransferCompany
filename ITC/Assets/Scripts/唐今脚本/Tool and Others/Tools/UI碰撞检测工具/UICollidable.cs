using UnityEngine;

/// <summary>
/// (被检测方)
/// 挂载在任何你希望被检测到的 UI 元素上。
/// 它只作为 'UIDetector' 的一个可识别目标。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UICollidable : MonoBehaviour
{
    // 公开 RectTransform 引用，以便 Detector 访问
    [HideInInspector] // 通常不需要在 Inspector 中看到，因为它总等于自己
    public RectTransform rectTransform;

    void Awake()
    {
        // 自动获取自身的 RectTransform
        rectTransform = GetComponent<RectTransform>();
    }
}