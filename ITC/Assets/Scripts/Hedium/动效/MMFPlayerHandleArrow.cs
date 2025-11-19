using MoreMountains.Feedbacks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MMFPlayerHandleArrow : MonoBehaviour
{
    // Start is called before the first frame update 
    //UTF8编码
    [SerializeField]
    private MMF_Player Enter;
    [SerializeField]
    private MMF_Player Exit;
    [SerializeField]
    private MMF_Player Breath;
    [SerializeField]
    private MMF_Player Fail;




    public void SetEnterPosition(Vector3 EnterPoint,Vector3 InitPointer,float TargetScale)
    {
        Enter.GetFeedbackOfType<MMF_Position>().InitialPosition = InitPointer;
        Enter.GetFeedbackOfType<MMF_Position>().DestinationPosition = EnterPoint;


        Enter.GetFeedbackOfType<MMF_Scale>().RemapCurveOne = TargetScale;

    }
    void PlayEnter()
    {
        if (!Enter)
        {
            Debug.LogError("No Enter Feedbacks assigned");
        }
        else
            Enter.PlayFeedbacks();
    }

    void PlayExit()
    {
        if (!Exit)
        {
            Debug.LogError("No Exit Feedbacks assigned");
        }
        else
            Exit.PlayFeedbacks();


        Debug.Log("PlayExit Feedbacks Called");
    }

    void PlayBreath()
    {
        if (!Breath)
        {

            Debug.LogError("No Breath Feedbacks assigned");
        }
        else
            Breath.PlayFeedbacks();


    }


    void PlayFail()
    {
        if (Fail)
        {
            Fail.PlayFeedbacks();
        }
        else
            Debug.LogError("No Fail Feedbacks assigned");
    }

    void StopBreath()
    {
        if (Breath)
        {
            Breath.StopFeedbacks();

        }
        else
        {
            Debug.LogError("No Breath Feedbacks assigned");
        }
    }

    public void Play()
    {

        PlayEnter();
        

    }
    


    public void OnExitCompeleted()
    {
                //Destroy(this);
    }
    public void OnEnterCompeleted()
    {
        PlayBreath();
        SlotCenter.Instance.trigger_event(HeEventNames.OnSpawnRuneArrowsEnd);

    }
   

    public void SuccessFade()
    {
        StopBreath();
        PlayExit();

    }

    public void FaildFade()
    {
        StopBreath();

        PlayFail();
    }
}
