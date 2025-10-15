using Spine.Unity;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(RectTransform))] 
public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("拖动时显示的临时预览")]
    public GameObject dragPreviewPrefab;
    [NonSerialized]
    public bool isDraggable=false;
    private RectTransform _rectTransform; 
    private Canvas canvas;
    private GameObject dragPreview; // 拖动时的临时显示对象
    private bool isDragging = false;
    

    public string targetType = "DocumentJudge";
    

    private RectTransform rectTransform;
  
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>(); // 获取父级Canvas
    }
    void Start()
    {
        
    }
    // 开始拖动
    public void OnBeginDrag(PointerEventData eventData)
    {

        if (isDraggable == false) return;
        isDragging = true;

        GetComponent<Image>().enabled = false;

        // 创建临时预览
        if (dragPreviewPrefab != null)
        {
            dragPreview = Instantiate(dragPreviewPrefab, canvas.transform);

            var rect = dragPreview.GetComponent<RectTransform>();
            rect.anchoredPosition = rectTransform.anchoredPosition;
            if (dragPreview.TryGetComponent<Image>(out var previewImg))
            {
                previewImg.raycastTarget = false; 
            }
          
            else if (dragPreview.TryGetComponent<SkeletonGraphic>(out var previewSkel))
            {
                previewSkel.raycastTarget = false; 
            }

        }
    }

   
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

      
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPos))
        {
        
            if (dragPreview != null)
            {
                dragPreview.GetComponent<RectTransform>().anchoredPosition = localPos;
            }
            else
            {
                rectTransform.anchoredPosition = localPos;
            }
        }
    }

   
    public void OnEndDrag(PointerEventData eventData)
    {

        if (isDraggable != true) return;


        isDragging = false;
     
        GetComponent<Image>().enabled = true;
       

        if (dragPreview != null)
        {
            Destroy(dragPreview);
        }
        SlotCenter.Instance.trigger_event(HeEventNames.EndDragEvent);

    }
}