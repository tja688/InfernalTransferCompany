using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 讓 UI RawImage 使用磨砂玻璃著色器並保證來自攝像機的模糊紋理可用。
/// </summary>
[RequireComponent(typeof(RawImage))]
public class UIFrostedGlassController : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int FrostTexId = Shader.PropertyToID("_FrostTex");
    private static readonly int FrostIntensityId = Shader.PropertyToID("_FrostIntensity");
    private static readonly int DistortionId = Shader.PropertyToID("_Distortion");
    private static readonly int BlurStrengthId = Shader.PropertyToID("_BlurStrength");

    [Tooltip("指向提供模糊紋理的攝像機。如果留空則嘗試使用 Camera.main。")]
    public Camera sourceCamera;

    [Tooltip("覆寫默認的 UI/FrostedGlass 著色器。通常保持默認即可。")]
    public Shader overrideShader;

    [Tooltip("磨砂噪聲紋理，RGB 通道將被用於扭曲取樣。")]
    public Texture frostTexture;

    [Tooltip("調整模糊貼圖的混合權重，0 為最清晰，3 為最模糊。")]
    [Range(0f, 3f)]
    public float blurStrength = 1.5f;

    [Tooltip("調整噪聲對模糊層次的影響程度。")]
    [Range(0f, 1f)]
    public float frostIntensity = 0.6f;

    [Tooltip("控制噪聲對採樣位置的扭曲幅度。")]
    [Range(0f, 1f)]
    public float distortion = 0.25f;

    [Tooltip("額外的色彩與透明度控制。")]
    public Color tintColor = new Color(1f, 1f, 1f, 0.65f);

    private RawImage _rawImage;
    private Material _runtimeMaterial;
    private CommandBufferBlur _blurProvider;

    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
    }

    void OnEnable()
    {
        EnsureResources();
        ApplyProperties();
        _rawImage.SetMaterialDirty();
    }

    void OnDisable()
    {
        ReleaseMaterial();
    }

    void OnDestroy()
    {
        ReleaseMaterial();
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        EnsureResources();
        ApplyProperties();
        if (_rawImage != null)
        {
            _rawImage.SetMaterialDirty();
        }
    }

    /// <summary>
    /// 在運行時手動刷新屬性，當參數通過腳本修改後可調用。
    /// </summary>
    public void Refresh()
    {
        if (!isActiveAndEnabled)
            return;

        ApplyProperties();
        if (_rawImage != null)
        {
            _rawImage.SetMaterialDirty();
        }
    }

    private void EnsureResources()
    {
        if (_rawImage == null)
            _rawImage = GetComponent<RawImage>();

        EnsureMaterial();
        EnsureBlurProvider();
    }

    private void EnsureMaterial()
    {
        if (_runtimeMaterial != null)
            return;

        Shader shader = overrideShader != null ? overrideShader : Shader.Find("UI/FrostedGlass");
        if (shader == null)
        {
            Debug.LogError("未能找到 UI/FrostedGlass 著色器，請確保文件已導入。", this);
            enabled = false;
            return;
        }

        _runtimeMaterial = new Material(shader)
        {
            name = "UIFrostedGlass (Instance)"
        };

        if (_rawImage != null)
        {
            _rawImage.material = _runtimeMaterial;
        }
    }

    private void EnsureBlurProvider()
    {
        Camera targetCamera = sourceCamera != null ? sourceCamera : Camera.main;
        if (targetCamera == null)
        {
            Debug.LogWarning("UIFrostedGlassController 找不到攝像機，模糊紋理無法更新。", this);
            return;
        }

        _blurProvider = targetCamera.GetComponent<CommandBufferBlur>();
        if (_blurProvider == null)
        {
            _blurProvider = targetCamera.gameObject.AddComponent<CommandBufferBlur>();
        }
    }

    private void ApplyProperties()
    {
        if (_runtimeMaterial == null)
            return;

        _runtimeMaterial.SetColor(ColorId, tintColor);
        _runtimeMaterial.SetFloat(FrostIntensityId, frostIntensity);
        _runtimeMaterial.SetFloat(DistortionId, distortion);
        _runtimeMaterial.SetFloat(BlurStrengthId, blurStrength);

        Texture textureToUse = frostTexture != null ? frostTexture : Texture2D.whiteTexture;
        _runtimeMaterial.SetTexture(FrostTexId, textureToUse);
    }

    private void ReleaseMaterial()
    {
        if (_runtimeMaterial == null)
            return;

        if (_rawImage != null && _rawImage.material == _runtimeMaterial)
        {
            _rawImage.material = null;
        }

        if (Application.isPlaying)
            Destroy(_runtimeMaterial);
        else
            DestroyImmediate(_runtimeMaterial);

        _runtimeMaterial = null;
    }
}






