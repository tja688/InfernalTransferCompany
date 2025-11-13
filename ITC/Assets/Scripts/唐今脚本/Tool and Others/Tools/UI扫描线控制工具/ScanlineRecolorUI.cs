using UnityEngine;
using UnityEngine.UI; // 需要引用 UI 命名空间

/// <summary>
/// ScanlineRecolor.shader 的 C# 桥接器。
/// 自动管理材质实例，并提供友好的 API 来控制着色器参数。
/// 
/// 用法：
/// 1. 将此脚本附加到使用了 "UI/ScanlineRecolor" 材质的 GameObject 上（例如一个 Image 组件）。
/// 2. 外部脚本（如长按管理器）可以获取此组件的引用，并直接设置其 C# 属性（如 Progress, TargetColor 等）。
/// </summary>
[RequireComponent(typeof(Graphic))] // 强制要求附加在有 Image, RawImage, Text 等组件的对象上
public class ScanlineRecolorBridge : MonoBehaviour
{
    // --- 私有字段 ---
    private Graphic _graphic;      // 目标 UI 组件 (Image, RawImage, etc.)
    private Material _dynamicMat;  // 动态创建的材质实例

    // --- Shader Property IDs (为了性能) ---
    private static readonly int kSrcColorProp = Shader.PropertyToID("_SrcColor");
    private static readonly int kTargetColorProp = Shader.PropertyToID("_TargetColor");
    private static readonly int kToleranceProp = Shader.PropertyToID("_Tolerance");
    private static readonly int kReplaceAmountProp = Shader.PropertyToID("_ReplaceAmount");
    private static readonly int kAngleDegProp = Shader.PropertyToID("_AngleDeg");
    private static readonly int kProgressProp = Shader.PropertyToID("_Progress");
    private static readonly int kLineSoftnessProp = Shader.PropertyToID("_LineSoftness");
    private static readonly int kAutoPlayProp = Shader.PropertyToID("_AutoPlay");
    private static readonly int kSpeedProp = Shader.PropertyToID("_Speed");
    private static readonly int kUseEaseProp = Shader.PropertyToID("_UseEase");

    // --- 备份的初始值 (用于 Reset) ---
    private Color _initialSrcColor;
    private Color _initialTargetColor;
    private float _initialTolerance;
    private float _initialReplaceAmount;
    private float _initialAngleDeg;
    private float _initialProgress;
    private float _initialLineSoftness;
    private bool  _initialAutoPlay;
    private float _initialSpeed;
    private bool  _initialUseEase;

    // --- 当前值的私有备份字段 ---
    private Color _srcColor;
    private Color _targetColor;
    private float _tolerance;
    private float _replaceAmount;
    private float _angleDeg;
    private float _progress;
    private float _lineSoftness;
    private bool  _autoPlay;
    private float _speed;
    private bool  _useEase;

    #region 公共 API 属性 (Friendly API)

    /// <summary>
    /// (Source Color) 要被替换的源颜色
    /// </summary>
    public Color SourceColor
    {
        get => _srcColor;
        set
        {
            if (_srcColor == value) return;
            _srcColor = value;
            _dynamicMat?.SetColor(kSrcColorProp, _srcColor);
        }
    }

    /// <summary>
    /// (Target Color) 替换后的目标颜色
    /// </summary>
    public Color TargetColor
    {
        get => _targetColor;
        set
        {
            if (_targetColor == value) return;
            _targetColor = value;
            _dynamicMat?.SetColor(kTargetColorProp, _targetColor);
        }
    }

    /// <summary>
    /// (Match Tolerance) 颜色匹配的容差 (0-1)
    /// </summary>
    public float Tolerance
    {
        get => _tolerance;
        set
        {
            if (_tolerance == value) return;
            _tolerance = value;
            _dynamicMat?.SetFloat(kToleranceProp, _tolerance);
        }
    }

    /// <summary>
    /// (Replace Amount) 整体替换强度 (0-1)
    /// </summary>
    public float ReplaceAmount
    {
        get => _replaceAmount;
        set
        {
            if (_replaceAmount == value) return;
            _replaceAmount = value;
            _dynamicMat?.SetFloat(kReplaceAmountProp, _replaceAmount);
        }
    }

    /// <summary>
    /// (Scan Angle) 扫描线角度 (度, -180 到 180)
    /// </summary>
    public float AngleDeg
    {
        get => _angleDeg;
        set
        {
            if (_angleDeg == value) return;
            _angleDeg = value;
            _dynamicMat?.SetFloat(kAngleDegProp, _angleDeg);
        }
    }

    /// <summary>
    /// (Progress) 扫描进度 (0-1)。
    /// 这是你的管理器最可能需要控制的属性。
    /// </summary>
    public float Progress
    {
        get => _progress;
        set
        {
            float clampedValue = Mathf.Clamp01(value); // 确保在 0-1 范围
            if (_progress == clampedValue) return;
            _progress = clampedValue;
            _dynamicMat?.SetFloat(kProgressProp, _progress);
        }
    }

