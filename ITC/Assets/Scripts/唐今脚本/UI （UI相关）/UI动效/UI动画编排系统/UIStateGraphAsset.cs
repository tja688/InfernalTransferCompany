using System;
using System.Collections.Generic;
using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Defines the collection of available states and transition profiles.
    /// </summary>
    [CreateAssetMenu(menuName = "ITC/UI/Choreography/UI State Graph", fileName = "UIStateGraph")]
    public class UIStateGraphAsset : ScriptableObject
    {
        public List<UIStateProfile> states = new List<UIStateProfile>();
        public List<StateTransition> transitions = new List<StateTransition>();

        [Serializable]
        public class StateTransition
        {
            public string fromStateId;
            public string toStateId;
            public UITransitionProfile profile;
        }

        public bool TryGetState(string stateId, out UIStateProfile profile)
        {
            for (int i = 0; i < states.Count; i++)
            {
                var candidate = states[i];
                if (candidate != null && candidate.stateId == stateId)
                {
                    profile = candidate;
                    return true;
                }
            }

            profile = null;
            return false;
        }

        public bool TryGetTransition(string fromStateId, string toStateId, out UITransitionProfile profile)
        {
            for (int i = 0; i < transitions.Count; i++)
            {
                var edge = transitions[i];
                if (edge != null && edge.fromStateId == fromStateId && edge.toStateId == toStateId)
                {
                    profile = edge.profile;
                    return profile != null;
                }
            }

            profile = null;
            return false;
        }
    }
}
