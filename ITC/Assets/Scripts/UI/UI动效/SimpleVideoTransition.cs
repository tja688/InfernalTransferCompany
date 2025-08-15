// SimpleVideoTransition.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class SimpleVideoTransition : MonoBehaviour
{
    [Header("Wires")]
    public Canvas transitionCanvas;          // 你的“转场Canvas”（Screen Space - Overlay，排序设高）
    public RawImage videoSurface;            // 承载视频的 RawImage
    public VideoPlayer videoPlayer;          // 你已挂在 RawImage 或子物体上的 VideoPlayer
    public RenderTexture targetRT;           // VideoPlayer 输出到的 RenderTexture
    public VideoClip transitionClip;         // 用作转场的视频剪辑

    [Header("Timings")]
    public float slideDuration = 0.6f;       // 幕布下落/上收时长
    public float holdAfterCover = 0.1f;      // 完全遮住后停顿（再切场）
    public float holdBeforeUncover = 0.1f;   // 新场景激活后再停顿

    RectTransform _rt;       // RawImage 的 RectTransform
    bool _busy;

    void Awake()
    {
        // 常驻，跨场景保持连贯
        DontDestroyOnLoad(gameObject);
        if (!transitionCanvas || !videoSurface || !videoPlayer)
        {
            Debug.LogError("[SimpleVideoTransition] 请在 Inspector 里把 Canvas/RawImage/VideoPlayer 绑定好。");
        }

        _rt = videoSurface.rectTransform;

        // 绑定输出纹理
        if (targetRT)
        {
            if (videoSurface.texture == null) videoSurface.texture = targetRT;
            if (videoPlayer.targetTexture == null) videoPlayer.targetTexture = targetRT;
        }

        // 建议：转场不循环
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;

        // 初始放屏幕上方、隐藏
        transitionCanvas.enabled = false;
        SetAnchoredY(OffscreenY());
    }

    // —— 对外调用：开始转场 ——
    public void TransitionTo(string sceneName) => TransitionTo(sceneName, transitionClip, 0.0);

    // 可指定 VideoClip 与起始时间（需要“连贯时”可传入上次的 videoPlayer.time）
    public void TransitionTo(string sceneName, VideoClip clip, double startTime)
    {
        if (_busy) return;
        StartCoroutine(Co_Transition(sceneName, clip, startTime));
    }

    IEnumerator Co_Transition(string sceneName, VideoClip clip, double startTime)
    {
        _busy = true;

        // 1) 准备并开播（避免黑闪）
        transitionCanvas.enabled = true;
        yield return PrepareAndPlay(clip, startTime);

        // 2) 下落遮住
        yield return SlideY(OffscreenY(), CoveredY(), slideDuration);
        if (holdAfterCover > 0f) yield return new WaitForSecondsRealtime(holdAfterCover);

        // 3) 异步加载场景（后台）
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;      // 激活
        yield return null;                   // 等一帧，确保新场景 ready
        if (holdBeforeUncover > 0f) yield return new WaitForSecondsRealtime(holdBeforeUncover);

        // 4) 上收离场 & 收尾
        yield return SlideY(CoveredY(), OffscreenY(), slideDuration);

        videoPlayer.Stop();                  // 如需“持续播放”可不 Stop
        transitionCanvas.enabled = false;

        _busy = false;
    }

    // —— 工具函数 ——
    IEnumerator PrepareAndPlay(VideoClip clip, double startTime)
    {
        if (clip) videoPlayer.clip = clip;
        if (videoPlayer.clip == null)
        {
            Debug.LogError("[SimpleVideoTransition] 未设置 VideoClip。");
            yield break;
        }
        videoPlayer.time = Mathf.Max(0f, (float)startTime);
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
        videoPlayer.Play();
    }

    float CoveredY() => 0f;

    float OffscreenY()
    {
        // 把面板移到屏幕上一整屏的高度之外
        var root = transitionCanvas.rootCanvas;
        var h = (root && root.pixelRect.height > 0) ? root.pixelRect.height : Screen.height;
        return h;
    }

    void SetAnchoredY(float y)
    {
        var p = _rt.anchoredPosition;
        p.y = y;
        _rt.anchoredPosition = p;
    }

    IEnumerator SlideY(float fromY, float toY, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            // easeOutCubic：顺滑
            float s = 1f - Mathf.Pow(1f - k, 3f);
            SetAnchoredY(Mathf.LerpUnclamped(fromY, toY, s));
            yield return null;
        }
        SetAnchoredY(toY);
    }

    // 若需要“连贯播放”用这个取上次时间：SimpleVideoTransition.Instance.CurrentTime
    public double CurrentTime => (videoPlayer && videoPlayer.isPlaying) ? videoPlayer.time : 0.0;
}
