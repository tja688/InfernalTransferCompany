#if UNITY_EDITOR
using System;
using System.Collections.Generic;

internal readonly struct UITweenPresetOption
{
    public string Name { get; }
    public UITweenPreset Preset { get; }

    public UITweenPresetOption(string name, UITweenPreset preset)
    {
        Name = name;
        Preset = preset;
    }
}

internal static class UITweenEditorUtility
{
    public static List<UITweenPresetOption> GetPresetOptions(UITweenPlayer player)
    {
        var options = new List<UITweenPresetOption>();
        if (player == null)
        {
            return options;
        }

        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        void AddPreset(UITweenPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            string displayName = string.IsNullOrEmpty(preset.presetName) ? preset.name : preset.presetName;
            if (!seenNames.Add(displayName))
            {
                return;
            }

            options.Add(new UITweenPresetOption(displayName, preset));
        }

        foreach (var preset in player.presets)
        {
            AddPreset(preset);
        }

        foreach (var library in player.libraries)
        {
            if (library == null)
            {
                continue;
            }

            foreach (var preset in library.items)
            {
                AddPreset(preset);
            }
        }

        options.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return options;
    }
}
#endif
