using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

/// <summary>
/// Sequencer Command: StartSoulHarvestGame()
/// 启动灵魂收取小游戏
/// 用法: StartSoulHarvestGame()
/// 小游戏完成后会发送 "SoulHarvestGameDone" 消息
/// </summary>
public class SequencerCommandStartSoulHarvestGame : SequencerCommand
{
    public void Start()
    {
        if (SlotCenter.Instance != null)
        {
            SlotCenter.Instance.trigger_event(HeEventNames.TriggerSoulHarvestStage);
            Debug.Log("[SequencerCommandStartSoulHarvestGame] 已触发灵魂收取小游戏");
        }
        else
        {
            if (DialogueDebug.logWarnings)
            {
                Debug.LogWarning("[SequencerCommandStartSoulHarvestGame] SlotCenter 实例未找到");
            }
        }

        Stop(); // 命令立即结束，小游戏异步执行
    }
}

