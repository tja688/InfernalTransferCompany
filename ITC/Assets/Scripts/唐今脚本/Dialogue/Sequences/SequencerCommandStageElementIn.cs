using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

public class SequencerCommandStageElementIn : SequencerCommand
{
    public void Start()
    {
        string elementID = GetParameter(0);
        string playerID = GetParameter(1);

        if (StageManager.Instance != null)
        {
            StageManager.Instance.StageElementIn(elementID, playerID);
        }
        else
        {
            if (DialogueDebug.logWarnings) Debug.LogWarning("Sequencer Command StageElementIn: StageManager instance not found.");
        }

        Stop();
    }
}
