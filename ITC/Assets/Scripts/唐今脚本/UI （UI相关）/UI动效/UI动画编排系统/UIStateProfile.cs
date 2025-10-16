using System;
using System.Collections.Generic;
using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// ScriptableObject describing how actors bind to roles inside a UI state.
    /// </summary>
    [CreateAssetMenu(menuName = "ITC/UI/Choreography/UI State Profile", fileName = "UIStateProfile")]
    public class UIStateProfile : ScriptableObject
    {
        public string stateId;
        public List<RoleBinding> roleBindings = new List<RoleBinding>();
        public List<string> defaultPhaseOrder = new List<string>();

        [Serializable]
        public class RoleBinding
        {
            [Tooltip("Logical actor identifier that should occupy this role when the state becomes active.")]
            public string actorId;

            [Tooltip("Role identifier (e.g. PauseItem, HeaderShortcut).")]
            public string roleId;

            [Tooltip("Anchor identifier resolved at runtime via UIRoleAnchor.")]
            public string anchorId;

            [Tooltip("Optional style variant keyword passed to UIActor style handlers.")]
            public string styleVariant;

            [Tooltip("Whether the actor should be visible when the state is active.")]
            public bool visible = true;

            [Tooltip("Optional grid coordinate for ByGrid ordering (row, column).")]
            public Vector2Int gridIndex = Vector2Int.zero;

            [Tooltip("Optional overrides for presets per phase.")]
            public List<RolePhasePreset> phasePresets = new List<RolePhasePreset>();
        }

        [Serializable]
        public class RolePhasePreset
        {
            public string phaseName;
            public string presetKey;
        }

        public bool TryGetBindingForActor(string actorId, out RoleBinding binding)
        {
            for (int i = 0; i < roleBindings.Count; i++)
            {
                var candidate = roleBindings[i];
                if (candidate != null && candidate.actorId == actorId)
                {
                    binding = candidate;
                    return true;
                }
            }

            binding = null;
            return false;
        }

        public RoleBinding TryGetBindingForRole(string roleId)
        {
            for (int i = 0; i < roleBindings.Count; i++)
            {
                var candidate = roleBindings[i];
                if (candidate != null && candidate.roleId == roleId)
                {
                    return candidate;
                }
            }

            return null;
        }

        public IEnumerable<RoleBinding> GetBindings()
        {
            return roleBindings;
        }
    }
}