    /// <summary>
    /// (Line Softness) 扫描线边缘的柔和度 (0-0.25)
    /// </summary>
    public float LineSoftness
    {
        get => _lineSoftness;
        set
        {
            if (_lineSoftness == value) return;
            _lineSoftness = value;
            _dynamicMat?.SetFloat(kLineSoftnessProp, _lineSoftness);
        }
    }

    /// <summary>
    /// (Auto Play) 是否启用自动播放 (会覆盖 Progress 属性)
    /// </summary>
    public bool AutoPlay
    {
        get => _autoPlay;
        set
        {
            if (_autoPlay == value) return;
            _autoPlay = value;
            _dynamicMat?.SetFloat(kAutoPlayProp, _autoPlay ? 1.0f : 0.0f);
        }
    }

    /// <summary>
    /// (Speed) 自动播放的速度
    /// </summary>
    public float Speed
    {
        get => _speed;
        set
        {
            if (_speed == value) return;
            _speed = value;
            _dynamicMat?.SetFloat(kSpeedProp, _speed);
        }
    }

    /// <summary>
    /// (Ease In-Out) 自动播放时是否使用缓动
    /// </summary>
    public bool UseEase
    {
        get => _useEase;
        set
        {
            if (_useEase == value) return;
            _useEase = value;
            _dynamicMat?.SetFloat(kUseEaseProp, _useEase ? 1.0f : 0.0f);
        }
    }

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        _graphic = GetComponent<Graphic>();
        
        // 检查是否有材质，如果没有或材质名不匹配，则发出警告
        if (_graphic.material == null || _graphic.material.shader.name != "UI/ScanlineRecolor")
        {
            Debug.LogWarning($"ScanlineRecolorBridge: {gameObject.name} 上的 Graphic 组件没有指定 'UI/ScanlineRecolor' 材质。脚本将不起作用。", this);
            return;
        }

        // --- 核心逻辑 ---
        // 1. 根据当前材质，创建一个新的实例
        _dynamicMat = new Material(_graphic.material);
        
        // 2. 将这个新实例指派回 Graphic 组件
        _graphic.material = _dynamicMat;

        // 3. 读取所有预设值并存储
        ReadAndStoreInitialValues();
    }

    private void OnDestroy()
    {
        // 当此组件被销毁时，我们也应该销毁动态创建的材质实例，防止内存泄漏
        if (_dynamicMat != null)
        {
            // (注意：在编辑器中退出播放模式时，Destroy 可能会被延迟，
            //  但 DestroyImmediate 在这里不是必需的，标准的 Destroy 即可)
            Destroy(_dynamicMat);
        }
        
        // 可选：将材质设置回 null，让 Graphic 使用默认材质
        // if (_graphic != null)
        // {
        //     _graphic.material = null; 
        // }
        // (通常不需要上面这步，因为 Graphic 组件自身被销毁时会处理)
    }

    #endregion

    #region 核心方法

    /// <summary>
    /// 从动态材质实例中读取所有预设值，
    /// 并将它们同时存储到 "初始值" 和 "当前值" 字段中。
    /// </summary>
    private void ReadAndStoreInitialValues()
    {
        if (_dynamicMat == null) return;

        // --- 读取并存储初始值 ---
        _initialSrcColor = _dynamicMat.GetColor(kSrcColorProp);
        _initialTargetColor = _dynamicMat.GetColor(kTargetColorProp);
        _initialTolerance = _dynamicMat.GetFloat(kToleranceProp);
        _initialReplaceAmount = _dynamicMat.GetFloat(kReplaceAmountProp);
        _initialAngleDeg = _dynamicMat.GetFloat(kAngleDegProp);
        _initialProgress = _dynamicMat.GetFloat(kProgressProp);
        _initialLineSoftness = _dynamicMat.GetFloat(kLineSoftnessProp);
        _initialAutoPlay = _dynamicMat.GetFloat(kAutoPlayProp) > 0.5f;
        _initialSpeed = _dynamicMat.GetFloat(kSpeedProp);
        _initialUseEase = _dynamicMat.GetFloat(kUseEaseProp) > 0.5f;

        // --- 同步当前值 ---
        _srcColor = _initialSrcColor;
        _targetColor = _initialTargetColor;
        _tolerance = _initialTolerance;
        _replaceAmount = _initialReplaceAmount;
        _angleDeg = _initialAngleDeg;
        _progress = _initialProgress;
        _lineSoftness = _initialLineSoftness;
        _autoPlay = _initialAutoPlay;
        _speed = _initialSpeed;
        _useEase = _initialUseEase;
    }

    /// <summary>
    /// (公共 API)
    /// 将所有参数重置回附加此脚本时，材质上预设的初始值。
    /// 你的管理器可以在长按取消或完成后调用此方法。
    /// </summary>
    public void ResetToInitialValues()
    {
        SourceColor = _initialSrcColor;
        TargetColor = _initialTargetColor;
        Tolerance = _initialTolerance;
        ReplaceAmount = _initialReplaceAmount;
        AngleDeg = _initialAngleDeg;
        Progress = _initialProgress;
        LineSoftness = _initialLineSoftness;
        AutoPlay = _initialAutoPlay;
        Speed = _initialSpeed;
        UseEase = _initialUseEase;
    }

    #endregion
}