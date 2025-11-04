using UnityEngine;

/// <summary>
/// 挂载到使用高斯模糊材质的 Sprite (或 Renderer) 上。
/// 负责创建材质实例，并在启用/禁用时向 GlobalBlurManager 注册/注销自己。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ManagedBlurSprite : MonoBehaviour
{
    private Renderer _renderer;
    private Material _materialInstance; // 材质实例
    private Material _originalMaterial; // 原始共享材质的引用
    private bool _isInitialized = false; // 初始化成功标记

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError("ManagedBlurSprite: 找不到 Renderer 组件！", this);
            enabled = false; // 如果没有Renderer，则彻底禁用
            return;
        }
        
        // 在 Awake 中只获取引用，不做任何检查
        _originalMaterial = _renderer.sharedMaterial;
    }

    // --- 核心修改 ---
    // 我们将所有依赖 GlobalBlurManager 的逻辑移到 Start()
    void Start()
    {
        // Start() 在所有 Awake() 之后运行,
        // 所以此时 GlobalBlurManager.Instance 应该已经可用了。

        if (GlobalBlurManager.Instance != null && 
            _originalMaterial == GlobalBlurManager.Instance.originalBlurMaterial)
        {
            // 1. 创建材质实例
            _materialInstance = _renderer.material;
            
            // 2. 标记初始化成功
            _isInitialized = true;
            
            // 3. (重要) 手动调用一次注册
            // 因为 Start 在 OnEnable 之后运行（如果对象在启动时就是激活的）
            // 我们需要确保它被注册。
            GlobalBlurManager.Instance.RegisterSprite(this);
        }
        else
        {
            // 如果管理器不存在，或材质不匹配，则禁用此脚本
            if (GlobalBlurManager.Instance == null)
                Debug.LogWarning("ManagedBlurSprite: GlobalBlurManager 未找到。此 Sprite 将不会被管理。", this);
            else
                Debug.LogWarning($"ManagedBlurSprite: 此对象使用的材质 '{(_originalMaterial != null ? _originalMaterial.name : "NULL")}' 与管理器设置的 '{GlobalBlurManager.Instance.originalBlurMaterial.name}' 不符。", this);
            
            enabled = false; // 禁用
            _isInitialized = false;
        }
    }

    void OnEnable()
    {
        // 只有在 Start() 成功初始化后才允许注册
        // (这个检查主要是为了防止在 Start 运行前对象被快速禁用又启用)
        if (_isInitialized && GlobalBlurManager.Instance != null)
        {
            GlobalBlurManager.Instance.RegisterSprite(this);
        }
    }

    void OnDisable()
    {
        // 检查实例是否有效
        if (GlobalBlurManager.Instance != null)
        {
            // 从管理器注销自己
            GlobalBlurManager.Instance.UnregisterSprite(this);
        }
        
        // --- 重要的清理 ---
        // 销毁我们创建的材质实例，防止内存泄漏
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
            _materialInstance = null;
        }
        
        // (可选) 将材质恢复为原始材质
        if (_renderer != null && _originalMaterial != null)
        {
            _renderer.sharedMaterial = _originalMaterial;
        }
    }

    /// <summary>
    /// (由 GlobalBlurManager 调用) 更新此材质实例的模糊值
    /// </summary>
    public void UpdateBlurValue(float value, int propertyID)
    {
        if (_isInitialized && _materialInstance != null)
        {
            _materialInstance.SetFloat(propertyID, value);
        }
    }
}