using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Thin convenience wrapper that forwards transition requests to the global orchestrator.
    /// </summary>
    public class UIStateMachine : MonoBehaviour
    {
        [SerializeField]
        private UIOrchestrator _orchestrator;

        private void Awake()
        {
            if (_orchestrator == null)
            {
                _orchestrator = GetComponentInChildren<UIOrchestrator>();
            }
        }

        public void RequestState(string stateId)
        {
            if (_orchestrator == null)
            {
                _orchestrator = UIOrchestrator.Instance;
            }

            if (_orchestrator != null)
            {
                _orchestrator.RequestTransition(stateId);
            }
            else
            {
                Debug.LogWarning("[UIStateMachine] No UIOrchestrator available to handle RequestState.", this);
            }
        }
    }
}
