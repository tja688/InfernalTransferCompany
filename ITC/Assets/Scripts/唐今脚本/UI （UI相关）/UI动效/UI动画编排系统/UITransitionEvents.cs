using System;
using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Represents a high-level request to transition from one UI state to another.
    /// </summary>
    [Serializable]
    public class UITransitionCommand
    {
        public string fromStateId;
        public string toStateId;
        public string reason;
        public object payload;

        public UITransitionCommand(string from, string to, string reason = null, object payload = null)
        {
            fromStateId = from;
            toStateId = to;
            this.reason = reason;
            this.payload = payload;
        }
    }

    public enum UITransitionLifecycle
    {
        Requested,
        Planned,
        Started,
        Completed,
        Cancelled
    }

    [Serializable]
    public class UITransitionLifecycleEvent
    {
        public string transitionId;
        public string fromStateId;
        public string toStateId;
        public UITransitionLifecycle lifecycle;
        public UITransitionProfile profile;

        public UITransitionLifecycleEvent(string id, string from, string to, UITransitionLifecycle phase, UITransitionProfile profile)
        {
            transitionId = id;
            fromStateId = from;
            toStateId = to;
            lifecycle = phase;
            this.profile = profile;
        }
    }

    public enum UIPhaseEventType
    {
        PhaseStarted,
        PhaseCompleted
    }

    [Serializable]
    public class UIPhaseEvent
    {
        public string transitionId;
        public string phaseName;
        public UIPhaseEventType eventType;
        public float scheduledTime;
        public float duration;

        public UIPhaseEvent(string transitionId, string phaseName, UIPhaseEventType eventType, float scheduledTime, float duration)
        {
            this.transitionId = transitionId;
            this.phaseName = phaseName;
            this.eventType = eventType;
            this.scheduledTime = scheduledTime;
            this.duration = duration;
        }
    }

    /// <summary>
    /// Runtime schedule for a single actor inside a transition phase.
    /// </summary>
    [Serializable]
    public struct UIActorSchedule
    {
        public string transitionId;
        public string actorId;
        public string roleId;
        public string phaseName;
        public string targetStateId;
        public string anchorId;
        public string styleVariant;
        public string presetKey;
        public bool targetVisible;
        public bool useUnscaledTime;
        public bool hasSnapshot;
        public RectTransformSnapshot snapshot;
        public float dispatchTime;
        public float localDelay;
        public float durationHint;
        public bool isEnterPhase;
        public bool isExitPhase;

        public float GetScheduledTime() => dispatchTime + localDelay;
    }
}
