using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Spine.AnimationState;
public class SkeletonClick : MonoBehaviour,
    IPointerDownHandler
    
{
    // Start is called before the first frame update 
    [SerializeField]
    private string animationNameClick;
    [SerializeField]
    private string animationNameRelease;
    [SerializeField]
    public bool enableClick=true;

    StampType type;


    private bool isClicking = false;
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
            
        }
        if (enableClick == false)
        {
            return;
        }
        //Debug.Log("animationNameClick");    
        GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, animationNameClick, false);
        var track = GetComponent<SkeletonGraphic>().AnimationState.AddAnimation(0, animationNameRelease, false, 0);


        enableClick = false;
        isClicking = true;

        track.Complete += OnAnimationComplete;
    }

 

    void OnAnimationComplete(Spine.TrackEntry trackEntry)
    {
        SlotCenter.Instance.trigger_event<StampType>(HeEventNames.ChosenStampType, type);
    }
    //void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    //{

    //    if (isClicking == true)
    //    {
    //        isClicking = false;
    //    }

    //    if(enableRelease)
    //    if (eventData.button == PointerEventData.InputButton.Left)
    //    {
    //        enableRelease = false;
    //            Debug.Log("animationNameRelease");
    //            GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, animationNameRelease, false);
    //     }
    //}




}
