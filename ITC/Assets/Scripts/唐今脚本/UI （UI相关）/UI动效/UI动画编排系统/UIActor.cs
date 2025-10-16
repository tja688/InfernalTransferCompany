using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Component that represents an animatable UI element participating in global choreography.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UIActor : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField]
        private string _actorId;

        [SerializeField]
        private string _choreoTag;

        [SerializeField]
        private float _choreoWeight = 0f;

        [SerializeField]
        private bool _autoRegister = true;

        [Header("Visibility Control")]
        [SerializeField]
        private bool _reactivateOnEnter = true;

        [SerializeField]
        private bool _deactivateAfterExit = true;

        [SerializeField]
        private bool _useCanvasGroupVisibility = true;

        [SerializeField]
        private CanvasGroup _visibilityCanvasGroup;

        [Header("Preset Mapping")]
        [SerializeField]
        private List<RolePresetCollection> _rolePresets = new List<RolePresetCollection>();

        [SerializeField]
        private List<PhasePreset> _globalPhasePresets = new List<PhasePreset>();

        [SerializeField]
        private string _fallbackEnterPreset;

        [SerializeField]
        private string _fallbackExitPreset;

        [Header("Events")]
        public UnityEvent<UIActorSchedule> onScheduleReceived;
        public UnityEvent<string> onStyleVariantChanged;

        private RectTransform _rectTransform;
        private IUIActorGoalApplier[] _goalAppliers;
        private IUIActorStyleHandler[] _styleHandlers;
        private Coroutine _pendingDeactivate;

        public string ActorId => _actorId;
        public string ChoreoTag => _choreoTag;
        public float ChoreoWeight => _choreoWeight;
        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_visibilityCanvasGroup == null && _useCanvasGroupVisibility)
            {
                _visibilityCanvasGroup = GetComponent<CanvasGroup>();
            }
            CacheHandlers();
        }

        private void CacheHandlers()
        {
            _goalAppliers = GetComponents<IUIActorGoalApplier>();
            _styleHandlers = GetComponents<IUIActorStyleHandler>();
        }

        private void OnEnable()
        {
            if (_autoRegister)
            {
                EnsureOrchestrator()?.RegisterActor(this);
            }

            UIEventBus.ActorScheduled += OnActorScheduled;
        }

        private void OnDisable()
        {
            UIEventBus.ActorScheduled -= OnActorScheduled;
            if (_autoRegister)
            {
                EnsureOrchestrator()?.UnregisterActor(this);
            }
        }

        private UIOrchestrator EnsureOrchestrator()
        {
            if (UIOrchestrator.Instance != null)
            {
                return UIOrchestrator.Instance;
            }

#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<UIOrchestrator>();
#else
            return FindObjectOfType<UIOrchestrator>();
#endif
        }

        private void OnActorScheduled(UIActorSchedule schedule)
        {
            if (schedule.actorId != _actorId)
            {
                return;
            }

            HandleSchedule(schedule);
        }

        private void HandleSchedule(UIActorSchedule schedule)
        {
            if (_reactivateOnEnter && schedule.targetVisible)
            {
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }
            }

            onScheduleReceived?.Invoke(schedule);
            if (!string.IsNullOrEmpty(schedule.styleVariant))
            {
                onStyleVariantChanged?.Invoke(schedule.styleVariant);
                if (_styleHandlers != null)
                {
                    for (int i = 0; i < _styleHandlers.Length; i++)
                    {
                        _styleHandlers[i]?.ApplyStyle(this, schedule.styleVariant, schedule.phaseName);
                    }
                }
            }

            if (_goalAppliers != null && _goalAppliers.Length > 0)
            {
                for (int i = 0; i < _goalAppliers.Length; i++)
                {
                    _goalAppliers[i]?.Apply(this, schedule);
                }
            }
            else if (schedule.hasSnapshot)
            {
                schedule.snapshot.ApplyTo(_rectTransform);
            }

            if (_useCanvasGroupVisibility && _visibilityCanvasGroup != null)
            {
                _visibilityCanvasGroup.interactable = schedule.targetVisible;
                _visibilityCanvasGroup.blocksRaycasts = schedule.targetVisible;
            }

            if (!schedule.targetVisible && _deactivateAfterExit && schedule.isExitPhase && !schedule.isEnterPhase)
            {
                if (_pendingDeactivate != null)
                {
                    StopCoroutine(_pendingDeactivate);
                }
                _pendingDeactivate = StartCoroutine(DeactivateAfter(schedule.durationHint, schedule.useUnscaledTime));
            }
        }

        private IEnumerator DeactivateAfter(float delay, bool unscaled)
        {
            if (delay > 0f)
            {
                if (unscaled)
                {
                    yield return new WaitForSecondsRealtime(delay);
                }
                else
                {
                    yield return new WaitForSeconds(delay);
                }
            }

            gameObject.SetActive(false);
            _pendingDeactivate = null;
        }

        public string ResolvePreset(string roleId, string phaseName)
        {
            if (!string.IsNullOrEmpty(phaseName))
            {
                for (int i = 0; i < _globalPhasePresets.Count; i++)
                {
                    var preset = _globalPhasePresets[i];
                    if (preset != null && preset.phaseName == phaseName && !string.IsNullOrEmpty(preset.presetKey))
                    {
                        return preset.presetKey;
                    }
                }
            }

            if (!string.IsNullOrEmpty(roleId))
            {
                for (int i = 0; i < _rolePresets.Count; i++)
                {
                    var collection = _rolePresets[i];
                    if (collection != null && collection.roleId == roleId)
                    {
                        string rolePreset = collection.Resolve(phaseName);
                        if (!string.IsNullOrEmpty(rolePreset))
                        {
                            return rolePreset;
                        }
                    }
                }
            }

            if (IsExitPhase(phaseName))
            {
                return _fallbackExitPreset;
            }

            return _fallbackEnterPreset;
        }

        private static bool IsExitPhase(string phaseName)
        {
            if (string.IsNullOrEmpty(phaseName))
            {
                return false;
            }

            return phaseName.IndexOf("exit", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [System.Serializable]
        public class PhasePreset
        {
            public string phaseName;
            public string presetKey;
        }

        [System.Serializable]
        public class RolePresetCollection
        {
            public string roleId;
            public string defaultEnterPreset;
            public string defaultExitPreset;
            public List<PhasePreset> overrides = new List<PhasePreset>();

            public string Resolve(string phaseName)
            {
                if (!string.IsNullOrEmpty(phaseName))
                {
                    for (int i = 0; i < overrides.Count; i++)
                    {
                        var entry = overrides[i];
                        if (entry != null && entry.phaseName == phaseName && !string.IsNullOrEmpty(entry.presetKey))
                        {
                            return entry.presetKey;
                        }
                    }
                }

                if (IsExitPhase(phaseName))
                {
                    return defaultExitPreset;
                }

                return defaultEnterPreset;
            }
        }
    }
}
