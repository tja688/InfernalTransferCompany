using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(RectTransform))]
public class TransitionSlice : MonoBehaviour
{
    [Header("Wires (Prefab 已配置)")]
    public RawImage videoSurface;
    public VideoPlayer videoPlayer;

    [Header("Options")]
    public bool waitForFirstFrame = true;
    public bool mute = true;

    RectTransform rt;
    AnimationCurve curve = AnimationCurve.Linear(0,0,1,1);
    float duration = 0.6f;
    float prepareTimeout = 3f;
    bool debugLogs = true;

    Vector2 inFrom, inTo, outTo;

    bool prepared;
    bool playing;
    bool videoFinished;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        if (videoSurface && !videoSurface.texture && videoPlayer && videoPlayer.targetTexture)
            videoSurface.texture = videoPlayer.targetTexture;

        if (videoPlayer)
        {
            videoPlayer.waitForFirstFrame = waitForFirstFrame;
            videoPlayer.skipOnDrop = true;
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            if (mute) videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.loopPointReached  -= OnVideoEnd;
            videoPlayer.loopPointReached  += OnVideoEnd;
        }
    }

    public void Configure(AnimationCurve ease, float slideSeconds, float prepareTO, bool logs)
    {
        curve = ease != null ? ease : AnimationCurve.Linear(0,0,1,1);
        duration = Mathf.Max(0.01f, slideSeconds);
        prepareTimeout = Mathf.Max(0.1f, prepareTO);
        debugLogs = logs;
    }

    public void SetPositions(Vector2 inStart, Vector2 inEnd, Vector2 outEnd)
    {
        inFrom = inStart; inTo = inEnd; outTo = outEnd;
        rt.anchoredPosition = inFrom;
    }

    public IEnumerator PlayIn()  => Move(inFrom, inTo, duration);
    public IEnumerator PlayOut() => Move(inTo,   outTo, duration);

    IEnumerator Move(Vector2 a, Vector2 b, float d)
    {
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d);
            float s = curve.Evaluate(k);
            rt.anchoredPosition = Vector2.LerpUnclamped(a, b, s);
            yield return null;
        }
        rt.anchoredPosition = b;
    }

    public void PlayConfigured()
    {
        if (!videoPlayer)
        {
            LogError("未绑定 VideoPlayer。");
            videoFinished = true; prepared = true; return;
        }
        if (videoPlayer.clip == null)
        {
            LogError("VideoPlayer 未设置 Clip。");
            videoFinished = true; prepared = true; return;
        }

        videoFinished = false;
        prepared = false;
        playing = true;

        StartCoroutine(Co_PlayPreparedWithTimeout());
    }

    IEnumerator Co_PlayPreparedWithTimeout()
    {
        if (debugLogs) Debug.Log("[TransitionSlice] 开始 Prepare()");
        videoPlayer.Prepare();

        float t = 0f;
        while (!prepared && t < prepareTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // 不管是超时还是正常完成，都在这里统一调用 Play()
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.time = 0.0;
            videoPlayer.Play();
            if (debugLogs) Debug.Log("[TransitionSlice] Play 启动。");
        }
        else
        {
            if (debugLogs) Debug.Log("[TransitionSlice] 已在播放，跳过重复 Play。");
        }
    }

    void OnPrepared(VideoPlayer _)
    {
        prepared = true;
        if (debugLogs) Debug.Log("[TransitionSlice] Prepare 完成。");
        // ❌ 不要在这里调用 Play()
    }


    void OnVideoEnd(VideoPlayer _)
    {
        playing = false;
        videoFinished = true;
        if (debugLogs) Debug.Log("[TransitionSlice] 视频自然结束。");
    }

    /// 到“片尾前 lead 秒”的时间点；若拿不到长度，立即返回（让上层可激活）
    public IEnumerator WaitUntilPreActivatePoint(float leadSeconds)
    {
        // 等能拿到时长或准备完成（多数平台 prepare 后 length 才可靠）
        float t = 0f;
        while (!prepared && t < prepareTimeout) { t += Time.unscaledDeltaTime; yield return null; }

        double len = videoPlayer && videoPlayer.clip ? videoPlayer.length : 0.0;
        if (len <= 0.001)
        {
            if (debugLogs) Debug.LogWarning("[TransitionSlice] 无法获取视频长度，将立即允许预激活。");
            yield break;
        }

        double triggerTime = Mathf.Max(0f, (float)(len - Mathf.Max(0f, leadSeconds)));
        if (debugLogs) Debug.Log($"[TransitionSlice] 预激活触发点: {triggerTime:F3}s / 长度 {len:F3}s");

        while (playing && videoPlayer.time < triggerTime)
            yield return null;
    }

    public IEnumerator WaitForVideoFinished()
    {
        while (!videoFinished) yield return null;
    }

    void LogError(string msg) { if (debugLogs) Debug.LogError("[TransitionSlice] " + msg); }
}
