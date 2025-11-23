using MoreMountains.Feedbacks;
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
    [SerializeField]
    MMF_Player MMFreset;
    private Vector2 dragOffset;
    public string targetType = "None";
    public string animationAName = "";
    public string animationBName = "";

    private bool isMatchedDraggingOver=false;
    public RectTransform rectTransform;

    void Awake()
    {
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


        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
           rectTransform,
           eventData.position,
           canvas.worldCamera,
           out Vector2 localClickPos))
        {
            // 偏移量 = 点击点在物体本地的位置（相对于轴心）
            dragOffset = localClickPos;
        }

        // 创建临时预览
        if (dragPreviewPrefab != null)
        {
            dragPreview = Instantiate(dragPreviewPrefab, canvas.transform);
            isMatchedDraggingOver = false;

            var previewRect = dragPreview.GetComponent<RectTransform>();
            // 关键步骤2：预览对象的初始位置 = 鼠标位置 - 偏移量（确保点击点对齐）
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                eventData.position,
                canvas.worldCamera,
                out Vector2 canvasLocalPos))
            {
                // 用鼠标当前位置减去偏移，让预览的点击点与鼠标对齐
                previewRect.anchoredPosition = canvasLocalPos - dragOffset;
            }

            //var rect = dragPreview.GetComponent<RectTransform>();
            //rect.anchoredPosition = rectTransform.anchoredPosition;
            if (dragPreview.TryGetComponent<Image>(out var previewImg))
            {
                previewImg.raycastTarget = false;

            }

            else if (dragPreview.TryGetComponent<SkeletonGraphic>(out var previewSkel))
            {
                previewSkel.raycastTarget = false;
                if (!(animationAName==""))
                previewSkel.AnimationState.SetAnimation(0, animationAName, false);
                if (!(animationBName == ""))
                    previewSkel.AnimationState.AddAnimation(0, animationBName, false, 0);
            }

        }
        if (TryGetComponent<Image>(out var image))
        {
            image.enabled = false;
        }
        else if (TryGetComponent<SkeletonGraphic>(out var skeleton))
        {
            skeleton.enabled = false;
        }
        {

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
                dragPreview.GetComponent<RectTransform>().anchoredPosition = localPos - dragOffset;
            }
            else
            {
                rectTransform.anchoredPosition = localPos - dragOffset;
            }
        }
    }

   
    public void OnEndDrag(PointerEventData eventData)
    {

        if (isDraggable != true) return;


        isDragging = false;
      if(TryGetComponent<Image>(out var image))
        {
            image.enabled = true;
        }
        else if (TryGetComponent<SkeletonGraphic>(out var skeleton))
        {
            skeleton.enabled = true;
        }



        if (dragPreview != null)
        {
            rectTransform.position = dragPreview.GetComponent<RectTransform>().position;
            Debug.Log($"Set position to target transform Position{rectTransform.position}");


            if (MMFreset != null&& !isMatchedDraggingOver)
        {
            MMFreset.PlayFeedbacks();
            Debug.Log("Play reset feedback");
        }
        else if(isMatchedDraggingOver)
        {
            GetComponent<EntryAnimation>().PlayExitAnimation();
        }
        }
        if (dragPreview != null)
        {
            Destroy(dragPreview);
        }
        SlotCenter.Instance.trigger_event(HeEventNames.EndDragEvent);

    }
}