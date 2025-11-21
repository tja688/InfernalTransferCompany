using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

public class SequencerCommandStageElementPlay : SequencerCommand
{
    public void Start()
    {
        string elementID = GetParameter(0);
        string playerID = GetParameter(1);

        if (StageManager.Instance != null)
        {
            StageManager.Instance.StageElementPlay(elementID, playerID);
        }
        else
        {
            if (DialogueDebug.logWarnings) Debug.LogWarning("Sequencer Command StageElementPlay: StageManager instance not found.");
        }

        Stop();
    }
}
