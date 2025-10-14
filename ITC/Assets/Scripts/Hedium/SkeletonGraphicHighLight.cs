using QFramework;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 仅在拖拽时，且类型匹配才显示高亮的组件（挂载到目标对象，如打字机）
/// </summary>
[RequireComponent(typeof(SkeletonGraphic), typeof(RectTransform))]
public class DragOnlyHighlight : MonoBehaviour,
    IPointerEnterHandler, 
    IPointerExitHandler 
      
{
    private SkeletonGraphic skeleton;
    private Color originalColor;

    [Header("高亮配置")]
    public Color highlightColor = new Color(1.2f, 1.2f, 0.8f, 1);
    public string targetType = "Typewriter";

    // 标记当前是否有匹配的拖拽对象在目标上
    private bool isMatchedDraggingOver = false;

    private void Awake()
    {

        skeleton = GetComponent<SkeletonGraphic>();
        originalColor = skeleton.color;
    }
    void Start()
    {
        // 使用 lambda 包装方法，确保类型匹配 System.Action<object>
        SlotCenter.Instance.add_listener("EndDragEvent", OnRemotgeEndDrag);
    }
    /// <summary>
    /// 拖拽对象进入目标区域时触发（仅拖拽状态下）
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

    
        DraggableUI draggable = eventData.pointerDrag.GetComponent<DraggableUI>();
        if (draggable == null) return;

        // 3. 校验类型是否匹配
        if (draggable.targetType == targetType)
        {
            isMatchedDraggingOver = true;
            skeleton.color = highlightColor; // 显示高亮
        }
        else
        {
            return;
        }
    }

    /// <summary>
    /// 拖拽对象离开目标区域时触发
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
      
        if (isMatchedDraggingOver)
        {
            isMatchedDraggingOver = false;
            skeleton.color = originalColor; 
        }
        else
        {
            isMatchedDraggingOver = false;
        }
    }

    /// <summary>
    /// 拖拽结束时触发（无论是否在目标上释放）
    /// </summary>
    public void OnRemotgeEndDrag()
    {
        // 拖拽结束，强制取消高亮
        if (isMatchedDraggingOver)
        {

            isMatchedDraggingOver = false;
            skeleton.color = originalColor;
            SlotCenter.Instance.trigger_event<DocumentError>("DocumentErrorChosen", DocumentError.Stub);

        }
    }
}