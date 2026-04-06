using System;
using System.Collections;
using MoreMountains.Feedbacks;
using System.Collections.Generic;
using UnityEngine;
using DG;
using DG.Tweening;
using UnityEngine.Scripting.APIUpdating;
using System.Reflection;

namespace MoreMountains.Feedbacks
{
    /// <summary>
    /// This feedback will let you pilot a DOTweenAnimation
    /// </summary>
    [AddComponentMenu("")]
    [FeedbackHelp("This feedback will let you pilot a DOTweenAnimation")]
    [FeedbackPath("DOTween/DOTween Animation")]
    [Serializable]
    [MovedFrom(false, null, "Assembly-CSharp")]
    public class DOTweenAnimationMMFeedback : MMF_Feedback
    {
        public enum Modes { DOPlay, DOPlayBackwards, DOPlayForward, DOPause, DOTogglePause, DORewind, DORestart, DOComplete, DOKill }
        
        [Header("DOTWeen Animation")]
        
        public static bool FeedbackTypeAuthorized = true;

        [MMFInspectorGroup("DOTween Animation", true, 54, true)]
        [Tooltip("The DOTweenAnimation to control when playing this feedback")] 
        public Component TargetDOTweenAnimation;

        [Tooltip("The action to perform on the DOTweenAnimation when this feedback plays")] 
        public Modes Mode = Modes.DOPlay;

#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.AnimationColor;
        public override bool EvaluateRequiresSetup() => TargetDOTweenAnimation == null;
        public override string RequiredTargetText => TargetDOTweenAnimation != null ? TargetDOTweenAnimation.name : string.Empty;
        public override string RequiresSetupText => "This feedback requires a DOTweenAnimation reference. You can set one below.";
#endif
        
        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (!Active || !FeedbackTypeAuthorized || (TargetDOTweenAnimation == null))
            {
                return;
            }
            
            switch (Mode)
            {
                case Modes.DOPlay:
                    InvokeTweenMethod("DOPlay");
                    break;
                case Modes.DOPlayBackwards:
                    InvokeTweenMethod("DOPlayBackwards");
                    break;
                case Modes.DOPlayForward:
                    InvokeTweenMethod("DOPlayForward");
                    break;
                case Modes.DOPause:
                    InvokeTweenMethod("DOPause");
                    break;
                case Modes.DOTogglePause:
                    InvokeTweenMethod("DOTogglePause");
                    break;
                case Modes.DORewind:
                    InvokeTweenMethod("DORewind");
                    break;
                case Modes.DORestart:
                    InvokeTweenMethod("DORestart");
                    break;
                case Modes.DOComplete:
                    InvokeTweenMethod("DOComplete");
                    break;
                case Modes.DOKill:
                    InvokeTweenMethod("DOKill");
                    break;
            }
        }

        protected virtual void InvokeTweenMethod(string methodName)
        {
            MethodInfo method = TargetDOTweenAnimation.GetType().GetMethod(methodName, Type.EmptyTypes);
            if (method != null)
            {
                method.Invoke(TargetDOTweenAnimation, null);
            }
        }
    }
}
