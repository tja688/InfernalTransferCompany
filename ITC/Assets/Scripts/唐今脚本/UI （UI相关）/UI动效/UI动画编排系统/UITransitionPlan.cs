using System.Collections.Generic;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Runtime data structure describing a resolved transition between two states.
    /// </summary>
    public class UITransitionPlan
    {
        public readonly string transitionId;
        public readonly string fromStateId;
        public readonly string toStateId;
        public readonly UITransitionProfile profile;
        public readonly List<UIPhaseSchedule> phases = new List<UIPhaseSchedule>();

        public UITransitionPlan(string transitionId, string fromStateId, string toStateId, UITransitionProfile profile)
        {
            this.transitionId = transitionId;
            this.fromStateId = fromStateId;
            this.toStateId = toStateId;
            this.profile = profile;
        }
    }

    /// <summary>
    /// Schedule for a single phase (with timing + participating actors).
    /// </summary>
    public class UIPhaseSchedule
    {
        public string phaseName;
        public float startTime;
        public float duration;
        public readonly List<UIActorSchedule> actorSchedules = new List<UIActorSchedule>();
    }
}
