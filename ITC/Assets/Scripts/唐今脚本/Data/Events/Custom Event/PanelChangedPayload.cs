using System;

/// <summary>
/// 面板切换事件的数据载体。
/// </summary>
[Serializable]
public struct PanelChangedPayload
{
    public string NewPanelName;
    public string PreviousPanelName;

    public PanelChangedPayload(string newPanel, string previousPanel)
    {
        NewPanelName = newPanel;
        PreviousPanelName = previousPanel;
    }
}

