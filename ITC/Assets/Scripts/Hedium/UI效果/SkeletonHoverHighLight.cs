using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
/// </summary>
[RequireComponent(typeof(SkeletonGraphic), typeof(RectTransform))]

public class SkeletonHoverHighLight : MonoBehaviour,
   IPointerEnterHandler,
    IPointerExitHandler

{
    private SkeletonGraphic skeleton;
    private Color originalColor;

    [Header("高亮配置")]
    public Color highlightColor = new Color(1.2f, 1.2f, 0.8f, 1);
  
    [NonSerialized]
    public bool enableHighLightOnHover=false;


    private void Awake()
    {

        skeleton = GetComponent<SkeletonGraphic>();
        originalColor = skeleton.color;
    }
    void Start()
    {

     
    }
    public void SetHighLight()
    {
        skeleton.color = highlightColor; 
    }
    public void UnSetHighLight()
    {
        skeleton.color = originalColor;
    }
    
    /// <summary>
    ///进入目标区域时触发
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (enableHighLightOnHover)
        {
            skeleton.color = highlightColor; // 显示高亮

        }

    }

    /// <summary>
    ///离开目标区域时触发
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
            skeleton.color = originalColor;
    }

  

}