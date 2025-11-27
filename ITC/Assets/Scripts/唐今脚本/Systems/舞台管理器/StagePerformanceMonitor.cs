using System.Reflection;
using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 舞台演员表演合法性监控脚本。
/// - 监控 StageManager 的 Performance 模式播放
/// - 当 Performance 使用的 MMF_Player 目标指向处于 OutsideStage 状态的 StageElement 时，在 Debug 中给出警告
/// - 仅用于调试，不会阻止真正的播放
/// </summary>
public partial class StagePerformanceMonitor : MonoBehaviour
{
    public static StagePerformanceMonitor Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 由 StageManager 在 Performance 模式播放时调用。
    /// </summary>
    public static void CheckPerformance(string performanceID, MMF_Player player)
    {
        if (Instance == null)
        {
            // 未在场景中挂载监控脚本则直接跳过，不影响游戏运行。
            return;
        }

        Instance.InternalCheckPerformance(performanceID, player);
    }

    private void InternalCheckPerformance(string performanceID, MMF_Player player)
    {
        if (player == null) return;
        if (StageManager.Instance == null) return;

        foreach (var feedback in player.FeedbacksList)
        {
            if (feedback == null) continue;

            var type = feedback.GetType();
            var flags = BindingFlags.Public | BindingFlags.Instance;

            // 与 StageManager.SetTargets 中一致的一批常见目标字段 / 属性名
            string[] targetPropertyNames = new string[]
            {
                "Target",
                "TargetTransform",
                "AnimatePositionTarget",
                "AnimateScaleTarget",
                "AnimateRotationTarget",
                "BoundGameObject"
            };

            foreach (var propName in targetPropertyNames)
            {
                // 字段
                var field = type.GetField(propName, flags);
                if (field != null)
                {
                    object value = field.GetValue(feedback);
                    CheckPotentialStageElementTarget(value, performanceID, feedback);
                }

                // 属性
                var prop = type.GetProperty(propName, flags);
                if (prop != null && prop.CanRead)
                {
                    object value = prop.GetValue(feedback, null);
                    CheckPotentialStageElementTarget(value, performanceID, feedback);
                }
            }
        }
    }

    private void CheckPotentialStageElementTarget(object value, string performanceID, MMF_Feedback feedback)
    {
        if (value == null) return;

        Transform t = null;

        if (value is Transform tf)
        {
            t = tf;
        }
        else if (value is GameObject go)
        {
            t = go.transform;
        }
        else if (value is RectTransform rt)
        {
            t = rt.transform;
        }

        if (t == null) return;

        // 寻找是否挂在某个 StageElement（或其父节点）上
        var element = t.GetComponentInParent<StageElement>();
        if (element == null) return;

        if (element.CurrentState == StageElement.ElementState.OutsideStage)
        {
            Debug.LogWarning(
                $"[StagePerformanceMonitor] Performance '{performanceID}' is using feedback '{feedback.GetType().Name}' " +
                $"to target StageElement '{element.StageElementID}' which is currently OutsideStage. 该 element 可能被非法调用，请注意。");
        }
    }
}


