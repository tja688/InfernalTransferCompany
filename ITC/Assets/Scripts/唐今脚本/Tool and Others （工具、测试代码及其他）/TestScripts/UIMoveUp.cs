using UnityEngine;

// 确保该脚本挂载的对象有 RectTransform 组件
[RequireComponent(typeof(RectTransform))]
public class UIMoveUp : MonoBehaviour
{
    // 公开变量，可以在 Unity 编辑器的 Inspector 面板中进行设置
    [Tooltip("UI元素每秒向上移动的速度（单位：像素/秒）")]
    public float moveSpeed = 50f;

    // 私有变量，用于存储UI对象的 RectTransform 组件
    private RectTransform rectTransform;

    // Start is called before the first frame update
    void Start()
    {
        // 获取挂载此脚本的UI对象的 RectTransform 组件
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        // 计算这一帧应该移动的距离 (速度 * 时间)
        // Time.deltaTime 保证了在不同帧率的设备上移动速度保持一致
        float distanceToMove = moveSpeed * Time.deltaTime;

        // 更新 RectTransform 的 anchoredPosition
        // 我们只改变 Y 轴的值，保持 X 轴不变
        rectTransform.anchoredPosition += new Vector2(0, distanceToMove);
    }
}