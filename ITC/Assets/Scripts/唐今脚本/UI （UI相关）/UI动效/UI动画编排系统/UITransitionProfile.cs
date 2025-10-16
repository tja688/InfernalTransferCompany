using System;
using System.Collections.Generic;
using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// ScriptableObject describing choreography rules for a transition between two UI states.
    /// </summary>
    [CreateAssetMenu(menuName = "ITC/UI/Choreography/UI Transition Profile", fileName = "UITransitionProfile")]
    public class UITransitionProfile : ScriptableObject
    {
        public string fromStateId;
        public string toStateId;
        public ConflictPolicy conflictPolicy = ConflictPolicy.Interrupt;
        public FallbackBehavior fallbackBehavior = FallbackBehavior.Hide;
        public List<PhaseSettings> phases = new List<PhaseSettings>
        {
            new PhaseSettings { phaseName = "PreExit", role = PhaseRole.Exit },
            new PhaseSettings { phaseName = "Exit", role = PhaseRole.Exit },
            new PhaseSettings { phaseName = "Enter", role = PhaseRole.Enter },
            new PhaseSettings { phaseName = "PostEnter", role = PhaseRole.Enter }
        };

        public enum ConflictPolicy
        {
            Interrupt,
            Blend,
            Queue
        }

        public enum FallbackBehavior
        {
            Hide,
            DisableInteractions,
            KeepLastState
        }

        public enum PhaseRole
        {
            Exit,
            Enter,
            Both
        }

        public enum SortMode
        {
            None,
            ByTagOrder,
            ByGrid,
            ByScreenX,
            ByScreenY,
            ByWeight
        }

        public enum StaggerMode
        {
            None,
            FixedInterval,
            Grouped
        }

        [Serializable]
        public class PhaseSettings
        {
            [Tooltip("Name of the phase (used for events and preset lookup).")]
            public string phaseName = "Enter";

            [Tooltip("Whether this phase targets exiting actors, entering actors, or both.")]
            public PhaseRole role = PhaseRole.Enter;

            [Tooltip("Tags that should participate. Empty = all.")]
            public List<string> includeTags = new List<string>();

            [Tooltip("Include actors that have no choreo tag assigned.")]
            public bool includeUntagged = true;

            [Tooltip("Delay before the phase starts relative to the previous phase ending.")]
            public float preDelay = 0f;

            [Tooltip("Minimum duration (used to compute when the next phase may start).")]
            public float minimumDuration = 0.2f;

            [Tooltip("Additional time added after the last staggered actor.")]
            public float tailHold = 0.05f;

            [Tooltip("Sort ordering for participating actors.")]
            public SortMode sortMode = SortMode.ByTagOrder;

            [Tooltip("Tag order priority used when SortMode=ByTagOrder.")]
            public List<string> tagOrder = new List<string> { "Header", "Primary", "Secondary", "ListItem", "Footer" };

            [Tooltip("When SortMode=ByGrid this controls row priority (true=top to bottom).")]
            public bool gridTopToBottom = true;

            [Tooltip("When SortMode=ByGrid this controls column priority (true=left to right).")]
            public bool gridLeftToRight = true;

            [Tooltip("When SortMode=ByScreenX/Y, true=ascending, false=descending.")]
            public bool ascending = true;

            [Tooltip("Stagger settings for actor activation within the phase.")]
            public StaggerSettings stagger = new StaggerSettings();

            [Tooltip("Optional preset override that applies to all actors in this phase when specific mapping is missing.")]
            public string defaultPresetKey;
        }

        [Serializable]
        public class StaggerSettings
        {
            public StaggerMode mode = StaggerMode.FixedInterval;
            public float interval = 0.03f;
            public int groupSize = 3;
            public float groupInterval = 0.1f;
        }
    }
}
