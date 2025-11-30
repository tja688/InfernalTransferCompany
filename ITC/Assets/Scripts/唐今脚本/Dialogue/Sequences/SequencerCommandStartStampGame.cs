using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

/// <summary>
/// Sequencer Command: StartStampGame()
/// 启动盖章小游戏
/// 用法: StartStampGame()
/// 小游戏完成后会发送 "StampGameDone" 消息
/// </summary>
public class SequencerCommandStartStampGame : SequencerCommand
{
    public void Start()
    {
        if (SlotCenter.Instance != null)
        {
            SlotCenter.Instance.trigger_event(HeEventNames.TriggerStampStage);
            Debug.Log("[SequencerCommandStartStampGame] 已触发盖章小游戏");
        }
        else
        {
            if (DialogueDebug.logWarnings)
            {
                Debug.LogWarning("[SequencerCommandStartStampGame] SlotCenter 实例未找到");
            }
        }

        Stop(); // 命令立即结束，小游戏异步执行
    }
}

