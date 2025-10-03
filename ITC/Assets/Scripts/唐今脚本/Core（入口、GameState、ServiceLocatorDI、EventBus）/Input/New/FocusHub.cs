using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ITC.UI.Focus
{
    public class FocusHub : MonoBehaviour
    {
        [Flags]
        public enum FocusRequestFlags
        {
            None = 0,
            Force = 1 << 0,
            FromSelection = 1 << 1
        }

        private struct DomainState
        {
            public FocusTag domain;
            public GameObject lastSelected;
        }

        public static FocusHub Instance { get; private set; }

        private static readonly List<FocusTag> PendingRegistrations = new List<FocusTag>();

        private readonly Dictionary<FocusTag, DomainState> registry = new Dictionary<FocusTag, DomainState>();
        private readonly List<FocusTag> focusStack = new List<FocusTag>();
        private readonly Dictionary<FocusTier, FocusTag> activeByTier = new Dictionary<FocusTier, FocusTag>();

        private GameObject lastObservedSelection;
        private bool suppressSelectionTracking;

        public event Action<FocusTag> FocusChanged;

        public FocusTag Current => focusStack.Count > 0 ? focusStack[focusStack.Count - 1] : null;
        public IReadOnlyList<FocusTag> Stack => focusStack;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (PendingRegistrations.Count > 0)
            {
                for (int i = 0; i < PendingRegistrations.Count; i++)
                {
                    var domain = PendingRegistrations[i];
                    if (domain == null) continue;
                    Register(domain, domain.AutoFocusOnEnable);
                }

                PendingRegistrations.Clear();
            }
        }

        private void Update()
        {
            if (suppressSelectionTracking) return;

            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            var selected = eventSystem.currentSelectedGameObject;
            if (selected == lastObservedSelection) return;

            lastObservedSelection = selected;
            if (selected == null) return;

            var domain = ResolveDomain(selected);
            if (domain == null) return;
            if (!registry.ContainsKey(domain)) return;

            RememberSelection(domain, selected);

            if (Current == domain) return;
            if (!CanReach(domain, FocusRequestFlags.None)) return;

            Focus(domain, selected, FocusRequestFlags.FromSelection);
        }

        public static void EnqueueRegistration(FocusTag domain)
        {
            if (domain == null) return;
            if (PendingRegistrations.Contains(domain)) return;
            PendingRegistrations.Add(domain);
        }

        public static void RemovePendingRegistration(FocusTag domain)
        {
            if (domain == null) return;
            PendingRegistrations.Remove(domain);
        }

        public void Register(FocusTag domain, bool autoFocus)
        {
            if (domain == null || domain.Panel == null) return;

            registry[domain] = new DomainState { domain = domain, lastSelected = domain.GetLastSelected() };
            domain.SetInteractionEnabled(false);

            if (autoFocus)
            {
                Focus(domain, domain.GetLastSelected() ?? domain.GetDefaultFocus(), FocusRequestFlags.Force);
            }
            else if (focusStack.Count == 0)
            {
                RefocusCurrentTop();
            }
            else
            {
                UpdateRaycastState();
            }
        }

        public void Unregister(FocusTag domain)
        {
            if (domain == null) return;

            registry.Remove(domain);

            for (int i = focusStack.Count - 1; i >= 0; i--)
            {
                if (focusStack[i] == domain)
                {
                    focusStack.RemoveAt(i);
                }
            }

            UpdateRaycastState();
            RefocusCurrentTop();
        }

        public bool Focus(FocusTag domain, GameObject preferred = null)
        {
            return Focus(domain, preferred, FocusRequestFlags.None);
        }

        public bool Focus(FocusTag domain, Selectable preferred)
        {
            return Focus(domain, preferred ? preferred.gameObject : null, FocusRequestFlags.None);
        }

        public bool Focus(FocusDomainMask mask, GameObject preferred = null)
        {
            var domain = Find(mask);
            if (domain == null) return false;
            return Focus(domain, preferred);
        }

        public bool Focus(FocusDomainMask mask, GameObject preferred, FocusRequestFlags flags)
        {
            var domain = Find(mask);
            if (domain == null) return false;
            return Focus(domain, preferred, flags);
        }

        public bool Focus(FocusTag domain, GameObject preferred, FocusRequestFlags flags)
        {
            if (domain == null || !domain.isActiveAndEnabled) return false;
            if (!registry.ContainsKey(domain)) return false;
            if (!CanReach(domain, flags)) return false;

            RemoveFromStack(domain);
            RemoveSameTier(domain.Tier);

            focusStack.Add(domain);
            ApplyFocus(domain, preferred, flags);
            return true;
        }

        public bool PopCurrent()
        {
            if (focusStack.Count == 0) return false;
            focusStack.RemoveAt(focusStack.Count - 1);
            RefocusCurrentTop();
            return true;
        }

        public bool PopToTier(FocusTier tier)
        {
            bool removed = false;
            for (int i = focusStack.Count - 1; i >= 0; i--)
            {
                if (focusStack[i].Tier >= tier)
                {
                    focusStack.RemoveAt(i);
                    removed = true;
                }
            }

            if (!removed) return false;
            RefocusCurrentTop();
            return true;
        }

        public FocusTag Find(FocusDomainMask mask)
        {
            foreach (var kv in registry)
            {
                if (kv.Key != null && kv.Key.Mask == mask)
                {
                    return kv.Key;
                }
            }

            return null;
        }

        public FocusTag GetActiveDomain(FocusTier tier)
        {
            if (activeByTier.TryGetValue(tier, out var domain)) return domain;
            return null;
        }

        public bool HasBlockingTier => focusStack.Count > 0 && focusStack[focusStack.Count - 1].Tier > FocusTier.Base;

        private bool CanReach(FocusTag domain, FocusRequestFlags flags)
        {
            if ((flags & FocusRequestFlags.Force) != 0) return true;
            if (focusStack.Count == 0) return true;

            var top = focusStack[focusStack.Count - 1];
            if (top == domain) return true;
            if (domain.Tier < top.Tier) return false;
            return true;
        }

        private void RemoveFromStack(FocusTag domain)
        {
            for (int i = focusStack.Count - 1; i >= 0; i--)
            {
                if (focusStack[i] == domain)
                {
                    focusStack.RemoveAt(i);
                }
            }
        }

        private void RemoveSameTier(FocusTier tier)
        {
            for (int i = focusStack.Count - 1; i >= 0; i--)
            {
                if (focusStack[i].Tier == tier)
                {
                    focusStack.RemoveAt(i);
                    break;
                }
            }
        }

        private void ApplyFocus(FocusTag domain, GameObject preferred, FocusRequestFlags flags)
        {
            UpdateRaycastState();

            GameObject target = preferred;
            if (target != null && !domain.Contains(target))
            {
                target = null;
            }

            if (target == null && registry.TryGetValue(domain, out var state))
            {
                if (state.lastSelected != null && domain.Contains(state.lastSelected))
                {
                    target = state.lastSelected;
                }
            }

            if (target == null)
            {
                target = domain.GetDefaultFocus();
            }

            suppressSelectionTracking = true;
            domain.EnsureOpen();
            domain.ApplyFocus(target);
            suppressSelectionTracking = false;

            var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (current == null) current = target;
            RememberSelection(domain, current);
            lastObservedSelection = current;

            FocusChanged?.Invoke(domain);
        }

        private void RememberSelection(FocusTag domain, GameObject selected)
        {
            if (domain == null) return;
            if (selected == null) return;

            domain.RememberSelection(selected);
            if (!registry.ContainsKey(domain)) return;

            var state = registry[domain];
            state.lastSelected = domain.Contains(selected) ? selected : state.lastSelected;
            registry[domain] = state;
        }

        private void UpdateRaycastState()
        {
            activeByTier.Clear();
            for (int i = focusStack.Count - 1; i >= 0; i--)
            {
                var domain = focusStack[i];
                if (domain == null) continue;
                if (!activeByTier.ContainsKey(domain.Tier))
                {
                    activeByTier[domain.Tier] = domain;
                }
            }

            FocusTier highestTier = FocusTier.Base;
            if (focusStack.Count > 0)
            {
                highestTier = focusStack[focusStack.Count - 1].Tier;
            }

            foreach (var kv in registry)
            {
                var domain = kv.Key;
                if (domain == null) continue;

                bool enable = false;
                if (activeByTier.TryGetValue(domain.Tier, out var active) && active == domain)
                {
                    enable = domain.Tier == highestTier;
                }

                domain.SetInteractionEnabled(enable);
            }
        }

        private void RefocusCurrentTop()
        {
            if (focusStack.Count == 0)
            {
                var fallback = GetFallbackForTier(FocusTier.Base);
                if (fallback != null)
                {
                    if (!Focus(fallback, fallback.GetLastSelected() ?? fallback.GetDefaultFocus(), FocusRequestFlags.Force))
                    {
                        UpdateRaycastState();
                        FocusChanged?.Invoke(null);
                    }
                }
                else
                {
                    UpdateRaycastState();
                    FocusChanged?.Invoke(null);
                }

                return;
            }

            var top = focusStack[focusStack.Count - 1];
            if (top == null || !top.isActiveAndEnabled)
            {
                focusStack.RemoveAt(focusStack.Count - 1);
                RefocusCurrentTop();
                return;
            }

            ApplyFocus(top, top.GetLastSelected(), FocusRequestFlags.Force);
        }

        private FocusTag GetFallbackForTier(FocusTier tier)
        {
            FocusTag best = null;
            int bestOrder = int.MaxValue;

            foreach (var kv in registry)
            {
                var domain = kv.Key;
                if (domain == null) continue;
                if (domain.Tier != tier) continue;
                if (!domain.AllowFallback) continue;

                if (domain.OrderInTier < bestOrder)
                {
                    bestOrder = domain.OrderInTier;
                    best = domain;
                }
            }

            return best;
        }

        private FocusTag ResolveDomain(GameObject go)
        {
            if (go == null) return null;

            for (int i = focusStack.Count - 1; i >= 0; i--)
            {
                var candidate = focusStack[i];
                if (candidate != null && candidate.Contains(go)) return candidate;
            }

            foreach (var kv in registry)
            {
                var candidate = kv.Key;
                if (candidate != null && candidate.Contains(go)) return candidate;
            }

            return null;
        }
    }
}
