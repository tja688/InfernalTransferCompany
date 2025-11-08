// ---- GameSystemEvents.cs ----
// 这个文件专门存放与游戏系统、流程相关的事件

using MoreMountains.Tools; 
using UnityEngine;

/// <summary>
/// 游戏开始事件
/// </summary>
public struct GameStartEvent
{
}

/// <summary>
/// 触发此事件以暂停游戏
/// </summary>
public struct GamePausedEvent
{
    public bool IsPaused; 
}

/// <summary>
/// 游戏结束事件
/// </summary>
public struct GameOverEvent
{
    public int FinalScore;
}

/// <summary>
/// 触发此事件以请求开始一个场景转场
/// </summary>
public struct SceneTransEvent
{
}

/// <summary>
/// 请求切换面板的事件。
/// </summary>
public struct RequestPanelChangeEvent
{
    /// <summary>
    /// 想要切换到的目标面板的名称 (必须在 GamePanelLibrarySO 中定义)。
    /// </summary>
    public string TargetPanelName;
}

/// <summary>
/// 当面板状态机成功切换面板后广播的事件。
/// </summary>
public struct PanelChangedEvent
{
    /// <summary>
    /// 切换到的新面板名称。
    /// </summary>
    public string NewPanelName;
    
    /// <summary>
    /// 切换前的旧面板名称。
    /// </summary>
    public string PreviousPanelName;
}