using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

/// <summary>
/// Sequencer Command: StartSpecialEventGame()
/// 启动特殊事件小游戏
/// 用法: StartSpecialEventGame()
/// 小游戏完成后会发送 "SpecialEventGameDone" 消息
/// </summary>
public class SequencerCommandStartSpecialEventGame : SequencerCommand
{
    public void Start()
    {
        if (SlotCenter.Instance != null)
        {
            SlotCenter.Instance.trigger_event(HeEventNames.TriggerSpecialEventStage);
            Debug.Log("[SequencerCommandStartSpecialEventGame] 已触发特殊事件小游戏");
        }
        else
        {
            if (DialogueDebug.logWarnings)
            {
                Debug.LogWarning("[SequencerCommandStartSpecialEventGame] SlotCenter 实例未找到");
            }
        }

        Stop(); // 命令立即结束，小游戏异步执行
    }
}

