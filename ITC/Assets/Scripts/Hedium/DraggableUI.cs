using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(RectTransform))] 
public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("拖动时显示的临时预览（可选，优化视觉效果）")]
    public GameObject dragPreviewPrefab;

    private RectTransform _rectTransform; 
    private Canvas canvas;
    private GameObject dragPreview; // 拖动时的临时显示对象
    private bool isDragging = false;
   

    public string targetType = "Typewriter";
    

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

    // 拖动中（更新位置）
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // 将鼠标位置转换为Canvas内的UI位置
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

    // 结束拖动
    public void OnEndDrag(PointerEventData eventData)
    {




        isDragging = false;
        // 恢复原对象显示
        GetComponent<Image>().enabled = true;
       

        if (dragPreview != null)
        {
            Destroy(dragPreview);
        }
        SlotCenter.Instance.trigger_event("EndDragEvent");

    }
}