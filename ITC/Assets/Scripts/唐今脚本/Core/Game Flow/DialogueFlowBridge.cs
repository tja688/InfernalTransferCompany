using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace ITC.Core.GameFlow
{
    /// <summary>
    /// Bridges the C# GameFlowManager state to Dialogue System variables.
    /// </summary>
    public class DialogueFlowBridge : MonoBehaviour
    {
        private void Start()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnStateChanged += HandleStateChanged;
                GameFlowManager.Instance.OnDayChanged += HandleDayChanged;

                // Sync initial state
                SyncStateToDS(GameFlowManager.Instance.CurrentState);
                SyncDayToDS(GameFlowManager.Instance.CurrentDay);
            }
            else
            {
                Debug.LogWarning("[DialogueFlowBridge] GameFlowManager Instance not found.");
            }
        }

        private void OnDestroy()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnStateChanged -= HandleStateChanged;
                GameFlowManager.Instance.OnDayChanged -= HandleDayChanged;
            }
        }

        private void HandleStateChanged(GameState newState)
        {
            SyncStateToDS(newState);
        }

        private void HandleDayChanged(int newDay)
        {
            SyncDayToDS(newDay);
        }

        private void SyncStateToDS(GameState state)
        {
            // Sys_CurrentPhase (String)
            DialogueLua.SetVariable("Sys_CurrentPhase", state.ToString());

            // Sys_IsGameFinished (Boolean)
            bool isFinished = (state == GameState.Ending);
            DialogueLua.SetVariable("Sys_IsGameFinished", isFinished);

            Debug.Log($"[DialogueFlowBridge] Synced DS Variable 'Sys_CurrentPhase' to: {state}");
        }

        private void SyncDayToDS(int day)
        {
            // Sys_CurrentDay (Number)
            DialogueLua.SetVariable("Sys_CurrentDay", day);
            Debug.Log($"[DialogueFlowBridge] Synced DS Variable 'Sys_CurrentDay' to: {day}");
        }
    }
}
