using System;
using PixelCrushers;
using UnityEngine;
using UnityEngine.UI;

namespace ITC.UI.Focus
{
    [Serializable]
    public struct FocusDomainMask : IEquatable<FocusDomainMask>
    {
        public FocusTier tier;
        public int orderInTier;

        public FocusDomainMask(FocusTier tier, int order)
        {
            this.tier = tier;
            orderInTier = order;
        }

        public bool Equals(FocusDomainMask other) => tier == other.tier && orderInTier == other.orderInTier;
        public override bool Equals(object obj) => obj is FocusDomainMask other && Equals(other);
        public override int GetHashCode() => ((int)tier * 397) ^ orderInTier;
        public static bool operator ==(FocusDomainMask left, FocusDomainMask right) => left.Equals(right);
        public static bool operator !=(FocusDomainMask left, FocusDomainMask right) => !left.Equals(right);
        public override string ToString() => $"{tier}-{orderInTier}";
    }

    public enum FocusTier
    {
        Base = 0,
        Modal = 1,
        Top = 2
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIPanel))]
    public class FocusTag : MonoBehaviour
    {
        [Header("Domain Identity")]
        [SerializeField] private FocusTier tier = FocusTier.Base;
        [SerializeField] private int orderInTier = 0;
        [SerializeField] private bool allowFallback = true;

        [Header("Behaviour")]
        [SerializeField] private bool autoFocusOnEnable = false;
        [SerializeField] private bool autoOpenOnFocus = true;
        [SerializeField] private bool autoCloseOnDisable = false;

        [Header("Default Focus")]
        [SerializeField] private Selectable defaultSelectable;

        [Header("Canvas Groups (optional)")]
        [SerializeField] private CanvasGroup[] managedCanvasGroups;

        private UIPanel panel;
        private GameObject lastSelected;
        private bool canvasGroupsResolved;

        public FocusTier Tier => tier;
        public int OrderInTier => orderInTier;
        public bool AllowFallback => allowFallback;
        public FocusDomainMask Mask => new FocusDomainMask(tier, orderInTier);
        internal bool AutoFocusOnEnable => autoFocusOnEnable;

        public UIPanel Panel => panel;

        private void Awake()
        {
            panel = GetComponent<UIPanel>();
        }

        private void OnEnable()
        {
            if (FocusHub.Instance != null)
            {
                FocusHub.Instance.Register(this, autoFocusOnEnable);
            }
            else
            {
                FocusHub.EnqueueRegistration(this);
            }
        }

        private void OnDisable()
        {
            if (autoCloseOnDisable && panel != null && panel.isOpen)
            {
                panel.Close();
            }

            FocusHub.RemovePendingRegistration(this);
            FocusHub.Instance?.Unregister(this);
        }

        internal GameObject GetDefaultFocus()
        {
            if (defaultSelectable != null) return defaultSelectable.gameObject;
            if (panel != null)
            {
                if (panel.firstSelected != null) return panel.firstSelected;
                if (panel.defaultControl != null) return panel.defaultControl;
            }

            return null;
        }

        internal void EnsureOpen()
        {
            if (panel != null && autoOpenOnFocus && !panel.isOpen)
            {
                panel.Open();
            }
        }

        internal void ApplyFocus(GameObject target)
        {
            if (panel == null) return;

            EnsureOpen();
            panel.TakeFocus();

            if (target != null)
            {
                panel.SetFocus(target);
            }
            else if (panel.firstSelected != null)
            {
                panel.SetFocus(panel.firstSelected);
            }
            else
            {
                panel.CheckFocus();
            }
        }

        internal void RememberSelection(GameObject go)
        {
            if (go != null && Contains(go))
            {
                lastSelected = go;
            }
        }

        internal GameObject GetLastSelected()
        {
            if (lastSelected != null && Contains(lastSelected)) return lastSelected;
            return null;
        }

        internal bool Contains(GameObject go)
        {
            if (go == null || panel == null) return false;
            return go.transform.IsChildOf(panel.transform);
        }

        internal void SetInteractionEnabled(bool enable)
        {
            EnsureCanvasGroups();

            if (managedCanvasGroups == null) return;
            for (int i = 0; i < managedCanvasGroups.Length; i++)
            {
                var group = managedCanvasGroups[i];
                if (group == null) continue;
                group.interactable = enable;
                group.blocksRaycasts = enable;
            }
        }

        private void EnsureCanvasGroups()
        {
            if (canvasGroupsResolved) return;

            if (managedCanvasGroups == null || managedCanvasGroups.Length == 0)
            {
                var group = GetComponentInChildren<CanvasGroup>(true);
                if (group != null)
                {
                    managedCanvasGroups = new[] { group };
                }
            }

            canvasGroupsResolved = true;
        }

        public void RequestFocus(GameObject preferred = null)
        {
            FocusHub.Instance?.Focus(this, preferred);
        }
    }
}
