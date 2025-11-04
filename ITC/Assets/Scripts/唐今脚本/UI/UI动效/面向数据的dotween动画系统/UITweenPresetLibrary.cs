// MIT License
// Optional: a name→preset registry for convenient lookups

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UITweenPresetLibrary", menuName = "UI Tween/Preset Library", order = 1001)]
public class UITweenPresetLibrary : ScriptableObject
{
    [Tooltip("把常用的 Preset 拖进来，按名字检索。")]
    public List<UITweenPreset> items = new List<UITweenPreset>();

    Dictionary<string, UITweenPreset> _map;

    void OnEnable() { BuildMap(); }
    public void BuildMap()
    {
        _map = new Dictionary<string, UITweenPreset>();
        foreach (var p in items)
        {
            if (p == null) continue;
            if (string.IsNullOrEmpty(p.presetName)) continue;
            _map[p.presetName] = p; // 后者覆盖前者，确保唯一
        }
    }

    public bool TryGet(string presetName, out UITweenPreset preset)
    {
        if (_map == null) BuildMap();
        return _map.TryGetValue(presetName, out preset);
    }
}