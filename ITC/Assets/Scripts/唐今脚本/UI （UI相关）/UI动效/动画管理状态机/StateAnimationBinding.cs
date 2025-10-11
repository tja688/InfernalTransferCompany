using UnityEngine;

[System.Serializable]
public class StateAnimationBinding
{
    public UIState state; // 这个绑定对应哪个状态

    [Tooltip("当进入此状态时播放的动画 Preset")]
    public UITweenPreset onEnterPreset;

    [Tooltip("当退出此状态时播放的动画 Preset (可选)")]
    public UITweenPreset onExitPreset;
    
    // 思考：onExitPreset 很多时候是 onEnterPreset 的反向播放。
    // 我们可以增加一个bool来简化配置。
    [Tooltip("如果勾选，退出状态时将反向播放 OnEnter Preset，忽略 OnExit Preset")]
    public bool reverseOnExit = true;
}