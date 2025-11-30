using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

/// <summary>
/// Sequencer Command: StartDebugGame()
/// 启动调试小游戏
/// 用法: StartDebugGame()
/// 小游戏完成后会发送 "DebugGameDone" 消息
/// </summary>
public class SequencerCommandStartDebugGame : SequencerCommand
{
    public void Start()
    {
        if (SlotCenter.Instance != null)
        {
            SlotCenter.Instance.trigger_event(HeEventNames.TriggerDebugStage);
            Debug.Log("[SequencerCommandStartDebugGame] 已触发调试小游戏");
        }
        else
        {
            if (DialogueDebug.logWarnings)
            {
                Debug.LogWarning("[SequencerCommandStartDebugGame] SlotCenter 实例未找到");
            }
        }

        Stop(); // 命令立即结束，小游戏异步执行
    }
}


