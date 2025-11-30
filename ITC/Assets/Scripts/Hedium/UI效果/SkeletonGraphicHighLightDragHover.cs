using MoreMountains.Feedbacks;
using QFramework;
using Spine.Unity;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static EffectToolManager;
/// <summary>
/// 仅在拖拽时，且类型匹配才显示高亮的组件（挂载到目标对象，如打字机）
/// </summary>
[RequireComponent(typeof(SkeletonGraphic), typeof(RectTransform))]
public class SkeletonGraphicHighLightDragHover : MonoBehaviour,
    IPointerEnterHandler, 
    IPointerExitHandler
    //IUIHeEventMetadata<DocumentError>

{
    public string eventName = "EndDragUpThePneumaticChannelSkeleton";
    [SerializeField]

    public MMF_Player HoverEffect;
    [SerializeField]

    public MMF_Player HoverRestoreEffect;
    

    public DocumentError error= DocumentError.Stub;
    //public DocumentError error = DocumentError.NoPassStub;
    private SkeletonGraphic skeleton;

    private Material mat;
    public string targetType = "DocumentJudge";

    





    // 标记当前是否有匹配的拖拽对象在目标上
    private bool isMatchedDraggingOver = false;

    public EffectToolManager.EffectTurn effectTurn;









    private bool isDirect = true;

    public bool EnableScale = true;









    private void Awake()
    {
        effectTurn= new EffectToolManager.EffectTurn(HoverEffect, HoverRestoreEffect);
        skeleton = GetComponent<SkeletonGraphic>();
       
        
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

        // 3. 校验类型是否匹配
        if (draggable.targetType == targetType)
        {
            isMatchedDraggingOver = true;
            SlotCenter.Instance.trigger_event(HeEventNames.OnMatchedDraggingOver);
            SetEffect();
        }
        else
        {
            Debug.Log("Type does not match. No highlight applied.");

            return;
        }
    }

    private void SetEffect()
    {
        if (mat != null)
        {

            Debug.Log("Setting highlight effect via Material.");

            if (mat.HasProperty("_EnableHighLight "))
            {
                mat.SetFloat("_EnableHighLight", 1f);
            }
            else
            {
                Debug.LogWarning("_EnableHighLight property not found in material.");
            }



            return;
        }
        else if (HoverEffect!=null)
        {

            effectTurn.TurnOn();
            Debug.Log("Setting highlight effect via HoverEffect.");
        }
        else
        {
            Debug.Log("Both material and HoverEffect are null. Cannot set highlight effect.暂时不用管");
        }
      
    }
    private void UnSetEffect()
    {
        if(mat == null)
        {
            effectTurn.TurnOff();
            return; 
        }
        if (mat.HasProperty("_EnableHighLight "))
        {
            mat.SetFloat("_EnableHighLight", 0f);
        }
        else
        {
            Debug.Log("_EnableHighLight property not found in material.");
        }
    }
    /// <summary>
    /// 拖拽对象离开目标区域时触发
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {

        Debug.Log("Pointer exited the target area.");
        if (isMatchedDraggingOver)
        {
            isMatchedDraggingOver = false;
            UnSetEffect();
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
            UnSetEffect();
            SlotCenter.Instance.trigger_event<DocumentError>(eventName, error);
        }
        else
        {
            Debug.Log("No matched dragging over. No highlight to remove.");
        }
    }
}