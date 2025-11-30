using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

/// <summary>
/// Sequencer Command: StartRuneInputGame()
/// 启动符文输入小游戏
/// 用法: StartRuneInputGame()
/// 小游戏完成后会发送 "RuneInputGameDone" 消息
/// </summary>
public class SequencerCommandStartRuneInputGame : SequencerCommand
{
    public void Start()
    {
        if (SlotCenter.Instance != null)
        {
            SlotCenter.Instance.trigger_event(HeEventNames.TriggerRuneInputStage);
            Debug.Log("[SequencerCommandStartRuneInputGame] 已触发符文输入小游戏");
        }
        else
        {
            if (DialogueDebug.logWarnings)
            {
                Debug.LogWarning("[SequencerCommandStartRuneInputGame] SlotCenter 实例未找到");
            }
        }

        Stop(); // 命令立即结束，小游戏异步执行
    }
}


