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
    private void Awake()
    {

        skeleton = GetComponent<SkeletonGraphic>();
        mat = skeleton.material;
    }
    void Start()
    {
     

    }


 
    public void SetHighLight()
    {


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
        UnSetHighLight();
    }

  

}