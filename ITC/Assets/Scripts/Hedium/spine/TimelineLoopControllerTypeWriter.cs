using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using static UnityEngine.EventSystems.EventTrigger;
[RequireComponent(typeof(PlayableDirector), typeof(SkeletonGraphic))]
public class TimelineLoopControllerTypeWriter : MonoBehaviour
{
    //UTF8编码
    private PlayableDirector director;
    public AnimationReferenceAsset EndAnimation;
    public bool loop = true;
    public float loopPoint = 4.0333f;
    public bool thisTurnIsNotBlock = false;
    public float LineBreakPoint = 5.133333f;
    private SkeletonGraphic skeletonGraphic;
    public bool enablePause = true;
    private bool disablePauseOnce = false;
    private void Awake()
    {
        skeletonGraphic = GetComponent<SkeletonGraphic>();
        director = GetComponent<PlayableDirector>(); ;

    }
    private void Start()
    {

        SlotCenter.Instance.add_listener(HeEventNames.LetStopTypeWriter, StopLoop);
        SlotCenter.Instance.add_listener(HeEventNames.LetStartTypeWriter, StartLoop);
        SlotCenter.Instance.add_listener(HeEventNames.LetContinueTypeWriter, ContinueLoop);
        SlotCenter.Instance.add_listener(HeEventNames.LetLineBreakTypeWriter, LineBreak);


    }

    public void LineBreak()
    {

        if (loop)
        {
            //director.time = LineBreakPoint;




            disablePauseOnce = true;




            director.Play();
        }
    }
    public void OnPausePoint()
    {
        if(enablePause&& !disablePauseOnce)
        PauseLoop();
    }
    private void OnTypeWriterEndType(TrackEntry entry)
    {
        SlotCenter.Instance.trigger_event(HeEventNames.OnTypeWriterEndType);
        entry.Complete -= OnTypeWriterEndType;
    }
    public void EmitIsReadyTypeWriter()
    {
        SlotCenter.Instance.trigger_event(HeEventNames.OnIsReadyTypeWriter);
    }
    public void OnCycleEndSignal()
    {
        disablePauseOnce = false;
        if (loop)
        {
            director.time = loopPoint;  
            director.Play();  
        }
    }
    private void PauseLoop()
    {
        if (director != null && director.state == PlayState.Playing)
        {
            director.Pause();

        }
        else
        {
            Debug.LogWarning("PlayableDirector is null or not playing.");
        }
    }
    /// <summary>
    /// 暂停后继续播放下一个动画
    /// </summary>
    private void ContinueLoop()
    {
        if (director != null && director.state == PlayState.Paused&&loop)
        {
            director.Play();
        }
    }
    private void StopLoop()
    {
        if (director != null && director.state == PlayState.Playing)
        {
            director.Pause(); 
        }

        if (   EndAnimation != null)
        {
            TrackEntry entry =  skeletonGraphic.AnimationState.AddAnimation(0, EndAnimation, true, 0);
            entry .Complete += OnTypeWriterEndType;
        }
        loop = false;
    }
    private void StartLoop()
    {
        loop = true;
        director.Play();
        director.Evaluate();
    }
}
