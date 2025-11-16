using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;
using UnityEngine;

/// <summary>
/// 自定义 DS sequence 命令：ChangeState("StateName")
/// 允许策划在对话节点的 Sequence 字段中直接切换宏观状态。
/// </summary>
public class SequencerCommandChangeState : SequencerCommand
{
    public void Start()
    {
        string targetStateName = GetParameter(0);
        if (string.IsNullOrWhiteSpace(targetStateName))
        {
            Debug.LogError("[SequencerCommandChangeState] 需要传入状态字符串，如 ChangeState(\"Signing\")。");
            Stop();
            return;
        }

        var gameFlow = ITCGameFlowManager.Instance ?? Object.FindObjectOfType<ITCGameFlowManager>();

        if (gameFlow == null)
        {
            Debug.LogError("[SequencerCommandChangeState] 场景中没有 ITCGameFlowManager，无法切换。");
            Stop();
            return;
        }

        gameFlow.RequestStateChange(targetStateName);
        Stop();
    }
}

