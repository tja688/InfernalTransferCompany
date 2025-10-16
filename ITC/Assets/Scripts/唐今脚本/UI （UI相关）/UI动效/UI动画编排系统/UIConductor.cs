using System;
using System.Collections;
using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Executes transition plans by publishing phase and actor schedule events over time.
    /// </summary>
    public class UIConductor : MonoBehaviour
    {
        [SerializeField]
        private bool _useUnscaledClock = true;

        private Coroutine _running;
        private UITransitionPlan _currentPlan;
        private Action<UITransitionPlan> _onCompleted;

        public void ExecutePlan(UITransitionPlan plan, Action<UITransitionPlan> onCompleted)
        {
            if (plan == null)
            {
                return;
            }

            CancelCurrentPlan(UITransitionLifecycle.Cancelled);

            _currentPlan = plan;
            _onCompleted = onCompleted;
            _running = StartCoroutine(RunPlan(plan));
        }

        public void CancelCurrentPlan(UITransitionLifecycle reason)
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            if (_currentPlan != null)
            {
                UIEventBus.PublishLifecycle(new UITransitionLifecycleEvent(_currentPlan.transitionId, _currentPlan.fromStateId, _currentPlan.toStateId, reason, _currentPlan.profile));
            }

            _currentPlan = null;
            _onCompleted = null;
        }

        private IEnumerator RunPlan(UITransitionPlan plan)
        {
            float cursor = 0f;
            foreach (var phase in plan.phases)
            {
                float wait = Mathf.Max(0f, phase.startTime - cursor);
                if (wait > 0f)
                {
                    yield return Wait(wait, _useUnscaledClock);
                }

                cursor = phase.startTime;
                UIEventBus.PublishPhase(new UIPhaseEvent(plan.transitionId, phase.phaseName, UIPhaseEventType.PhaseStarted, cursor, phase.duration));

                foreach (var schedule in phase.actorSchedules)
                {
                    StartCoroutine(DispatchSchedule(schedule));
                }

                if (phase.duration > 0f)
                {
                    yield return Wait(phase.duration, _useUnscaledClock);
                }

                cursor += phase.duration;
                UIEventBus.PublishPhase(new UIPhaseEvent(plan.transitionId, phase.phaseName, UIPhaseEventType.PhaseCompleted, cursor, phase.duration));
            }

            _running = null;
            var completedPlan = plan;
            _currentPlan = null;
            var callback = _onCompleted;
            _onCompleted = null;
            callback?.Invoke(completedPlan);
        }

        private IEnumerator DispatchSchedule(UIActorSchedule schedule)
        {
            if (schedule.localDelay > 0f)
            {
                yield return Wait(schedule.localDelay, schedule.useUnscaledTime);
            }

            UIEventBus.PublishSchedule(schedule);
        }

        private static object  Wait(float duration, bool unscaled)
        {
            if (duration <= 0f)
            {
                return null;
            }

            return unscaled ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);
        }
    }
}
