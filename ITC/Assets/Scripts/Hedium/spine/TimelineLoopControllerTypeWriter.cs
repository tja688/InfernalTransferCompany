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
    private SkeletonGraphic skeletonGraphic;
    private void Awake()
    {
        skeletonGraphic = GetComponent<SkeletonGraphic>();
        director = GetComponent<PlayableDirector>(); ;

    }
    private void Start()
    {
        
        SlotCenter.Instance.add_listener(HeEventNames.LetStopTypeWriter, StopLoop);
        SlotCenter.Instance.add_listener(HeEventNames.LetStartTypeWriter, StartLoop);
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
        
        if (loop)
        {
            director.time = 5.9334;  
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
