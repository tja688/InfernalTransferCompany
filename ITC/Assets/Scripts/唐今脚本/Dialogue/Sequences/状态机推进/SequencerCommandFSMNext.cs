using UnityEngine;
using PixelCrushers.DialogueSystem;
using ITC.Core.GameFlow;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: FSMNext()
    /// Advances the Game Global FSM to the next logical state.
    /// Usage: FSMNext()
    /// </summary>
    public class SequencerCommandFSMNext : SequencerCommand
    {
        public void Start()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.RequestAdvanceState();
            }
            else
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Sequencer Command FSMNext: GameFlowManager Instance not found.");
            }

            Stop(); // Command is instantaneous
        }
    }
}
