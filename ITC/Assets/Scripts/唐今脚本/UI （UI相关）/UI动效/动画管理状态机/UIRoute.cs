using System.Collections.Generic;
using System.Text;

public class UIRoute
{
    private readonly Dictionary<UIHierarchyLevel, UIRouteNode> _levels = new();

    public UIRoute Clone()
    {
        var clone = new UIRoute();
        foreach (var pair in _levels)
        {
            clone._levels[pair.Key] = pair.Value;
        }

        return clone;
    }

    public void SetNode(UIHierarchyLevel level, UIRouteNode node)
    {
        if (node == null)
        {
            _levels.Remove(level);
        }
        else
        {
            _levels[level] = node;
        }
    }

    public UIRouteNode GetNode(UIHierarchyLevel level)
    {
        _levels.TryGetValue(level, out var node);
        return node;
    }

    public bool IsActive(UIHierarchyLevel level)
    {
        return _levels.ContainsKey(level);
    }

    public UIHierarchyLevel GetHighestLevel()
    {
        UIHierarchyLevel highest = UIHierarchyLevel.None;
        foreach (var pair in _levels)
        {
            if ((int)pair.Key > (int)highest)
            {
                highest = pair.Key;
            }
        }

        return highest;
    }

    public IEnumerable<KeyValuePair<UIHierarchyLevel, UIRouteNode>> Enumerate()
    {
        foreach (var pair in _levels)
        {
            yield return pair;
        }
    }

    public string BuildPath(UIHierarchyLevel startLevel)
    {
        var sb = new StringBuilder();
        for (int level = (int)startLevel; level <= (int)UIHierarchyLevelUtility.Highest; level++)
        {
            var key = (UIHierarchyLevel)level;
            if (_levels.TryGetValue(key, out var node) && node != null)
            {
                if (sb.Length > 0) sb.Append('/');
                sb.Append(node.Id);
            }
        }

        return sb.ToString();
    }
}

public class UIRouteNode
{
    public string Id { get; }
    public object Payload { get; }

    public UIRouteNode(string id, object payload = null)
    {
        Id = id;
        Payload = payload;
    }
}
