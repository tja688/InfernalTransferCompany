using System.Collections.Generic;
using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Central registry that exposes lookup services for UI role anchors at runtime.
    /// Anchors are registered automatically by the <see cref="UIRoleAnchor"/> component.
    /// </summary>
    public static class UIAnchorRegistry
    {
        private static readonly Dictionary<string, UIRoleAnchor> _anchors = new Dictionary<string, UIRoleAnchor>();

        /// <summary>
        /// Registers (or replaces) an anchor in the global registry.
        /// </summary>
        public static void Register(UIRoleAnchor anchor)
        {
            if (anchor == null || string.IsNullOrEmpty(anchor.AnchorId))
            {
                return;
            }

            _anchors[anchor.AnchorId] = anchor;
        }

        /// <summary>
        /// Removes an anchor from the global registry.
        /// </summary>
        public static void Unregister(UIRoleAnchor anchor)
        {
            if (anchor == null || string.IsNullOrEmpty(anchor.AnchorId))
            {
                return;
            }

            if (_anchors.TryGetValue(anchor.AnchorId, out var existing) && ReferenceEquals(existing, anchor))
            {
                _anchors.Remove(anchor.AnchorId);
            }
        }

        /// <summary>
        /// Tries to locate an anchor by its identifier.
        /// </summary>
        public static bool TryGetAnchor(string anchorId, out UIRoleAnchor anchor)
        {
            if (!string.IsNullOrEmpty(anchorId))
            {
                return _anchors.TryGetValue(anchorId, out anchor);
            }

            anchor = null;
            return false;
        }

        /// <summary>
        /// Captures a snapshot of a registered anchor's RectTransform.
        /// </summary>
        public static bool TryGetSnapshot(string anchorId, out RectTransformSnapshot snapshot)
        {
            snapshot = default;
            if (TryGetAnchor(anchorId, out var anchor) && anchor != null)
            {
                var rect = anchor.GetRectTransform();
                if (rect != null)
                {
                    snapshot = RectTransformSnapshot.FromRectTransform(rect);
                    return true;
                }
            }

            return false;
        }
    }
}
