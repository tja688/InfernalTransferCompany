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
    private Image image;
    [SerializeField]
    private Material mat;
    public string targetType = "DocumentJudge";

    // 标记当前是否有匹配的拖拽对象在目标上
    private bool isMatchedDraggingOver = false;
    
    private void Awake()
    {
        image = GetComponent<Image>();
        mat = image.material;
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


        if (mat.HasProperty("_EnableHighLight"))
        {
            //Debug.Log("Seting HighLight called,");
            mat.SetFloat("_EnableHighLight", 1f);
            //Debug.Log("SetHighLight called, _EnableHighLight set to 1");
        }
        else
        {
            //Debug.Log("_EnableHighLight property not found in material.");
        }
    }
    private void UnSetHighLight()
    {

        if (mat.HasProperty("_EnableHighLight"))
        {
            mat.SetFloat("_EnableHighLight", 0f);
        }
        else
        {
            //Debug.Log("_EnableHighLight property not found in material.");
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