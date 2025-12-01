using MoreMountains.Feedbacks;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static EffectToolManager;
/// </summary>
[RequireComponent(typeof(SkeletonGraphic), typeof(RectTransform))]

public class SkeletonHoverHighLight : MonoBehaviour,
   IPointerEnterHandler,
    IPointerExitHandler

{
    private SkeletonGraphic skeleton;
    private Color originalColor;
 

  
    [NonSerialized]
    public bool enableHighLightOnHover=false;
    private MeshRenderer _mR;




    public MMF_Player HoverEffect;
    public MMF_Player HoverEffectRestore;
    public EffectToolManager.EffectTurn effectTurn;

  






   


    private void Awake()
    {
        effectTurn = new EffectTurn
        (HoverEffect,
            HoverEffectRestore);
          
       
        skeleton = GetComponent<SkeletonGraphic>();
    }
    void Start()
    {
     

    }


 
    public void SetHighLight()
    {

    
    }
    public void UnSetHighLight()
    {

        effectTurn.TurnOff();
      
    }
    
    /// <summary>
    ///进入目标区域时触发
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        effectTurn.TurnOn();
        if (enableHighLightOnHover)
        {
            SetHighLight();

        }

    }

    /// <summary>
    ///离开目标区域时触发
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("OnPointerExit");
        UnSetHighLight();
    }

  

}