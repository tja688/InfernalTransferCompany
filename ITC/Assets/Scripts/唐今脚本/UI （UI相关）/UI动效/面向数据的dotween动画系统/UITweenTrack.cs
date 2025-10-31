using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


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

            [Tooltip("播放过程中禁用鼠标交互（在第一个对象开始到最后一个对象完成）。")]
            public bool disableInteractionDuringPlay = true;

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

    [Header("调试选项")]
    [Tooltip("开启后会在控制台输出轨道播放和播放完毕的调试信息")]
    public bool enableDebugLog = false;

    readonly Dictionary<int, Coroutine> _runningTracks = new();

    // 运行时交互状态快照（用于在播放结束或被打断时恢复）
    private struct InteractionSnapshot
    {
        public CanvasGroup canvasGroup;
        public bool hadCanvasGroup;
        public bool prevInteractable;
        public bool prevBlocksRaycasts;
    }

    private readonly Dictionary<int, List<InteractionSnapshot>> _trackInteractionSnapshots = new();

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

        // Debug日志：轨道开始播放
        if (enableDebugLog)
        {
            string trackName = string.IsNullOrEmpty(track.trackName) ? $"轨道 {trackIndex}" : track.trackName;
            string playModeStr = playMode == PlayMode.Forward ? "正向" : "反向";
            string reverseModeStr = playMode == PlayMode.Reverse 
                ? $" ({reverseMode})" 
                : "";
            Debug.Log($"[UITweenTrack] 开始播放轨道: {trackName} | 模式: {playModeStr}{reverseModeStr} | 管理器: {gameObject.name} | 元素数量: {track.items.Count}");
        }

        // 可选：播放期间禁用交互（在第一个对象播放开始前立即生效）
        if (track.disableInteractionDuringPlay)
        {
            ApplyInteractionDisable(trackIndex, track);
        }

        var routine = StartCoroutine(RunTrackCoroutine(track, playMode, reverseMode, trackIndex, track.disableInteractionDuringPlay));
        _runningTracks[trackIndex] = routine;
    }

    private int FindTrackIndex(string trackName)
    {
        if (string.IsNullOrEmpty(trackName)) return -1;
        return tracks.FindIndex(t => t != null && t.trackName == trackName);
    }

    

    public void ApplyUniformInterval(int trackIndex, float interval)
    {
        if (trackIndex < 0 || trackIndex >= tracks.Count) return;
        tracks[trackIndex]?.ApplyUniformInterval(interval);
    }

    private IEnumerator RunTrackCoroutine(Track track, PlayMode playMode, ReversePlayMode reverseMode, int trackIndex, bool shouldDisableInteraction)
    {
        // 等待一帧确保所有UI对象初始化完毕
        yield return new WaitForEndOfFrame();

        Tween lastStartedTween = null; // 用于在非顺序播放时等待“最后一个对象”完成

        if (playMode == PlayMode.Forward)
        {
            for (int i = 0; i < track.items.Count; i++)
            {
                var item = track.items[i];
                Tween tween = PlayItem(item, false);
                if (tween != null) lastStartedTween = tween;

                if (playFlow == TrackPlayFlow.Sequential)
                {
                    bool isLast = (i == track.items.Count - 1);
                    if (tween != null) yield return tween.WaitForCompletion();
                    // 按需求，播放过程在“最后一个对象动画完成”即结束：跳过最后一个的额外延迟
                    if (isLast && shouldDisableInteraction) continue;
                }

                float wait = Mathf.Max(0f, item.delayAfterPlay);
                if (wait > 0f)
                {
                    bool isLast = (i == track.items.Count - 1);
                    if (!(shouldDisableInteraction && isLast))
                    {
                        yield return useUnscaledIntervals ? new WaitForSecondsRealtime(wait) : new WaitForSeconds(wait);
                    }
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
                        if (tween != null) lastStartedTween = tween;

                        if (playFlow == TrackPlayFlow.Sequential)
                        {
                            bool isLastInOrder = (i == 0);
                            if (tween != null) yield return tween.WaitForCompletion();
                            if (isLastInOrder && shouldDisableInteraction) continue;
                        }

                        float wait = Mathf.Max(0f, item.delayAfterPlay);
                        if (wait > 0f)
                        {
                            bool isLastInOrder = (i == 0);
                            if (!(shouldDisableInteraction && isLastInOrder))
                            {
                                yield return useUnscaledIntervals ? new WaitForSecondsRealtime(wait) : new WaitForSeconds(wait);
                            }
                        }
                    }
                    break;

                case ReversePlayMode.ForwardOrderReverse:
                    for (int i = 0; i < track.items.Count; i++)
                    {
                        var item = track.items[i];
                        Tween tween = PlayItem(item, true);
                        if (tween != null) lastStartedTween = tween;

                        if (playFlow == TrackPlayFlow.Sequential)
                        {
                            bool isLast = (i == track.items.Count - 1);
                            if (tween != null) yield return tween.WaitForCompletion();
                            if (isLast && shouldDisableInteraction) continue;
                        }

                        float wait = Mathf.Max(0f, item.delayAfterPlay);
                        if (wait > 0f)
                        {
                            bool isLast = (i == track.items.Count - 1);
                            if (!(shouldDisableInteraction && isLast))
                            {
                                yield return useUnscaledIntervals ? new WaitForSecondsRealtime(wait) : new WaitForSeconds(wait);
                            }
                        }
                    }
                    break;

                case ReversePlayMode.QuickExit:
                    for (int i = 0; i < track.items.Count; i++)
                    {
                        var item = track.items[i];
                        var tween = PlayItem(item, true);
                        if (tween != null) lastStartedTween = tween;
                    }
                    break;
            }
        }

        // 在非顺序播放或快速退场等情况下，确保等待“最后一个对象动画完成”的瞬间
        if (shouldDisableInteraction)
        {
            bool needWaitLastTween =
                (playFlow == TrackPlayFlow.Staggered) ||
                (playMode == PlayMode.Reverse && reverseMode == ReversePlayMode.QuickExit) ||
                (playMode == PlayMode.Forward && playFlow != TrackPlayFlow.Sequential) ||
                (playMode == PlayMode.Reverse && playFlow != TrackPlayFlow.Sequential);

            if (needWaitLastTween && lastStartedTween != null)
            {
                yield return lastStartedTween.WaitForCompletion();
            }
        }

        // 恢复交互
        if (shouldDisableInteraction)
        {
            RestoreInteractions(trackIndex);
        }

        // Debug日志：轨道播放完毕
        if (enableDebugLog)
        {
            string trackName = string.IsNullOrEmpty(track.trackName) ? $"轨道 {trackIndex}" : track.trackName;
            string playModeStr = playMode == PlayMode.Forward ? "正向" : "反向";
            string reverseModeStr = playMode == PlayMode.Reverse 
                ? $" ({reverseMode})" 
                : "";
            Debug.Log($"[UITweenTrack] 轨道播放完毕: {trackName} | 模式: {playModeStr}{reverseModeStr} | 管理器: {gameObject.name}");
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

    private void ApplyInteractionDisable(int trackIndex, Track track)
    {
        // 防止重复应用
        if (_trackInteractionSnapshots.ContainsKey(trackIndex)) return;

        var snapshots = new List<InteractionSnapshot>();
        var processedObjectIds = new HashSet<int>();

        foreach (var it in track.items)
        {
            if (it == null || it.player == null) continue;
            var go = it.player.gameObject;
            if (go == null) continue;

            int id = go.GetInstanceID();
            if (!processedObjectIds.Add(id)) continue; // 对同一对象只处理一次

            var cg = go.GetComponent<CanvasGroup>();
            bool hadCg = cg != null;
            if (!hadCg)
            {
                cg = go.AddComponent<CanvasGroup>();
            }

            var snap = new InteractionSnapshot
            {
                canvasGroup = cg,
                hadCanvasGroup = hadCg,
                prevInteractable = cg.interactable,
                prevBlocksRaycasts = cg.blocksRaycasts
            };

            // 禁用交互与射线
            cg.interactable = false;
            cg.blocksRaycasts = false;

            snapshots.Add(snap);
        }

        if (snapshots.Count > 0)
        {
            _trackInteractionSnapshots[trackIndex] = snapshots;
        }
    }

    private void RestoreInteractions(int trackIndex)
    {
        if (!_trackInteractionSnapshots.TryGetValue(trackIndex, out var snapshots)) return;

        foreach (var snap in snapshots)
        {
            if (snap.canvasGroup == null) continue;

            if (snap.hadCanvasGroup)
            {
                snap.canvasGroup.interactable = snap.prevInteractable;
                snap.canvasGroup.blocksRaycasts = snap.prevBlocksRaycasts;
            }
            else
            {
                // 我们在播放开始时添加的，播放结束后移除
                Destroy(snap.canvasGroup);
            }
        }

        _trackInteractionSnapshots.Remove(trackIndex);
    }

    void OnDisable()
    {
        StopAllTracks();
    }

    public void StopTrack(int trackIndex)
    {
        if (_runningTracks.TryGetValue(trackIndex, out var routine) && routine != null)
        {
            StopCoroutine(routine);
            
            // Debug日志：轨道被停止
            if (enableDebugLog && trackIndex >= 0 && trackIndex < tracks.Count)
            {
                var track = tracks[trackIndex];
                if (track != null)
                {
                    string trackName = string.IsNullOrEmpty(track.trackName) ? $"轨道 {trackIndex}" : track.trackName;
                    Debug.Log($"[UITweenTrack] 轨道被停止: {trackName} | 管理器: {gameObject.name}");
                }
            }
        }
        _runningTracks.Remove(trackIndex);
        // 确保被中断时也能恢复交互
        RestoreInteractions(trackIndex);
    }

    public void StopAllTracks()
    {
        // 停止所有并恢复交互
        if (_runningTracks.Count > 0)
        {
            var keys = new List<int>(_runningTracks.Keys);
            foreach (var key in keys)
            {
                var routine = _runningTracks[key];
                if (routine != null) StopCoroutine(routine);
                RestoreInteractions(key);
            }
            _runningTracks.Clear();
        }
    }
}
