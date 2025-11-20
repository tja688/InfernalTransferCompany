using MoreMountains.Feedbacks;
using QFramework;
using Spine.Unity;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 仅在拖拽时，且类型匹配才显示高亮的组件（挂载到目标对象，如打字机）
/// </summary>
//[RequireComponent(typeof(Image), typeof(RectTransform))]
public class ImageHighLightDragHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public string eventName = "EndDragUpThePneumaticChannelSkeleton";
  
    public DocumentError error = DocumentError.Stub;
    public string targetType = "DocumentJudge";
    [SerializeField]
    public MMF_Player HoverEffect;
    [SerializeField]
    public MMF_Player HoverRestoreEffect;
    // 标记当前是否有匹配的拖拽对象在目标上
    private bool isMatchedDraggingOver = false;
    public EffectToolManager.EffectTurn effectTurn;
    private void Awake()
    {
        effectTurn=new EffectToolManager.EffectTurn(HoverEffect, HoverRestoreEffect);
    }
    
    void Start()
    {
        SlotCenter.Instance.add_listener(HeEventNames.EndDragEvent, OnRemotgeEndDrag);
        
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
        Debug.Log("PointerEnter and Drag");
        // 校验类型是否匹配
        if (draggable.targetType == targetType)
        {
            isMatchedDraggingOver = true;
            SetHighLight();
        }
        else
        {
            //Debug.Log("Type does not match. No highlight applied.");
            return;
        }



    }

    private void SetHighLight()
    {

        effectTurn.TurnOn();
    
    }
    private void UnSetHighLight()
    {
        effectTurn.TurnOff();
       
    }
    /// <summary>
    /// 拖拽对象离开目标区域时触发
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {

        if (isMatchedDraggingOver)
        {
            isMatchedDraggingOver = false;
            UnSetHighLight();
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
            UnSetHighLight();
            SlotCenter.Instance.trigger_event<DocumentError>(HeEventNames.DocumentErrorChosen, DocumentError.NoPass);
        }
    }   
}