using PixelCrushers;
using UnityEngine;
using UnityEngine.EventSystems;

public class EdgeTransferOnMove : MonoBehaviour, IMoveHandler
{
    public enum Mode { ToRegisteredKey, ToDock }

    public Mode mode = Mode.ToRegisteredKey;
    public FocusKey registeredKey = FocusKey.DialogueMenu;  // 默认交接到“对话菜单”
    public MoveDirection triggerDirection = MoveDirection.Right;

    public void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir != triggerDirection) return;

        if (mode == Mode.ToRegisteredKey)
        {
            FocusHub.Instance?.Focus(registeredKey);
        }
        else // 回 Dock
        {
            FocusHub.Instance?.Focus(FocusKey.Dock);
        }

        eventData.Use();
    }
}