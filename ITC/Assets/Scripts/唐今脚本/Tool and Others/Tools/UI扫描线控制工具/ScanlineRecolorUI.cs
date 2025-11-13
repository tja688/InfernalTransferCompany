using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScanlineRecolorUI : MonoBehaviour
{
    public enum Direction
    {
        LeftToRight,   // 0°
        RightToLeft,   // 180°
        TopToBottom,   // -90°
        BottomToTop,   // 90°
        CustomAngle    // 自定义角度
    }

    [Header("Scan Direction")]
    public Direction direction = Direction.LeftToRight;
    [Range(-180f, 180f)] public float customAngleDeg = 0f;

    [Header("Playback")]
    public bool playOnStart = true;
    public bool loop = true;
    [Min(0.0001f)] public float duration = 2f;   // 完成一次扫描所需时间（秒）
    public bool useEase = true;                  // 是否平滑缓动
    [Range(0f, 1f)] public float manualProgress = 0f; // 手动控制时的进度（0-1）
    public bool useManualProgress = false;            // 勾选后由 manualProgress 控

    [Header("Line Look")]
    [Range(0f, 0.25f)] public float lineSoftness = 0.02f;

    private Image _image;
    private Material _mat;
    private int _idProgress, _idAngle, _idUseEase, _idAutoPlay, _idSpeed, _idSoft;

    private float _t; // 播放计时

    void Awake()
    {
        _image = GetComponent<Image>();
        // 实例化材质，避免改到共享材质
        _mat = Instantiate(_image.material);
        _image.material = _mat;

        _idProgress = Shader.PropertyToID("_Progress");
        _idAngle    = Shader.PropertyToID("_AngleDeg");
        _idUseEase  = Shader.PropertyToID("_UseEase");
        _idAutoPlay = Shader.PropertyToID("_AutoPlay");
        _idSpeed    = Shader.PropertyToID("_Speed");
        _idSoft     = Shader.PropertyToID("_LineSoftness");
    }

    void OnEnable()
    {
        _t = 0f;
        if (playOnStart) _t = 0f;
        ApplyStaticParams();
    }

    void ApplyStaticParams()
    {
        _mat.SetFloat(_idUseEase, useEase ? 1f : 0f);
        _mat.SetFloat(_idSoft, lineSoftness);

        // 把自动播放关掉，由脚本手动写 _Progress（控制更灵活）
        _mat.SetFloat(_idAutoPlay, 0f);

        // 方向 -> 角度
        float angle = customAngleDeg;
        switch (direction)
        {
            case Direction.LeftToRight:  angle = 0f; break;
            case Direction.RightToLeft:  angle = 180f; break;
            case Direction.TopToBottom:  angle = -90f; break;
            case Direction.BottomToTop:  angle = 90f; break;
            case Direction.CustomAngle:  angle = customAngleDeg; break;
        }
        _mat.SetFloat(_idAngle, angle);
    }

    void Update()
    {
        if (useManualProgress)
        {
            _mat.SetFloat(_idProgress, Mathf.Clamp01(manualProgress));
            return;
        }

        if (!playOnStart && _t <= 0f) return;

        _t += Time.unscaledDeltaTime; // UI 通常用不随时间缩放的时间
        float p = _t / Mathf.Max(0.0001f, duration);

        if (loop)
        {
            p = p - Mathf.Floor(p); // 0..1 循环
        }
        else
        {
            p = Mathf.Clamp01(p);
        }

        if (useEase)
        {
            // 简单 S 曲线
            p = p * p * (3f - 2f * p);
        }

        _mat.SetFloat(_idProgress, p);
    }

    // 运行时也可以修改这些参数
    public void SetSourceAndTargetColors(Color src, Color target, float tolerance, float replaceAmount = 1f)
    {
        _mat.SetColor("_SrcColor", src);
        _mat.SetColor("_TargetColor", target);
        _mat.SetFloat("_Tolerance", Mathf.Clamp01(tolerance));
        _mat.SetFloat("_ReplaceAmount", Mathf.Clamp01(replaceAmount));
   }
}