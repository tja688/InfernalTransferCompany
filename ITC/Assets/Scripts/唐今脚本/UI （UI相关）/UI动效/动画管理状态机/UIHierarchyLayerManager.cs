using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class UIHierarchyLayerManager : MonoBehaviour
{
    [Header("层级设置")]
    public UIHierarchyLevel managedLevel = UIHierarchyLevel.PrimaryMenu;

    [Tooltip("默认用于播放层级切换动画的 Tween Player。")]
    public UITweenPlayer defaultTweenPlayer;

    [Tooltip("用于屏蔽下层 UI 的 CanvasGroup，可选。")]
    public CanvasGroup raycastShield;

    [Tooltip("当层级非激活时是否自动关闭射线。")]
    public bool disableRaycastWhenInactive = true;

    [Tooltip("当层级激活时是否自动打开射线。")]
    public bool enableRaycastWhenActive = true;

    [Header("过渡策略")]
    public List<UITransitionPolicy> transitionPolicies = new();

    [Header("托管对象")]
    public List<UIManagedElement> managedElements = new();

    [Header("事件")]
    public UIRouteNodeEvent onLayerEntered;
    public UIRouteNodeEvent onLayerLeft;

    private UIRouteNode _activeNode;

    private void OnEnable()
    {
        if (UIRouter.HasInstance)
        {
            UIRouter.Instance.RegisterLayerManager(this);
        }
    }

    private void Start()
    {
        if (UIRouter.HasInstance)
        {
            UIRouter.Instance.RegisterLayerManager(this);
        }
    }

    private void OnDisable()
    {
        if (UIRouter.HasInstance)
        {
            UIRouter.Instance.UnregisterLayerManager(this);
        }
    }

    internal void SyncImmediately(UIRoute snapshot)
    {
        var node = snapshot?.GetNode(managedLevel);
        bool active = node != null;
        _activeNode = node;
        ApplyActiveState(active);
        if (active)
        {
            BroadcastEntry(node);
        }
    }

    internal void CancelActiveTransitions()
    {
        if (defaultTweenPlayer != null)
        {
            defaultTweenPlayer.Kill(false);
        }

        foreach (var element in managedElements)
        {
            element.KillTweens();
        }
    }

    internal void HandleRouteWillChange(UIRouteChangeContext context)
    {
        var nextNode = context.NextNode(managedLevel);
        bool willBeActive = nextNode != null;
        if (!willBeActive)
        {
            ApplyRaycast(false);
        }
    }

    internal float HandleRouteChanged(UIRouteChangeContext context)
    {
        var previousNode = context.PreviousNode(managedLevel);
        var nextNode = context.NextNode(managedLevel);
        bool wasActive = previousNode != null;
        bool willBeActive = nextNode != null;

        _activeNode = nextNode;

        ApplyActiveState(willBeActive);

        if (willBeActive)
        {
            BroadcastEntry(nextNode);
        }
        else if (wasActive)
        {
            BroadcastExit(previousNode);
        }

        return EvaluateTransition(context);
    }

    private void ApplyActiveState(bool active)
    {
        ApplyRaycast(active);

        foreach (var element in managedElements)
        {
            element.ApplyProfile(managedLevel, active);
        }
    }

    private void ApplyRaycast(bool active)
    {
        if (raycastShield != null)
        {
            raycastShield.blocksRaycasts = active;
            raycastShield.interactable = active;
        }

        foreach (var element in managedElements)
        {
            if (active)
            {
                if (enableRaycastWhenActive)
                {
                    element.SetRaycast(true);
                }
            }
            else if (disableRaycastWhenInactive)
            {
                element.SetRaycast(false);
            }
        }
    }

    private void BroadcastEntry(UIRouteNode node)
    {
        if (node == null) return;
        if (onLayerEntered != null)
        {
            onLayerEntered.Invoke(node);
        }
    }

    private void BroadcastExit(UIRouteNode node)
    {
        if (node == null) return;
        if (onLayerLeft != null)
        {
            onLayerLeft.Invoke(node);
        }
    }

    private float EvaluateTransition(UIRouteChangeContext context)
    {
        var prevHighest = context.PreviousHighest;
        var nextHighest = context.NextHighest;
        if (prevHighest == nextHighest)
        {
            return 0f;
        }

        bool reversed = false;
        var policy = FindPolicy(prevHighest, nextHighest, out reversed);
        if (policy == null)
        {
            return 0f;
        }

        var player = policy.playerOverride != null ? policy.playerOverride : defaultTweenPlayer;
        if (player == null)
        {
            return 0f;
        }

        UITweenPreset preset = reversed ? policy.backwardPreset : policy.forwardPreset;
        if (preset == null)
        {
            return 0f;
        }

        player.Kill(false);
        player.Play(preset);

        foreach (var element in managedElements)
        {
            element.NotifyTrackTriggered(player, preset);
        }

        float overrideDuration = reversed ? policy.backwardDurationOverride : policy.forwardDurationOverride;
        if (overrideDuration > 0f)
        {
            return overrideDuration;
        }

        return preset != null ? Mathf.Max(0f, preset.duration) : 0f;
    }

    private UITransitionPolicy FindPolicy(UIHierarchyLevel prevHighest, UIHierarchyLevel nextHighest, out bool reversed)
    {
        foreach (var policy in transitionPolicies)
        {
            if (policy == null) continue;
            if (policy.fromLevel == prevHighest && policy.toLevel == nextHighest)
            {
                reversed = false;
                return policy;
            }

            if (policy.fromLevel == nextHighest && policy.toLevel == prevHighest)
            {
                reversed = true;
                return policy;
            }
        }

        reversed = false;
        return null;
    }
}

