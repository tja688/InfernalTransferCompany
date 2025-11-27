using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: ContinueDialogue()
    /// 在当前对话中模拟点击「继续」按钮，自动推进到下一句对话。
    /// 用法（在对话 Sequence 中）: ContinueDialogue()
    /// </summary>
    public class SequencerCommandContinueDialogue : SequencerCommand
    {
        public void Start()
        {
            // 如果当前没有对话，直接结束指令
            if (!DialogueManager.isConversationActive)
            {
                if (DialogueDebug.logWarnings)
                {
                    Debug.LogWarning("Sequencer Command ContinueDialogue: No active conversation.");
                }
                Stop();
                return;
            }

            StandardDialogueUI ui = null;

            if (DialogueManager.Instance != null)
            {
                ui = DialogueManager.Instance.DialogueUI as StandardDialogueUI;
            }

            // 兜底：如果 DialogueManager 没有挂 UI，尝试在场景里直接查找
            if (ui == null)
            {
                ui = GameObject.FindObjectOfType<StandardDialogueUI>();
            }

            if (ui != null)
            {
                ui.OnContinue();   // 等价于玩家按下继续键
            }
            else
            {
                if (DialogueDebug.logWarnings)
                {
                    Debug.LogWarning("Sequencer Command ContinueDialogue: StandardDialogueUI not found.");
                }
            }

            // 指令是瞬时执行的，立刻结束
            Stop();
        }
    }
}


