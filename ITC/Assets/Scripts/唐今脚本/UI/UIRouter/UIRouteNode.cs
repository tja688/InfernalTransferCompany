using System;
using System.Collections.Generic;

namespace ITC.UIRouter
{
    /// <summary>
    /// Represents a single node within a UI route hierarchy.
    /// </summary>
    public sealed class UIRouteNode
    {
        public UIRouteNode(string id, IReadOnlyList<UIRouteNode> children = null)
        {
            Id = id ?? string.Empty;
            if (children != null)
            {
                _children.AddRange(children);
            }
        }

        public string Id { get; }

        private readonly List<UIRouteNode> _children = new List<UIRouteNode>();

        public IReadOnlyList<UIRouteNode> Children => _children;

        public void AddChild(UIRouteNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _children.Add(node);
        }
    }
}
