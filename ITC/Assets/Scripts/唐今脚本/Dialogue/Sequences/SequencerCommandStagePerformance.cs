using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

public class SequencerCommandStagePerformance : SequencerCommand
{
    public void Start()
    {
        string performanceID = GetParameter(0);

        if (StageManager.Instance != null)
        {
            StageManager.Instance.StagePerformance(performanceID);
        }
        else
        {
            if (DialogueDebug.logWarnings) Debug.LogWarning("Sequencer Command StagePerformance: StageManager instance not found.");
        }

        Stop();
    }
}
