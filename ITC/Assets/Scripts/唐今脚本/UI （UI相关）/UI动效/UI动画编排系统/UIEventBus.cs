using System;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Lightweight event bus dedicated to the UI choreography system.
    /// Actors subscribe to high-level events instead of invoking each other directly.
    /// </summary>
    public static class UIEventBus
    {
        public static event Action<UITransitionCommand> TransitionRequested;
        public static event Action<UITransitionLifecycleEvent> TransitionLifecycleChanged;
        public static event Action<UIPhaseEvent> PhaseEventRaised;
        public static event Action<UIActorSchedule> ActorScheduled;

        public static void Publish(UITransitionCommand command)
        {
            TransitionRequested?.Invoke(command);
        }

        public static void PublishLifecycle(UITransitionLifecycleEvent lifecycleEvent)
        {
            TransitionLifecycleChanged?.Invoke(lifecycleEvent);
        }

        public static void PublishPhase(UIPhaseEvent phaseEvent)
        {
            PhaseEventRaised?.Invoke(phaseEvent);
        }

        public static void PublishSchedule(UIActorSchedule schedule)
        {
            ActorScheduled?.Invoke(schedule);
        }
    }
}
