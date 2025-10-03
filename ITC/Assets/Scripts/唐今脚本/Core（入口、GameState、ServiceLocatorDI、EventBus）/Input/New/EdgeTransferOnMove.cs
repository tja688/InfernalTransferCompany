using ITC.UI.Focus;
using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("Input/Legacy/Edge Transfer On Move")]
public class EdgeTransferOnMove : MonoBehaviour, IMoveHandler
{
    public enum Mode
    {
        UseExplicitDomain,
        UseMask,
        FocusDock
    }

    [Tooltip("切换逻辑模式。推荐改用新的 FocusHub 体系，本组件仅作兼容用途。")]
    public Mode mode = Mode.UseMask;

    [Tooltip("Mode = UseExplicitDomain 时指定目标面板。")]
    public FocusTag explicitDomain;

    [Tooltip("Mode = UseMask 时使用层级掩码查找目标面板。")]
    public FocusDomainMask domainMask = new FocusDomainMask(FocusTier.Base, 0);

    public MoveDirection triggerDirection = MoveDirection.Right;

    public void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir != triggerDirection) return;
        if (FocusHub.Instance == null) return;

        FocusTag target = null;
        switch (mode)
        {
            case Mode.UseExplicitDomain:
                target = explicitDomain;
                break;
            case Mode.UseMask:
                target = FocusHub.Instance.Find(domainMask);
                break;
            case Mode.FocusDock:
                target = FocusHub.Instance.Find(new FocusDomainMask(FocusTier.Base, 0));
                break;
        }

        if (target != null && FocusHub.Instance.Focus(target))
        {
            eventData.Use();
        }
    }
}
