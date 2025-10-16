using System;
using System.Collections.Generic;
using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// High-level service that resolves transition commands into concrete schedules and feeds them to the conductor.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class UIOrchestrator : MonoBehaviour
    {
        public static UIOrchestrator Instance { get; private set; }

        [SerializeField]
        private UIStateGraphAsset _stateGraph;

        [SerializeField]
        private string _initialStateId;

        [SerializeField]
        private bool _autoEnterInitialState = true;

        [SerializeField]
        private UIConductor _conductor;

        [SerializeField]
        private bool _useUnscaledTime = true;

        private readonly Dictionary<string, UIActor> _actors = new Dictionary<string, UIActor>();
        private UIStateProfile _currentStateProfile;
        private string _currentStateId;
        private UITransitionPlan _activePlan;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple UIOrchestrators detected. Only the first instance will be used.", this);
            }
            else
            {
                Instance = this;
            }

            if (_conductor == null)
            {
                _conductor = GetComponentInChildren<UIConductor>();
            }
        }

        private void OnEnable()
        {
            UIEventBus.TransitionRequested += OnTransitionRequested;
        }

        private void OnDisable()
        {
            UIEventBus.TransitionRequested -= OnTransitionRequested;
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(_initialStateId))
            {
                if (_stateGraph != null && _stateGraph.TryGetState(_initialStateId, out var state))
                {
                    _currentStateId = _initialStateId;
                    _currentStateProfile = state;
                }

                if (_autoEnterInitialState)
                {
                    RequestTransition(_initialStateId, "Initial");
                }
            }
        }

        public void RegisterActor(UIActor actor)
        {
            if (actor == null || string.IsNullOrEmpty(actor.ActorId))
            {
                return;
            }

            _actors[actor.ActorId] = actor;
        }

        public void UnregisterActor(UIActor actor)
        {
            if (actor == null || string.IsNullOrEmpty(actor.ActorId))
            {
                return;
            }

            if (_actors.TryGetValue(actor.ActorId, out var existing) && ReferenceEquals(existing, actor))
            {
                _actors.Remove(actor.ActorId);
            }
        }

        private void OnTransitionRequested(UITransitionCommand command)
        {
            if (command == null)
            {
                return;
            }

            RequestTransition(command.toStateId, command.reason, command.payload);
        }

        public void RequestTransition(string toStateId, string reason = null, object payload = null)
        {
            if (string.IsNullOrEmpty(toStateId))
            {
                Debug.LogWarning("[UIOrchestrator] toStateId is null or empty.", this);
                return;
            }

            if (_stateGraph == null)
            {
                Debug.LogError("[UIOrchestrator] State graph not assigned.", this);
                return;
            }

            string fromStateId = _currentStateId;
            UIStateProfile fromProfile = _currentStateProfile;
            _stateGraph.TryGetState(toStateId, out var toProfile);

            if (!_stateGraph.TryGetTransition(fromStateId, toStateId, out var transitionProfile))
            {
                transitionProfile = ResolveFallbackTransition(fromStateId, toStateId);
                if (transitionProfile == null)
                {
                    Debug.LogWarning($"[UIOrchestrator] No transition profile found for {fromStateId} -> {toStateId}.", this);
                    return;
                }
            }

            string transitionId = Guid.NewGuid().ToString("N");

            UIEventBus.PublishLifecycle(new UITransitionLifecycleEvent(transitionId, fromStateId, toStateId, UITransitionLifecycle.Requested, transitionProfile));

            var plan = BuildPlan(transitionId, fromStateId, fromProfile, toStateId, toProfile, transitionProfile, payload);
            if (plan == null)
            {
                Debug.LogWarning("[UIOrchestrator] Failed to build transition plan.", this);
                return;
            }

            _activePlan = plan;
            UIEventBus.PublishLifecycle(new UITransitionLifecycleEvent(transitionId, fromStateId, toStateId, UITransitionLifecycle.Planned, transitionProfile));
            if (_conductor != null)
            {
                _conductor.ExecutePlan(plan, OnTransitionCompleted);
                UIEventBus.PublishLifecycle(new UITransitionLifecycleEvent(transitionId, fromStateId, toStateId, UITransitionLifecycle.Started, transitionProfile));
            }
            else
            {
                Debug.LogError("[UIOrchestrator] No conductor assigned to execute plan.", this);
            }
        }

        private void OnTransitionCompleted(UITransitionPlan plan)
        {
            if (plan == null)
            {
                return;
            }

            if (_stateGraph != null && _stateGraph.TryGetState(plan.toStateId, out var newState))
            {
                _currentStateId = plan.toStateId;
                _currentStateProfile = newState;
            }

            UIEventBus.PublishLifecycle(new UITransitionLifecycleEvent(plan.transitionId, plan.fromStateId, plan.toStateId, UITransitionLifecycle.Completed, plan.profile));
        }

        private UITransitionPlan BuildPlan(string transitionId, string fromStateId, UIStateProfile fromProfile, string toStateId, UIStateProfile toProfile, UITransitionProfile profile, object payload)
        {
            if (profile == null)
            {
                return null;
            }

            var plan = new UITransitionPlan(transitionId, fromStateId, toStateId, profile);
            var contexts = BuildActorContexts(fromProfile, toProfile);

            float currentTime = 0f;
            foreach (var phase in profile.phases)
            {
                currentTime += Mathf.Max(0f, phase.preDelay);
                var phaseSchedule = new UIPhaseSchedule
                {
                    phaseName = phase.phaseName,
                    startTime = currentTime
                };

                var participants = FilterParticipants(contexts, phase);
                SortParticipants(participants, phase);
                var offsets = ComputeOffsets(participants.Count, phase);

                float lastOffset = 0f;
                for (int i = 0; i < participants.Count; i++)
                {
                    var context = participants[i];
                    var binding = SelectBindingForPhase(context, phase.role);
                    if (binding == null)
                    {
                        continue;
                    }

                    float offset = offsets != null && i < offsets.Count ? offsets[i] : 0f;
                    lastOffset = Mathf.Max(lastOffset, offset);

                    var schedule = BuildActorSchedule(plan, phase, context, binding, phaseSchedule.startTime, offset);
                    phaseSchedule.actorSchedules.Add(schedule);
                }

                phaseSchedule.duration = Mathf.Max(phase.minimumDuration, lastOffset + phase.tailHold);
                currentTime = phaseSchedule.startTime + phaseSchedule.duration;
                plan.phases.Add(phaseSchedule);
            }

            return plan;
        }

        private UITransitionProfile ResolveFallbackTransition(string fromStateId, string toStateId)
        {
            if (_stateGraph == null)
            {
                return null;
            }

            foreach (var edge in _stateGraph.transitions)
            {
                if (edge == null || edge.profile == null)
                {
                    continue;
                }

                bool fromMatches = string.Equals(edge.fromStateId, fromStateId, StringComparison.Ordinal);
                if (!fromMatches)
                {
                    if (string.IsNullOrEmpty(fromStateId) && string.IsNullOrEmpty(edge.fromStateId))
                    {
                        fromMatches = true;
                    }
                    else if (string.Equals(edge.fromStateId, "*", StringComparison.Ordinal))
                    {
                        fromMatches = true;
                    }
                }

                if (!fromMatches)
                {
                    continue;
                }

                if (string.Equals(edge.toStateId, toStateId, StringComparison.Ordinal))
                {
                    return edge.profile;
                }
            }

            return null;
        }
        private List<ActorContext> BuildActorContexts(UIStateProfile fromProfile, UIStateProfile toProfile)
        {
            var list = new List<ActorContext>();
            foreach (var kv in _actors)
            {
                var actor = kv.Value;
                if (actor == null)
                {
                    continue;
                }

                UIStateProfile.RoleBinding fromBinding = null;
                UIStateProfile.RoleBinding toBinding = null;
                fromProfile?.TryGetBindingForActor(actor.ActorId, out fromBinding);
                toProfile?.TryGetBindingForActor(actor.ActorId, out toBinding);

                if (fromBinding == null && toBinding == null)
                {
                    continue;
                }

                list.Add(new ActorContext
                {
                    actor = actor,
                    fromBinding = fromBinding,
                    toBinding = toBinding
                });
            }

            return list;
        }

        private List<ActorContext> FilterParticipants(List<ActorContext> contexts, UITransitionProfile.PhaseSettings phase)
        {
            var result = new List<ActorContext>();
            foreach (var context in contexts)
            {
                bool participates = phase.role switch
                {
                    UITransitionProfile.PhaseRole.Exit => context.fromBinding != null,
                    UITransitionProfile.PhaseRole.Enter => context.toBinding != null,
                    UITransitionProfile.PhaseRole.Both => context.fromBinding != null || context.toBinding != null,
                    _ => false
                };

                if (!participates)
                {
                    continue;
                }

                string tag = context.actor != null ? context.actor.ChoreoTag : null;
                if (phase.includeTags != null && phase.includeTags.Count > 0)
                {
                    bool contains = false;
                    for (int i = 0; i < phase.includeTags.Count; i++)
                    {
                        if (phase.includeTags[i] == tag)
                        {
                            contains = true;
                            break;
                        }
                    }

                    if (!contains)
                    {
                        if (!phase.includeUntagged || !string.IsNullOrEmpty(tag))
                        {
                            continue;
                        }
                    }
                }
                else if (!phase.includeUntagged && string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                result.Add(context);
            }

            return result;
        }

        private void SortParticipants(List<ActorContext> participants, UITransitionProfile.PhaseSettings phase)
        {
            switch (phase.sortMode)
            {
                case UITransitionProfile.SortMode.ByTagOrder:
                    participants.Sort((a, b) => CompareTagOrder(a.actor, b.actor, phase.tagOrder, phase.ascending));
                    break;
                case UITransitionProfile.SortMode.ByGrid:
                    participants.Sort((a, b) => CompareGridIndex(a, b, phase));
                    break;
                case UITransitionProfile.SortMode.ByScreenX:
                    participants.Sort((a, b) => CompareScreenPosition(a, b, phase, true));
                    break;
                case UITransitionProfile.SortMode.ByScreenY:
                    participants.Sort((a, b) => CompareScreenPosition(a, b, phase, false));
                    break;
                case UITransitionProfile.SortMode.ByWeight:
                    participants.Sort((a, b) => CompareWeight(a.actor, b.actor, phase.ascending));
                    break;
                case UITransitionProfile.SortMode.None:
                default:
                    break;
            }
        }

        private List<float> ComputeOffsets(int count, UITransitionProfile.PhaseSettings phase)
        {
            if (count <= 0)
            {
                return null;
            }

            var stagger = phase.stagger ?? new UITransitionProfile.StaggerSettings();
            var result = new List<float>(count);
            switch (stagger.mode)
            {
                case UITransitionProfile.StaggerMode.None:
                    for (int i = 0; i < count; i++) result.Add(0f);
                    break;
                case UITransitionProfile.StaggerMode.FixedInterval:
                    float step = Mathf.Max(0f, stagger.interval);
                    for (int i = 0; i < count; i++) result.Add(i * step);
                    break;
                case UITransitionProfile.StaggerMode.Grouped:
                    int groupSize = Mathf.Max(1, stagger.groupSize);
                    float interval = Mathf.Max(0f, stagger.groupInterval);
                    for (int i = 0; i < count; i++)
                    {
                        int groupIndex = i / groupSize;
                        result.Add(groupIndex * interval);
                    }
                    break;
            }

            return result;
        }

        private UIStateProfile.RoleBinding SelectBindingForPhase(ActorContext context, UITransitionProfile.PhaseRole role)
        {
            return role switch
            {
                UITransitionProfile.PhaseRole.Exit => context.fromBinding,
                UITransitionProfile.PhaseRole.Enter => context.toBinding,
                UITransitionProfile.PhaseRole.Both => context.toBinding ?? context.fromBinding,
                _ => context.toBinding ?? context.fromBinding
            };
        }

        private UIActorSchedule BuildActorSchedule(UITransitionPlan plan, UITransitionProfile.PhaseSettings phase, ActorContext context, UIStateProfile.RoleBinding binding, float phaseStartTime, float localOffset)
        {
            string presetKey = ResolvePresetKey(phase, context.actor, binding);
            var schedule = new UIActorSchedule
            {
                transitionId = plan.transitionId,
                actorId = context.actor.ActorId,
                roleId = binding.roleId,
                phaseName = phase.phaseName,
                targetStateId = plan.toStateId,
                anchorId = binding.anchorId,
                styleVariant = binding.styleVariant,
                presetKey = presetKey,
                targetVisible = binding.visible,
                useUnscaledTime = _useUnscaledTime,
                dispatchTime = phaseStartTime,
                localDelay = Mathf.Max(0f, localOffset),
                durationHint = Mathf.Max(phase.minimumDuration, phase.tailHold),
                isEnterPhase = phase.role != UITransitionProfile.PhaseRole.Exit,
                isExitPhase = phase.role != UITransitionProfile.PhaseRole.Enter
            };

            if (!string.IsNullOrEmpty(binding.anchorId) && UIAnchorRegistry.TryGetSnapshot(binding.anchorId, out var snapshot))
            {
                schedule.hasSnapshot = true;
                schedule.snapshot = snapshot;
            }
            else
            {
                schedule.hasSnapshot = false;
            }

            return schedule;
        }

        private string ResolvePresetKey(UITransitionProfile.PhaseSettings phase, UIActor actor, UIStateProfile.RoleBinding binding)
        {
            if (binding != null && binding.phasePresets != null)
            {
                for (int i = 0; i < binding.phasePresets.Count; i++)
                {
                    var preset = binding.phasePresets[i];
                    if (preset != null && preset.phaseName == phase.phaseName && !string.IsNullOrEmpty(preset.presetKey))
                    {
                        return preset.presetKey;
                    }
                }
            }

            string actorPreset = actor != null ? actor.ResolvePreset(binding?.roleId, phase.phaseName) : null;
            if (!string.IsNullOrEmpty(actorPreset))
            {
                return actorPreset;
            }

            return phase.defaultPresetKey;
        }

        private int CompareTagOrder(UIActor a, UIActor b, List<string> tagOrder, bool ascending)
        {
            int ia = IndexOf(tagOrder, a != null ? a.ChoreoTag : null);
            int ib = IndexOf(tagOrder, b != null ? b.ChoreoTag : null);
            int cmp = ia.CompareTo(ib);
            if (cmp == 0)
            {
                cmp = CompareWeight(a, b, true);
            }
            return ascending ? cmp : -cmp;
        }

        private int CompareGridIndex(ActorContext a, ActorContext b, UITransitionProfile.PhaseSettings phase)
        {
            var bindingA = SelectBindingForPhase(a, phase.role);
            var bindingB = SelectBindingForPhase(b, phase.role);
            Vector2Int gridA = bindingA != null ? bindingA.gridIndex : Vector2Int.zero;
            Vector2Int gridB = bindingB != null ? bindingB.gridIndex : Vector2Int.zero;

            int rowCmp = phase.gridTopToBottom ? gridA.y.CompareTo(gridB.y) : gridB.y.CompareTo(gridA.y);
            if (rowCmp != 0) return rowCmp;
            int colCmp = phase.gridLeftToRight ? gridA.x.CompareTo(gridB.x) : gridB.x.CompareTo(gridA.x);
            if (colCmp != 0) return colCmp;
            return CompareWeight(a.actor, b.actor, true);
        }

        private int CompareScreenPosition(ActorContext a, ActorContext b, UITransitionProfile.PhaseSettings phase, bool byX)
        {
            Vector2 posA = GetAnchorScreenPosition(a, phase.role);
            Vector2 posB = GetAnchorScreenPosition(b, phase.role);
            int cmp = byX ? posA.x.CompareTo(posB.x) : posA.y.CompareTo(posB.y);
            if (!phase.ascending) cmp = -cmp;
            if (cmp == 0)
            {
                cmp = CompareWeight(a.actor, b.actor, true);
            }
            return cmp;
        }

        private Vector2 GetAnchorScreenPosition(ActorContext context, UITransitionProfile.PhaseRole role)
        {
            var binding = SelectBindingForPhase(context, role);
            if (binding != null && !string.IsNullOrEmpty(binding.anchorId) && UIAnchorRegistry.TryGetAnchor(binding.anchorId, out var anchor) && anchor != null)
            {
                var rect = anchor.GetRectTransform();
                if (rect != null)
                {
                    Vector3 world = rect.TransformPoint(rect.rect.center);
                    return new Vector2(world.x, world.y);
                }
            }

            var actorRect = context.actor != null ? context.actor.RectTransform : null;
            if (actorRect != null)
            {
                Vector3 world = actorRect.TransformPoint(actorRect.rect.center);
                return new Vector2(world.x, world.y);
            }

            return Vector2.zero;
        }

        private int CompareWeight(UIActor a, UIActor b, bool ascending)
        {
            float wa = a != null ? a.ChoreoWeight : 0f;
            float wb = b != null ? b.ChoreoWeight : 0f;
            int cmp = wa.CompareTo(wb);
            return ascending ? cmp : -cmp;
        }

        private static int IndexOf(List<string> list, string value)
        {
            if (list == null || list.Count == 0)
            {
                return 0;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return i;
                }
            }

            return list.Count;
        }

        private class ActorContext
        {
            public UIActor actor;
            public UIStateProfile.RoleBinding fromBinding;
            public UIStateProfile.RoleBinding toBinding;
        }
    }
}
