using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 全局模糊管理器 (单例)。
/// 负责协调所有注册了 ManagedBlurSprite 的对象，
/// 统一控制它们的 _BlurRadius 属性。
/// </summary>
public class GlobalBlurManager : MonoBehaviour
{
    // --- 单例 ---
    public static GlobalBlurManager Instance { get; private set; }

    [Header("配置")]
    [Tooltip("要控制的 *原始* 高斯模糊材质 (.mat 文件)")]
    public Material originalBlurMaterial;

    [Tooltip("默认模糊时间")]
    public float duration = 0.5f;

    // --- 内部状态 ---
    private HashSet<ManagedBlurSprite> _managedSprites = new HashSet<ManagedBlurSprite>();
    private int _blurRadiusPropertyID;
    private Coroutine _currentBlurCoroutine;
    private float _currentGlobalBlurValue = 0f; // 当前的全局模糊值

    void Awake()
    {
        // --- 1. 设置单例 ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // (可选) 如果您的管理器需要跨场景，请取消注释下一行
        // DontDestroyOnLoad(gameObject);

        // --- 2. 验证和缓存 ---
        if (originalBlurMaterial == null)
        {
            Debug.LogError("GlobalBlurManager: 'Original Blur Material' 尚未分配！", this);
            enabled = false;
            return;
        }

        _blurRadiusPropertyID = Shader.PropertyToID("_BlurRadius");

        if (!originalBlurMaterial.HasProperty(_blurRadiusPropertyID))
        {
            Debug.LogError($"GlobalBlurManager: 材质 '{originalBlurMaterial.name}' 没有 '_BlurRadius' 属性。", this);
            enabled = false;
            return;
        }

        // --- 3. 重置编辑器状态 ---
        // 重置原始材质，以防上次在编辑器中退出时它是模糊的
        _currentGlobalBlurValue = 0f;
        originalBlurMaterial.SetFloat(_blurRadiusPropertyID, 0f);
    }

    /// <summary>
    /// (公开方法) 动画所有受管理的Sprite的模糊效果。
    /// 可由 Unity Event 调用。
    /// </summary>
    /// <param name="targetBlurRadius">目标模糊半径 (0 = 清晰)</param>
    /// <param name="duration">动画时间 (秒)</param>
    public void AnimateBlur(float targetBlurRadius)
    {
        if (_currentBlurCoroutine != null)
        {
            StopCoroutine(_currentBlurCoroutine);
        }

        if (duration <= 0)
        {
            SetGlobalBlurValue(targetBlurRadius);
        }
        else
        {
            _currentBlurCoroutine = StartCoroutine(Co_AnimateAllBlur(targetBlurRadius, duration));
        }
    }

    /// <summary>
    /// (内部) 协程，用于随时间平滑过渡所有材质实例
    /// </summary>
    private IEnumerator Co_AnimateAllBlur(float targetValue, float duration)
    {
        float startValue = _currentGlobalBlurValue; // 从当前值开始
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / duration);
            float newBlurValue = Mathf.Lerp(startValue, targetValue, t);
            
            SetGlobalBlurValue(newBlurValue); // 更新全局值并应用到所有实例

            yield return null;
        }

        SetGlobalBlurValue(targetValue); // 确保最后设置了精确值
        _currentBlurCoroutine = null;
    }

    /// <summary>
    /// 将当前的全局模糊值应用到所有已注册的 Sprite 实例上
    /// </summary>
    private void SetGlobalBlurValue(float value)
    {
        _currentGlobalBlurValue = value;
        // 遍历所有已注册的 Sprite 并设置它们各自的材质实例
        foreach (ManagedBlurSprite sprite in _managedSprites)
        {
            sprite.UpdateBlurValue(value, _blurRadiusPropertyID);
        }
    }

    // --- 注册/注销 ---

    /// <summary>
    /// 由 ManagedBlurSprite 在 OnEnable 时调用
    /// </summary>
    public void RegisterSprite(ManagedBlurSprite sprite)
    {
        if (sprite != null)
        {
            _managedSprites.Add(sprite);
            // 立即将其设置为当前的全局模糊值
            sprite.UpdateBlurValue(_currentGlobalBlurValue, _blurRadiusPropertyID);
        }
    }

    /// <summary>
    /// 由 ManagedBlurSprite 在 OnDisable 时调用
    /// </summary>
    public void UnregisterSprite(ManagedBlurSprite sprite)
    {
        if (sprite != null)
        {
            _managedSprites.Remove(sprite);
        }
    }
    
    // --- 编辑器清理 ---
    void OnDisable()
    {
        if (originalBlurMaterial != null)
        {
            originalBlurMaterial.SetFloat(_blurRadiusPropertyID, 0f);
        }
    }

    void OnApplicationQuit()
    {
        if (originalBlurMaterial != null)
        {
            originalBlurMaterial.SetFloat(_blurRadiusPropertyID, 0f);
        }
    }
}