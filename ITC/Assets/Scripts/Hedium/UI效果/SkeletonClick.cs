using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using MoreMountains.Feedbacks;
public class SkeletonClick : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler

{
    // Start is called before the first frame update 
    [SerializeField]
    private string animationNameClick;
    [SerializeField]
    private string animationNameRelease;
    [SerializeField]
    public bool enableOnceClick=true;
    [SerializeField]
    public MMF_Player clickMMf;

    [SerializeField]
    public MMF_Player clickMMfRestore;
    public bool IsStamp = true;
    StampType type;


    private bool isClicking = false;
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
            
        }
        if (enableOnceClick == false)
        {
            return;
        }


        if(IsStamp)
       { //Debug.Log("animationNameClick");    
            GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, animationNameClick, false);
            var track = GetComponent<SkeletonGraphic>().AnimationState.AddAnimation(0, animationNameRelease, false, 0);
            track.Complete += OnAnimationComplete;

        }
        else
        {
            clickMMf.PlayFeedbacks();
        }

        enableOnceClick = false;
        isClicking = true;

    }

 

    void OnAnimationComplete(Spine.TrackEntry trackEntry)
    {
        if(IsStamp)
        SlotCenter.Instance.trigger_event<StampType>(HeEventNames.ChosenStampType, type);
    }
    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        if(!IsStamp)
        if (isClicking == true)
        {
            isClicking = false;
            clickMMfRestore.PlayFeedbacks();




            }
            else
        {
            Debug.Log("Not Clicked");
        }
     
    }




}
