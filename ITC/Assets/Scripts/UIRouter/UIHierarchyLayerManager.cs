using System;
using UnityEngine;

namespace ITC.UIRouter
{
    /// <summary>
    /// Controls activation of UI hierarchy layers based on the upcoming route state.
    /// </summary>
    public class UIHierarchyLayerManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The hierarchy level managed by this component.")]
        private int managedLevel;

        public enum NodeActivationFilter { Any, MatchExactId, MatchPrefix }

        [Header("节点过滤")]
        [Tooltip("Any=只要本层有节点就激活；MatchExactId/MatchPrefix=仅当节点Id匹配时才激活")]
        public NodeActivationFilter activationFilter = NodeActivationFilter.Any;

        [Tooltip("用于匹配的节点 Id 或前缀（如 Settings 或 Settings/）")]
        public string nodeIdOrPrefix;

        public event Action<UIRouteNode, UIRouteChangeContext> LayerActivated;
        public event Action<UIRouteChangeContext> LayerDeactivated;

        public void HandleRouteChanged(UIRouteChangeContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var nextNode = context.NextNode(managedLevel);
            bool willBeActive = nextNode != null && Matches(nextNode);

            if (willBeActive)
            {
                ApplyActiveState(nextNode, context);
            }
            else
            {
                ApplyInactiveState(context);
            }
        }

        protected virtual void ApplyActiveState(UIRouteNode nextNode, UIRouteChangeContext context)
        {
            LayerActivated?.Invoke(nextNode, context);
        }

        protected virtual void ApplyInactiveState(UIRouteChangeContext context)
        {
            LayerDeactivated?.Invoke(context);
        }

        private bool Matches(UIRouteNode node)
        {
            if (node == null) return false;

            switch (activationFilter)
            {
                case NodeActivationFilter.MatchExactId:
                    return !string.IsNullOrEmpty(nodeIdOrPrefix) && string.Equals(node.Id, nodeIdOrPrefix, StringComparison.Ordinal);
                case NodeActivationFilter.MatchPrefix:
                    return !string.IsNullOrEmpty(nodeIdOrPrefix) && node.Id != null && node.Id.StartsWith(nodeIdOrPrefix, StringComparison.Ordinal);
                default:
                    return true;
            }
        }
    }
}
