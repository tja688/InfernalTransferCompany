using UnityEngine;

[ExecuteInEditMode] // 可以在编辑器中实时看到效果
[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    // 你的目标宽高比 (16:9)
    public float targetAspect = 16.0f / 9.0f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        // 1. 获取当前屏幕的宽高比
        float screenAspect = (float)Screen.width / (float)Screen.height;

        // 2. 计算缩放比例
        float scaleHeight = screenAspect / targetAspect;

        // 3. 创建视口矩形(Viewport Rect)
        Rect viewportRect = new Rect();

        if (scaleHeight < 1.0f)
        {
            // 屏幕更“高” (例如 16:10)，上下加黑边 (Letterbox)
            viewportRect.width = 1.0f;
            viewportRect.height = scaleHeight;
            viewportRect.x = 0;
            viewportRect.y = (1.0f - scaleHeight) / 2.0f;
        }
        else
        {
            // 屏幕更“宽” (例如 21:9)，左右加黑边 (Pillarbox)
            float scaleWidth = 1.0f / scaleHeight;
            viewportRect.width = scaleWidth;
            viewportRect.height = 1.0f;
            viewportRect.x = (1.0f - scaleWidth) / 2.0f;
            viewportRect.y = 0;
        }

        // 4. 应用视口
        cam.rect = viewportRect;
    }
}