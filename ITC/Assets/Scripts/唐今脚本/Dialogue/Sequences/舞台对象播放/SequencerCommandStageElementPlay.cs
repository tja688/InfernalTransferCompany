using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

public class SequencerCommandStageElementPlay : SequencerCommand
{
    public void Start()
    {
        string playerID = GetParameter(0);
        string legacyParam = GetParameter(1);

        if (string.IsNullOrEmpty(playerID) && !string.IsNullOrEmpty(legacyParam))
        {
            playerID = legacyParam;
#if UNITY_EDITOR
            if (DialogueDebug.logWarnings)
            {
                Debug.LogWarning("Sequencer Command StageElementPlay: 第二个参数语法已弃用，请改为 StageElementPlay(playerID)。");
            }
#endif
        }

        if (string.IsNullOrEmpty(playerID))
        {
            if (DialogueDebug.logWarnings)
            {
                Debug.LogWarning("Sequencer Command StageElementPlay: 需要提供有效的 playerID。");
            }
            Stop();
            return;
        }

        if (StageManager.Instance == null)
        {
            if (DialogueDebug.logWarnings)
            {
                Debug.LogWarning("Sequencer Command StageElementPlay: StageManager instance not found.");
            }
            Stop();
            return;
        }

        StageManager.Instance.StageElementPlay(playerID);
        Stop();
    }
}
