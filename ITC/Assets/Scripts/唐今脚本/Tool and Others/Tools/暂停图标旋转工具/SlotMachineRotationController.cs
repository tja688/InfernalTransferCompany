using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 老虎机旋转控制器。
/// 订阅 SlotMachinePicker 的带方向回调事件，并根据方向播放相应的 MMF_Player 反馈。
/// </summary>
[RequireComponent(typeof(SlotMachinePicker))]
public class SlotMachineRotationController : MonoBehaviour
{
    [Header("老虎机选择器引用")]
    [Tooltip("要监听的老虎机选择器组件。如果为空，将自动从当前 GameObject 获取。")]
    [SerializeField]
    private SlotMachinePicker slotMachinePicker;

    [Header("反馈播放器")]
    [Tooltip("当向上滚动（Backward 方向）时播放的反馈")]
    [SerializeField]
    private MMF_Player upwardFeedbackPlayer;

    [Tooltip("当向下滚动（Forward 方向）时播放的反馈")]
    [SerializeField]
    private MMF_Player downwardFeedbackPlayer;

    [Header("调试选项")]
    [Tooltip("启用后会在控制台输出回调信息")]
    [SerializeField]
    private bool enableDebugLog = false;

    private void Awake()
    {
        // 如果没有手动指定，尝试从当前 GameObject 获取
        if (slotMachinePicker == null)
        {
            slotMachinePicker = GetComponent<SlotMachinePicker>();
        }

        // 验证引用
        if (slotMachinePicker == null)
        {
            Debug.LogError($"[SlotMachineRotationController] 在 {gameObject.name} 上未找到 SlotMachinePicker 组件！", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // 订阅带方向的事件回调
        if (slotMachinePicker != null)
        {
            slotMachinePicker.onSnappedWithDirection.AddListener(OnSnappedWithDirection);
        }
    }

    private void OnDisable()
    {
        // 取消订阅，避免内存泄漏
        if (slotMachinePicker != null)
        {
            slotMachinePicker.onSnappedWithDirection.RemoveListener(OnSnappedWithDirection);
        }
    }

    /// <summary>
    /// 当老虎机吸附到某个索引时，根据方向播放相应的反馈
    /// </summary>
    /// <param name="index">当前吸附到的索引</param>
    /// <param name="direction">滚动方向</param>
    private void OnSnappedWithDirection(int index, SnapDirection direction)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SlotMachineRotationController] 吸附到索引 {index}，方向: {direction}", this);
        }

        switch (direction)
        {
            case SnapDirection.Forward:
                // 向下滚动 - 播放向下反馈
                PlayDownwardFeedback();
                break;

            case SnapDirection.Backward:
                // 向上滚动 - 播放向上反馈
                PlayUpwardFeedback();
                break;

            case SnapDirection.Neutral:
                // 中立方向（例如强制指定索引），不播放反馈
                if (enableDebugLog)
                {
                    Debug.Log($"[SlotMachineRotationController] 中立方向，跳过反馈播放", this);
                }
                break;
        }
    }

    /// <summary>
    /// 播放向上反馈
    /// </summary>
    private void PlayUpwardFeedback()
    {
        if (upwardFeedbackPlayer != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SlotMachineRotationController] 播放向上反馈", this);
            }
            upwardFeedbackPlayer.PlayFeedbacks();
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning($"[SlotMachineRotationController] 向上反馈播放器未配置！", this);
        }
    }

    /// <summary>
    /// 播放向下反馈
    /// </summary>
    private void PlayDownwardFeedback()
    {
        if (downwardFeedbackPlayer != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SlotMachineRotationController] 播放向下反馈", this);
            }
            downwardFeedbackPlayer.PlayFeedbacks();
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning($"[SlotMachineRotationController] 向下反馈播放器未配置！", this);
        }
    }

    /// <summary>
    /// 在编辑器中验证配置
    /// </summary>
    private void OnValidate()
    {
        // 如果没有手动指定，尝试自动获取
        if (slotMachinePicker == null)
        {
            slotMachinePicker = GetComponent<SlotMachinePicker>();
        }
    }
}



