using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Simple goal applier that bridges UIActor schedules to the existing UITweenPlayer component.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIActor))]
    public class UITweenActorGoalApplier : MonoBehaviour, IUIActorGoalApplier
    {
        [SerializeField]
        private UITweenPlayer _tweenPlayer;

        [SerializeField]
        private bool _applySnapshotBeforePlay = true;

        private UIActor _actor;

        private void Awake()
        {
            _actor = GetComponent<UIActor>();
            if (_tweenPlayer == null)
            {
                _tweenPlayer = GetComponent<UITweenPlayer>();
            }
        }

        public void Apply(UIActor actor, UIActorSchedule schedule)
        {
            if (_applySnapshotBeforePlay && schedule.hasSnapshot)
            {
                schedule.snapshot.ApplyTo(actor.RectTransform);
            }

            if (_tweenPlayer == null)
            {
                return;
            }

            string presetKey = schedule.presetKey;
            if (string.IsNullOrEmpty(presetKey))
            {
                presetKey = actor.ResolvePreset(schedule.roleId, schedule.phaseName);
            }

            if (!string.IsNullOrEmpty(presetKey))
            {
                _tweenPlayer.PlayByName(presetKey);
            }
        }
    }
}
