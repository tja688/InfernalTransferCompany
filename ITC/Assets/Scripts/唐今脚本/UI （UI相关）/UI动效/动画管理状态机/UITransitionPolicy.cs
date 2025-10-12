using UnityEngine;

[System.Serializable]
public class UITransitionPolicy
{
    [Tooltip("路由最高层级从此层级切换到 ToLevel 时触发正向动画。")]
    public UIHierarchyLevel fromLevel = UIHierarchyLevel.GameUI;

    [Tooltip("路由最高层级切换到此层级时触发正向动画。")]
    public UIHierarchyLevel toLevel = UIHierarchyLevel.PrimaryMenu;

    [Tooltip("当匹配 from->to 时播放的动画预设。")]
    public UITweenPreset forwardPreset;

    [Tooltip("正向动画时使用的自定义时长（<=0 使用预设时长）。")]
    public float forwardDurationOverride = -1f;

    [Tooltip("当路由最高层级从 ToLevel 回到 FromLevel 时播放的动画预设。")]
    public UITweenPreset backwardPreset;

    [Tooltip("反向动画时使用的自定义时长（<=0 使用预设时长）。")]
    public float backwardDurationOverride = -1f;

    [Tooltip("优先使用此播放器触发动画，留空则使用管理器默认播放器。")]
    public UITweenPlayer playerOverride;
}