[System.Serializable]
public class UIManagedElement
{
    [Tooltip("便于识别的名称，可选。")]
    public string name;

    [Tooltip("用于控制 Raycast 的 CanvasGroup，可选。")]
    public CanvasGroup canvasGroup;

    [Tooltip("关联的状态机组件，可选。")]
    public UIStateMachine stateMachine;

    [Tooltip("在本层级激活时使用的 Profile Id，留空则走状态机的层级映射。")]
    public string profileOverrideId;

    [Tooltip("激活时是否将状态机重置到 Profile 的起始状态。")]
    public bool resetStateOnActivate = true;

    [Tooltip("当层级非激活时是否自动切回默认 Profile。")]
    public bool resetToDefaultOnDeactivate = true;

    [Tooltip("额外需要 Kill 的动画播放器列表。")]
    public List<UITweenPlayer> tweenPlayers = new();

    public void ApplyProfile(UIHierarchyLevel level, bool active)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = active;
            canvasGroup.interactable = active;
        }

        if (stateMachine != null)
        {
            if (active)
            {
                if (!string.IsNullOrEmpty(profileOverrideId))
                {
                    stateMachine.ApplyProfile(profileOverrideId, resetStateOnActivate);
                }
                else
                {
                    bool applied = stateMachine.ApplyLevelProfile(level, resetStateOnActivate);
                    if (!applied && resetStateOnActivate)
                    {
                        stateMachine.ResetToDefaultProfile(true);
                    }
                }
            }
            else if (resetToDefaultOnDeactivate)
            {
                stateMachine.ResetToDefaultProfile(true);
            }
        }
    }

    public void SetRaycast(bool enabled)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = enabled;
            canvasGroup.interactable = enabled;
        }
    }

    public void KillTweens()
    {
        if (stateMachine != null)
        {
            stateMachine.KillActiveTween();
        }

        foreach (var player in tweenPlayers)
        {
            if (player != null)
            {
                player.Kill(false);
            }
        }
    }

    public void NotifyTrackTriggered(UITweenPlayer player, UITweenPreset preset)
    {
        // 预留扩展点：此处可根据需要同步其他动画状态。
    }
}

[System.Serializable]
public class UIRouteNodeEvent : UnityEvent<UIRouteNode>
{
}
