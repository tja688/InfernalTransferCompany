using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

public class SequencerCommandStageElementOut : SequencerCommand
{
    public void Start()
    {
        string elementID = GetParameter(0);
        string playerID = GetParameter(1);

        if (StageManager.Instance != null)
        {
            StageManager.Instance.StageElementOut(elementID, playerID);
        }
        else
        {
            if (DialogueDebug.logWarnings) Debug.LogWarning("Sequencer Command StageElementOut: StageManager instance not found.");
        }

        Stop();
    }
}
