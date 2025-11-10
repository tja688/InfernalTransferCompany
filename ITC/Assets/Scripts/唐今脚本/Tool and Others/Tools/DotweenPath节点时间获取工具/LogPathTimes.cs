using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class DOTweenPathWaypointLogger : MonoBehaviour
{
    [Tooltip("不指定则自动在本物体上找 DOTweenPath")]
    public DOTweenPath targetPath;

    [Header("Options")]
    [Tooltip("启动时把 tween 回到 0 再开始（推荐勾选，避免错过第一个路标回调）")]
    public bool RestartFromZero = true;

    [Tooltip("报表时间是否包含 DOTweenPath 的 delay（通常设为 true，便于直接给 Feel 的 InitialDelay 用）")]
    public bool IncludeDelayInReport = true;

    [Tooltip("同时打印缩放时间(Time.time)与非缩放时间(Time.unscaledTime)（仅作参考）")]
    public bool AlsoLogUnityClock = false;

    private Tween _tween;
    private float _startTimeScaled;
    private float _startTimeUnscaled;
    private readonly List<float> _timesWithDelay = new List<float>();     // 到达每个 waypoint 的“从0算起”秒数（可含 delay）
    private readonly List<float> _timesNoDelay   = new List<float>();     // 不含 delay 的 tween 内部秒数（Elapsed(false)）

    void Awake()
    {
        if (!targetPath) targetPath = GetComponent<DOTweenPath>();
        if (!targetPath)
        {
            Debug.LogError("[PathLogger] 未找到 DOTweenPath，请在 Inspector 指定。", this);
            enabled = false;
            return;
        }
    }

    void Start()
    {
        _tween = targetPath.GetTween();
        if (_tween == null)
        {
            Debug.LogError("[PathLogger] DOTweenPath 的 tween 尚未创建。确认 DOTweenPath 没被禁用，且在 Awake 里成功创建 tween。", this);
            return;
        }

        // 记录起点时刻（用于打印 Unity 时钟）
        _startTimeScaled   = Time.time;
        _startTimeUnscaled = Time.unscaledTime;

        // 清空旧结果
        _timesWithDelay.Clear();
        _timesNoDelay.Clear();

        // 订阅路标回调：索引从 0 开始（0=第一个 waypoint）
        _tween.OnWaypointChange(OnWaypoint);

        // 可选：确保从 0 开始跑，避免错过第一个点
        if (RestartFromZero)
        {
            _tween.Pause();
            _tween.Rewind();
            _tween.Play();
            _startTimeScaled   = Time.time;
            _startTimeUnscaled = Time.unscaledTime;
        }

        // 结束时输出汇总
        _tween.OnComplete(PrintSummary);
    }

    private void OnWaypoint(int index)
    {
        // DOTween 的 Elapsed(false) = 不含 delay 的补间内部秒数
        float elapsedNoDelay = _tween.Elapsed(false);
        float reportTime     = IncludeDelayInReport ? elapsedNoDelay + Mathf.Max(0f, targetPath.delay) : elapsedNoDelay;

        _timesNoDelay.Add(elapsedNoDelay);
        _timesWithDelay.Add(reportTime);

        if (AlsoLogUnityClock)
        {
            float sinceStartScaled   = Time.time        - _startTimeScaled;
            float sinceStartUnscaled = Time.unscaledTime - _startTimeUnscaled;
            Debug.Log(
                $"[PathLogger] Waypoint {index} reached | " +
                $"elapsed(noDelay)={elapsedNoDelay:F4}s, report={reportTime:F4}s " +
                $"| unityScaled={sinceStartScaled:F4}s, unityUnscaled={sinceStartUnscaled:F4}s",
                this
            );
        }
        else
        {
            Debug.Log(
                $"[PathLogger] Waypoint {index} reached | elapsed(noDelay)={elapsedNoDelay:F4}s, report={reportTime:F4}s",
                this
            );
        }
    }

    private void PrintSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[PathLogger] -------- Summary --------");
        sb.AppendLine($"GameObject: {targetPath.gameObject.name}");
        sb.AppendLine($"Waypoints : {targetPath.wps?.Count ?? 0} (索引从0开始：0=第一个)");
        sb.AppendLine($"Ease      : {targetPath.easeType}{(targetPath.easeType == Ease.INTERNAL_Custom ? " (AnimationCurve)" : "")}");
        sb.AppendLine($"Delay     : {targetPath.delay:F4}s  (IncludeDelayInReport={IncludeDelayInReport})");
        sb.AppendLine($"Closed    : {targetPath.isClosedPath}, PathType={targetPath.pathType}, PathMode={targetPath.pathMode}");
        sb.AppendLine();

        // 明细
        for (int i = 0; i < _timesWithDelay.Count; i++)
        {
            sb.AppendLine($"Waypoint {i}: report={_timesWithDelay[i]:F4}s, noDelay={_timesNoDelay[i]:F4}s");
        }

        // CSV 便于粘贴：index,reportSeconds,noDelaySeconds
        sb.AppendLine();
        sb.AppendLine("CSV (index, reportSeconds, noDelaySeconds):");
        for (int i = 0; i < _timesWithDelay.Count; i++)
        {
            sb.AppendLine($"{i},{_timesWithDelay[i]:F4},{_timesNoDelay[i]:F4}");
        }

        Debug.Log(sb.ToString(), this);
    }
}
