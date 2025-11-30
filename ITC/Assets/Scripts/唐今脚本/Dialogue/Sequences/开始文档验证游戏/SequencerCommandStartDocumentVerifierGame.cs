using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

/// <summary>
/// Sequencer Command: StartDocumentVerifierGame()
/// 启动文书核验小游戏
/// 用法: StartDocumentVerifierGame()
/// 小游戏完成后会发送 "DocumentVerifierGameDone" 消息
/// </summary>
public class SequencerCommandStartDocumentVerifierGame : SequencerCommand
{
    public void Start()
    {
        if (SlotCenter.Instance != null)
        {
            SlotCenter.Instance.trigger_event(HeEventNames.TriggerDocumentVerifierStage);
            Debug.Log("[SequencerCommandStartDocumentVerifierGame] 已触发文书核验小游戏");
        }
        else
        {
            if (DialogueDebug.logWarnings)
            {
                Debug.LogWarning("[SequencerCommandStartDocumentVerifierGame] SlotCenter 实例未找到");
            }
        }

        Stop(); // 命令立即结束，小游戏异步执行
    }
}


