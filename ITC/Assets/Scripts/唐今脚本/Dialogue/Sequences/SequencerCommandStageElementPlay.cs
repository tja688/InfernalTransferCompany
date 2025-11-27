using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

public class SequencerCommandStageElementPlay : SequencerCommand
{
    public void Start()
    {
        string param0 = GetParameter(0);
        string param1 = GetParameter(1);

        if (StageManager.Instance != null)
        {
            // 两个参数都不为空：按旧逻辑，elementID + playerID，播放指定舞台元素的动效
            if (!string.IsNullOrEmpty(param0) && !string.IsNullOrEmpty(param1))
            {
                string elementID = param0;
                string playerID = param1;
                StageManager.Instance.StageElementPlay(elementID, playerID);
            }
            else
            {
                // 支持重载：
                // - 只填一个 ID：StageElementPlay(playerID) => 走 Performance 播放模式
                // - 或第一个留空，第二个为 ID：StageElementPlay(, playerID) => 也视为 Performance
                string performanceID = null;

                if (string.IsNullOrEmpty(param0) && !string.IsNullOrEmpty(param1))
                {
                    // 形如 StageElementPlay(, playerID)
                    performanceID = param1;
                }
                else if (!string.IsNullOrEmpty(param0) && string.IsNullOrEmpty(param1))
                {
                    // 形如 StageElementPlay(playerID)
                    performanceID = param0;
                }

                if (!string.IsNullOrEmpty(performanceID))
                {
                    // 不绑定到具体舞台元素，直接按 Performance 模式播放，不做元素在场检查
                    StageManager.Instance.StagePerformance(performanceID);
                }
                else
                {
                    if (DialogueDebug.logWarnings)
                    {
                        Debug.LogWarning("Sequencer Command StageElementPlay: 参数不合法，至少需要一个有效的动效 ID。");
                    }
                }
            }
        }
        else
        {
            if (DialogueDebug.logWarnings) Debug.LogWarning("Sequencer Command StageElementPlay: StageManager instance not found.");
        }

        Stop();
    }
}
