using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;


/// <summary>
/// 轻量化的轨道播放器。
/// </summary>
[DisallowMultipleComponent]
public class UITweenTrack : MonoBehaviour
{
    /// <summary>
    /// 轨道播放模式
    /// </summary>
    public enum PlayMode
    {
        Forward, // 正向播放
        Reverse  // 反向播放
    }

    /// <summary>
    /// 轨道反向播放的策略
    /// </summary>
    public enum ReversePlayMode
    {
        Default,           // 倒序播放Clip、倒序执行Item、带延迟
        ForwardOrderReverse, // 正序播放Clip、倒序执行Item、带延迟
        QuickExit          // 倒序播放Clip、同时执行所有Item、无延迟
    }

    public enum TrackPlayFlow
    {
        [Tooltip("上一个动画播放完毕后，再等待延迟，然后播放下一个")]
        Sequential,
        [Tooltip("上一个动画开始后，直接等待延迟，然后播放下一个（动画会重叠）")]
        Staggered
    }

    [Header("播放流程控制")]
    [Tooltip("控制正向播放时，轨道内元素的播放方式")]
    public TrackPlayFlow playFlow = TrackPlayFlow.Staggered;
    
    [System.Serializable]
    public class TrackItem
    {
        [Tooltip("需要播放动画的 UI 对象，必须带有 UITweenPlayer 组件。")]
        public UITweenPlayer player;

        [Tooltip("在 Library 中的动画名称，仅支持按名称播放。")]
        public string presetName;

        [Tooltip("该动画播放完毕后的间隔，单位：秒。")]
        public float delayAfterPlay = 0.1f;
    }

    [System.Serializable]
    public class Track
    {
        [Tooltip("轨道名称，仅用于标识。")]
        public string trackName;

        [Tooltip("为该轨道全部元素统一设置的播放间隔。点击“应用到轨道”按钮后生效。")]
        public float uniformInterval = 0.1f;

        [Tooltip("轨道包含的元素，添加顺序即播放顺序。")]
        public List<TrackItem> items = new List<TrackItem>();

        public void ApplyUniformInterval(float interval)
        {
            uniformInterval = Mathf.Max(0f, interval);
            foreach (var item in items)
            {
                if (item == null) continue;
                item.delayAfterPlay = uniformInterval;
            }
        }
    }

    [Header("轨道集合")]
    [Tooltip("轨道集合")]
    public List<Track> tracks = new List<Track>();

    [Tooltip("播放间隔是否使用真实时间")]
    public bool useUnscaledIntervals = true;

    readonly Dictionary<int, Coroutine> _runningTracks = new();

    #region Public API for UnityEvents

    // --- 正向播放 ---
    public void PlayTrackByIndex_Event(int trackIndex) => PlayTrack(trackIndex);
    public void PlayTrackByName_Event(string trackName) => PlayTrack(trackName);

    // --- 默认反向 ---
    public void PlayTrackReverse_Default_ByIndex(int trackIndex) => PlayTrackReverse(trackIndex, ReversePlayMode.Default);
    public void PlayTrackReverse_Default_ByName(string trackName) => PlayTrackReverse(trackName, ReversePlayMode.Default);

    // --- 正序反向 ---
    public void PlayTrackReverse_ForwardOrder_ByIndex(int trackIndex) => PlayTrackReverse(trackIndex, ReversePlayMode.ForwardOrderReverse);
    public void PlayTrackReverse_ForwardOrder_ByName(string trackName) => PlayTrackReverse(trackName, ReversePlayMode.ForwardOrderReverse);

    // --- 快速退场 ---
    public void PlayTrackReverse_QuickExit_ByIndex(int trackIndex) => PlayTrackReverse(trackIndex, ReversePlayMode.QuickExit);
    public void PlayTrackReverse_QuickExit_ByName(string trackName) => PlayTrackReverse(trackName, ReversePlayMode.QuickExit);

    #endregion

    public void PlayTrack(string trackName)
    {
        int index = FindTrackIndex(trackName);
        if (index != -1) PlayTrack(index);
    }

    public void PlayTrack(int trackIndex)
    {
        PlayTrackCore(trackIndex, PlayMode.Forward, ReversePlayMode.Default);
    }

    public void PlayTrackReverse(string trackName, ReversePlayMode reverseMode)
    {
        int index = FindTrackIndex(trackName);
        if (index != -1) PlayTrackReverse(index, reverseMode);
    }

    public void PlayTrackReverse(int trackIndex, ReversePlayMode reverseMode)
    {
        PlayTrackCore(trackIndex, PlayMode.Reverse, reverseMode);
    }

