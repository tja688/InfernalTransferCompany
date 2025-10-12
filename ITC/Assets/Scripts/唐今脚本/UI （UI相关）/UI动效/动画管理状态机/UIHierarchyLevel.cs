using System.Collections.Generic;

public enum UIHierarchyLevel
{
    None = -1,
    MainMenu = 0,
    GameUI = 1,
    PrimaryMenu = 2,
    SecondaryMenu = 3,
    TertiaryMenu = 4,
}

public static class UIHierarchyLevelUtility
{
    public const UIHierarchyLevel Lowest = UIHierarchyLevel.MainMenu;
    public const UIHierarchyLevel Highest = UIHierarchyLevel.TertiaryMenu;

    public static bool IsWithinBounds(this UIHierarchyLevel level)
    {
        return level >= Lowest && level <= Highest;
    }

    public static bool TryGetNext(UIHierarchyLevel current, out UIHierarchyLevel next)
    {
        int candidate = (int)current + 1;
        if (candidate > (int)Highest)
        {
            next = Highest;
            return false;
        }

        next = (UIHierarchyLevel)candidate;
        return true;
    }

    public static IEnumerable<UIHierarchyLevel> EnumerateFrom(UIHierarchyLevel start)
    {
        for (int level = (int)start; level <= (int)Highest; level++)
        {
            yield return (UIHierarchyLevel)level;
        }
    }
}
