using System;
using System.Collections.Generic;

namespace ITC.UIRouter
{
    /// <summary>
    /// Provides context information about an upcoming route change.
    /// </summary>
    public sealed class UIRouteChangeContext
    {
        private readonly IReadOnlyList<UIRouteNode> _nextPath;

        public UIRouteChangeContext(IReadOnlyList<UIRouteNode> nextPath)
        {
            _nextPath = nextPath ?? Array.Empty<UIRouteNode>();
        }

        public UIRouteNode NextNode(int level)
        {
            if (level < 0 || level >= _nextPath.Count)
            {
                return null;
            }

            return _nextPath[level];
        }
    }
}
