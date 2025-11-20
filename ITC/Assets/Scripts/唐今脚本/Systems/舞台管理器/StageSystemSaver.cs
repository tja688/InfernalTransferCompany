using UnityEngine;
using PixelCrushers;

public class StageSystemSaver : Saver
{
    public override string RecordData()
    {
        if (StageManager.Instance == null) return string.Empty;
        return StageManager.Instance.RecordData();
    }

    public override void ApplyData(string s)
    {
        if (StageManager.Instance == null || string.IsNullOrEmpty(s)) return;
        StageManager.Instance.RestoreState(s);
    }
}
