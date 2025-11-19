using MoreMountains.Feedbacks;
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
 
    [SerializeField]
    private Material mat; 

  
    [NonSerialized]
    public bool enableHighLightOnHover=false;
    private MeshRenderer _mR;




    public MMF_Player HoverEffect;
    public MMF_Player HoverEffectRestore;
    private bool isLastTimeDirect = false;
    public bool EnableScale = true;


  


    public void DisableScale()
    {
        RestoreScaleAndRotateObj();
        EnableScale = false;

    }

    public void ScaleAndRotateObj()
    {
        if (!EnableScale)
        {
            return;
        }
        if (!isLastTimeDirect)
        {
            isLastTimeDirect = true;
            _playFeedBack();
        }
 

    }


    private void _playFeedBack()
    {
        
        HoverEffect?.PlayFeedbacks();
        HoverEffectRestore?.StopFeedbacks(); 

    }
    private void _playRestoreFeedBack()
    {
        HoverEffect.StopFeedbacks(); 
        HoverEffectRestore?.PlayFeedbacks();
    }
    public void RestoreScaleAndRotateObj()
    {
        if (!EnableScale)
        {
            return;
        }
        if (isLastTimeDirect)
        {
            isLastTimeDirect = false;
            _playRestoreFeedBack();
        }
    }


    private void Awake()
    {

        skeleton = GetComponent<SkeletonGraphic>();
    }
    void Start()
    {
     

    }


 
    public void SetHighLight()
    {

        if(mat == null)
        {
            return;
        }
        if (mat.HasProperty("_EnableHighLight "))
        {
            mat.SetFloat("_EnableHighLight", 1f);
        }
        else
        {
            Debug.LogWarning("_EnableHighLight property not found in material.");
        }
    }
    public void UnSetHighLight()
    {

        RestoreScaleAndRotateObj();
        if (mat == null)
        {
            return;
        }
        if (mat.HasProperty("_EnableHighLight "))
        {
            mat.SetFloat("_EnableHighLight", 0f);
        }
        else
        {
            Debug.LogWarning("_EnableHighLight property not found in material.");
        }
    }
    
    /// <summary>
    ///进入目标区域时触发
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        ScaleAndRotateObj();
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