    private void PlayTrackCore(int trackIndex, PlayMode playMode, ReversePlayMode reverseMode)
    {
        if (!isActiveAndEnabled || trackIndex < 0 || trackIndex >= tracks.Count)
        {
            return;
        }

        StopTrack(trackIndex);
        var track = tracks[trackIndex];
        if (track == null) return;

        var routine = StartCoroutine(RunTrackCoroutine(track, playMode, reverseMode));
        _runningTracks[trackIndex] = routine;
    }

    private int FindTrackIndex(string trackName)
    {
        if (string.IsNullOrEmpty(trackName)) return -1;
        return tracks.FindIndex(t => t != null && t.trackName == trackName);
    }

    public void StopTrack(int trackIndex)
    {
        if (_runningTracks.TryGetValue(trackIndex, out var routine) && routine != null)
        {
            StopCoroutine(routine);
        }
        _runningTracks.Remove(trackIndex);
    }

    public void StopAllTracks()
    {
        foreach (var routine in _runningTracks.Values)
        {
            if (routine != null) StopCoroutine(routine);
        }
        _runningTracks.Clear();
    }

    public void ApplyUniformInterval(int trackIndex, float interval)
    {
        if (trackIndex < 0 || trackIndex >= tracks.Count) return;
        tracks[trackIndex]?.ApplyUniformInterval(interval);
    }

    private IEnumerator RunTrackCoroutine(Track track, PlayMode playMode, ReversePlayMode reverseMode)
    {
        // 等待一帧确保所有UI对象初始化完毕
        yield return new WaitForEndOfFrame();

        if (playMode == PlayMode.Forward)
        {
            for (int i = 0; i < track.items.Count; i++)
            {
                var item = track.items[i];
                Tween tween = PlayItem(item, false);

                if (playFlow == TrackPlayFlow.Sequential)
                {
                    if (tween != null) yield return tween.WaitForCompletion();
                }

                float wait = Mathf.Max(0f, item.delayAfterPlay);
                if (wait > 0f)
                {
                    yield return useUnscaledIntervals ? new WaitForSecondsRealtime(wait) : new WaitForSeconds(wait);
                }
            }
        }
        else // Reverse Modes
        {
            switch (reverseMode)
            {
                case ReversePlayMode.Default:
                    for (int i = track.items.Count - 1; i >= 0; i--)
                    {
                        var item = track.items[i];
                        Tween tween = PlayItem(item, true);

                        if (playFlow == TrackPlayFlow.Sequential)
                        {
                            if (tween != null) yield return tween.WaitForCompletion();
                        }

                        float wait = Mathf.Max(0f, item.delayAfterPlay);
                        if (wait > 0f)
                        {
                            yield return useUnscaledIntervals ? new WaitForSecondsRealtime(wait) : new WaitForSeconds(wait);
                        }
                    }
                    break;

                case ReversePlayMode.ForwardOrderReverse:
                    for (int i = 0; i < track.items.Count; i++)
                    {
                        var item = track.items[i];
                        Tween tween = PlayItem(item, true);

                        if (playFlow == TrackPlayFlow.Sequential)
                        {
                            if (tween != null) yield return tween.WaitForCompletion();
                        }

                        float wait = Mathf.Max(0f, item.delayAfterPlay);
                        if (wait > 0f)
                        {
                            yield return useUnscaledIntervals ? new WaitForSecondsRealtime(wait) : new WaitForSeconds(wait);
                        }
                    }
                    break;

                case ReversePlayMode.QuickExit:
                    foreach (var item in track.items)
                    {
                        PlayItem(item, true);
                    }
                    break;
            }
        }
    }

    private IEnumerator PlayItemAndWait(TrackItem item, bool reversed)
    {
        Tween tween = PlayItem(item, reversed);
        if (tween != null) yield return tween.WaitForCompletion();

        float wait = Mathf.Max(0f, item.delayAfterPlay);
        if (wait > 0f)
        {
            yield return useUnscaledIntervals ? new WaitForSecondsRealtime(wait) : new WaitForSeconds(wait);
        }
    }

    private Tween PlayItem(TrackItem item, bool reversed)
    {
        if (item == null || item.player == null) return null;

        string sourceName = string.IsNullOrEmpty(item.player.name) ? "<Unnamed>" : item.player.name;
        string detail = $"TrackItem: {sourceName} · {item.presetName}";
        using (UITweenCallContext.BeginScope(this, "Track", gameObject.name, detail))
        {
            return reversed
                ? item.player.PlayMasterReversedByName(item.presetName, UITweenPlayer.BaselineCaptureMode.FunctionalState)
                : item.player.PlayMasterByName(item.presetName, UITweenPlayer.BaselineCaptureMode.FunctionalState);
        }
    }

    void OnDisable()
    {
        StopAllTracks();
    }
}
