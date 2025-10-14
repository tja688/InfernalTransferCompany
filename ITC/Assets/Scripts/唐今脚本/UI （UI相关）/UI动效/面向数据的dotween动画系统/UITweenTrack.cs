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
    public enum PlayFlow { SequentialWait, StaggeredStart }
    public PlayFlow playFlow = PlayFlow.StaggeredStart; 
    
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

    [Tooltip("轨道集合")]
    public List<Track> tracks = new List<Track>();

    [Tooltip("播放间隔是否使用真实时间")]
    public bool useUnscaledIntervals = true;

    readonly Dictionary<int, Coroutine> _runningTracks = new();

    public void PlayTrackByIndex_Event(int trackIndex)
    {
        PlayTrack(trackIndex);
    }

    public void PlayTrackByName(string trackName)
    {
        if (string.IsNullOrEmpty(trackName)) return;

        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i] != null && tracks[i].trackName == trackName)
            {
                PlayTrack(i);
                return;
            }
        }
    }

    public void PlayTrack(int trackIndex)
    {
        if (!isActiveAndEnabled) return;
        if (trackIndex < 0 || trackIndex >= tracks.Count) return;

        StopTrack(trackIndex);
        var track = tracks[trackIndex];
        if (track == null) return;

        var routine = StartCoroutine(RunTrackInitializer(trackIndex, track));
        _runningTracks[trackIndex] = routine;
    }

    private IEnumerator RunTrackInitializer(int trackIndex, Track track)
    {
        yield return new WaitForEndOfFrame(); 
    
        yield return RunTrack(trackIndex, track);
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
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        _runningTracks.Clear();
    }

    public void ApplyUniformInterval(int trackIndex, float interval)
    {
        if (trackIndex < 0 || trackIndex >= tracks.Count) return;
        var track = tracks[trackIndex];
        track?.ApplyUniformInterval(interval);
    }

    IEnumerator RunTrack(int trackIndex, Track track)
    {
        foreach (var item in track.items)
        {
            if (item == null || item.player == null) continue;

            var tween = item.player.PlayMasterByName(item.presetName);

            if (playFlow == PlayFlow.SequentialWait)
            {
                if (tween != null) yield return tween.WaitForCompletion();

                float wait = Mathf.Max(0f, item.delayAfterPlay);
                if (wait > 0f)
                    yield return useUnscaledIntervals ? new WaitForSecondsRealtime(wait) : new WaitForSeconds(wait);
            }
            else // PlayFlow.StaggeredStart
            {
                float wait = Mathf.Max(0f, item.delayAfterPlay);
                if (wait > 0f)
                    yield return useUnscaledIntervals ? new WaitForSecondsRealtime(wait) : new WaitForSeconds(wait);
            }
        }
        _runningTracks.Remove(trackIndex);
    }


    void OnDisable()
    {
        StopAllTracks();
    }
}
