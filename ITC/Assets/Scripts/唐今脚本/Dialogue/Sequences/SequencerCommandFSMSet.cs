using UnityEngine;
using PixelCrushers.DialogueSystem;
using ITC.Core.GameFlow;
using System;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: FSMSet(StateName)
    /// Forcefully sets the Game Global FSM to a specific state.
    /// Usage: FSMSet(Prologue) or FSMSet(Work), etc.
    /// </summary>
    public class SequencerCommandFSMSet : SequencerCommand
    {
        public void Start()
        {
            string stateName = GetParameter(0);

            if (string.IsNullOrEmpty(stateName))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Sequencer Command FSMSet: No state name provided.");
                Stop();
                return;
            }

            if (GameFlowManager.Instance == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Sequencer Command FSMSet: GameFlowManager Instance not found.");
                Stop();
                return;
            }

            try
            {
                GameState targetState = (GameState)Enum.Parse(typeof(GameState), stateName, true); // Case insensitive
                GameFlowManager.Instance.RequestSetState(targetState);
            }
            catch (ArgumentException)
            {
                Debug.LogError($"Sequencer Command FSMSet: Invalid state name '{stateName}'. Must be one of: {string.Join(", ", Enum.GetNames(typeof(GameState)))}");
            }

            Stop(); // Command is instantaneous
        }
    }
}
