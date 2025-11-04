using UnityEngine;
using UnityEngine.UI;

// 这个脚本需要一个简单的模糊Shader（可以从网上找一个，例如"Unity Blit Blur Shader"）
// 或者使用现成的模糊工具库

public class StaticBlurBackground : MonoBehaviour
{
    public RawImage blurBackgroundImage;
    public Material blurMaterial; // 这是一个用于模糊的材质
    public float blurAmount = 2f; // 模糊程度

    private RenderTexture capturedScreenRT;

    void OnEnable()
    {
        // 1. 创建一个与屏幕大小相同的RenderTexture
        int width = Screen.width;
        int height = Screen.height;
        if (capturedScreenRT == null || capturedScreenRT.width != width || capturedScreenRT.height != height)
        {
            if (capturedScreenRT != null) capturedScreenRT.Release();
            capturedScreenRT = new RenderTexture(width, height, 0, RenderTextureFormat.Default);
            capturedScreenRT.Create();
        }

        // 2. 截取当前屏幕到RenderTexture
        // 注意：这在某些管线（如URP）中可能需要更复杂的方法，
        // 但最简单的是 ScreenCapture.CaptureScreenshotIntoRenderTexture
        // （然而这个方法可能较慢且有延迟）
        
        // 一个更可靠的方法是让相机渲染一次
        // （这里假设你只有一个主相机）
        Camera mainCamera = Camera.main;
        mainCamera.targetTexture = capturedScreenRT;
        mainCamera.Render();
        mainCamera.targetTexture = null; // 恢复正常渲染

        // 3. 应用模糊
        // Graphics.Blit 是最简单的方法，它将一个纹理通过一个材质（Shader）“画”到另一个纹理上
        RenderTexture blurredRT = RenderTexture.GetTemporary(width, height, 0, capturedScreenRT.format);
        
        // 假设你的blurMaterial有一个控制模糊迭代或半径的属性
        if (blurMaterial != null)
        {
            blurMaterial.SetFloat("_BlurSize", blurAmount); // 示例属性
            Graphics.Blit(capturedScreenRT, blurredRT, blurMaterial);
        }
        else
        {
            // 如果没有模糊材质，就先用原图（至少功能能跑通）
            Graphics.Blit(capturedScreenRT, blurredRT); 
        }

        // 4. 将模糊后的纹理显示在UI上
        blurBackgroundImage.texture = blurredRT;
        blurBackgroundImage.color = Color.white; // 确保RawImage可见

        // 注意：我们没有释放 capturedScreenRT，因为 blurredRT 是临时的
        // 我们也不能释放 blurredRT，因为它正被UI使用
        // 我们需要在 OnDisable 中释放它们
    }

    void OnDisable()
    {
        // 释放RenderTexture以防内存泄漏
        if (blurBackgroundImage.texture != null)
        {
            RenderTexture.ReleaseTemporary((RenderTexture)blurBackgroundImage.texture);
            blurBackgroundImage.texture = null;
        }
        if (capturedScreenRT != null)
        {
            capturedScreenRT.Release();
            Destroy(capturedScreenRT);
            capturedScreenRT = null;
        }
    }
}