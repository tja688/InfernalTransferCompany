using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))] // 确保对象有SpriteRenderer
[ExecuteAlways] // 允许在编辑模式下运行，方便调试
public class SpriteBlurTool : MonoBehaviour
{
    [Header("Blur Settings")]
    [Range(0.0f, 0.1f)] // 限制模糊半径
    public float blurRadius = 0.005f;

    [Range(0, 10)] // 限制模糊迭代次数
    public int blurIterations = 3;

    [Header("Material References")]
    public Material blurMaterial; // 在Inspector中拖入M_SpriteGaussianBlur
    private Material _originalMaterial; // 用于存储原始材质
    private SpriteRenderer _spriteRenderer;

    private bool _isBlurred = false; // 当前是否处于模糊状态

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // 保存原始材质
        if (_spriteRenderer != null && _spriteRenderer.sharedMaterial != null)
        {
            _originalMaterial = _spriteRenderer.sharedMaterial;
        }

        // 如果原始材质是默认的Sprite Default，可能需要创建一个临时材质
        // 避免直接修改系统默认材质
        if (_originalMaterial == null || _originalMaterial.shader.name == "Sprites/Default")
        {
            _originalMaterial = new Material(Shader.Find("Sprites/Default"));
            if(_spriteRenderer.sprite != null)
            {
                _originalMaterial.mainTexture = _spriteRenderer.sprite.texture; // 确保纹理被设置
            }
        }
    }

    void OnEnable()
    {
        // 确保在启用时应用当前设置
        // 确保 _spriteRenderer 已经被初始化
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        ApplyBlurSettings();
    }

    void OnValidate() // 在Inspector中修改参数时调用
    {
        // 确保引用不为空，并在编辑模式下及时更新
        if (blurMaterial == null)
        {
            // 在 OnValidate 中不要使用 LogError，因为它会刷屏
            // Debug.LogError("Blur Material is not assigned! Please assign 'M_SpriteGaussianBlur' to the blurMaterial slot.");
            return;
        }

        // 确保 _spriteRenderer 已经被初始化
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        ApplyBlurSettings();
    }

    /// <summary>
    /// 应用模糊效果。
    /// </summary>
    public void SetBlurred(bool blur = true)
    {
        _isBlurred = blur;
        ApplyBlurSettings();
    }

    /// <summary>
    /// 移除模糊效果，恢复原始图片。
    /// </summary>
    public void SetNormal()
    {
        _isBlurred = false;
        ApplyBlurSettings();
    }

    private void ApplyBlurSettings()
    {
        if (blurMaterial == null || _spriteRenderer == null)
        {
            if (blurMaterial == null && _isBlurred) // 只有在尝试模糊时才报错
            {
                Debug.LogError("Blur Material is not assigned!");
            }
            return;
        }

        // 使用MaterialPropertyBlock以避免每次切换都创建新材质，性能更好
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        if (_isBlurred)
        {
            // 应用模糊材质
            _spriteRenderer.SetPropertyBlock(mpb); // 清除任何旧的PropertyBlock
            _spriteRenderer.material = blurMaterial;
            _spriteRenderer.GetPropertyBlock(mpb); // 获取当前的PropertyBlock
            
            // 设置Shader参数
            mpb.SetFloat("_BlurRadius", blurRadius);
            mpb.SetInt("_BlurIterations", blurIterations);

            _spriteRenderer.SetPropertyBlock(mpb);
        }
        else
        {
            // 恢复原始材质
            _spriteRenderer.material = _originalMaterial;
            _spriteRenderer.SetPropertyBlock(null); // 清除PropertyBlock
        }
    }

    void OnDisable()
    {
        // 在组件禁用时恢复原始材质，确保编辑器状态干净
        if (_spriteRenderer != null) // 检查是否为空
        {
            SetNormal();
        }
    }

    void OnDestroy()
    {
        // 在对象销毁时恢复原始材质，清理
        if (_spriteRenderer != null) // 检查是否为空
        {
            SetNormal();
        }
    }
}