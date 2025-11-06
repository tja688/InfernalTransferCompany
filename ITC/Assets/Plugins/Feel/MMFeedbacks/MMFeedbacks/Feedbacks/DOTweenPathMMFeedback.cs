using System;
using System.Collections;
using MoreMountains.Feedbacks;
using System.Collections.Generic;
using UnityEngine;
using DG;
using DG.Tweening;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
    /// <summary>
    /// This feedback will let you pilot a DOTweenPath
    /// </summary>
    [AddComponentMenu("")]
    [FeedbackHelp("This feedback will let you pilot a DOTweenPath")]
    [FeedbackPath("DOTween/DOTween Path")]
    [Serializable]
    [MovedFrom(false, null, "Assembly-CSharp")]
    public class DOTweenPathMMFeedback : MMF_Feedback
    {
        public enum Modes { DOPlay, DOPlayBackwards, DOPlayForward, DOPause, DOTogglePause, DORewind, DORestart, DOComplete, DOKill }

        [Header("DOTWeen Path")]

        public static bool FeedbackTypeAuthorized = true;

        [MMFInspectorGroup("DOTween Path", true, 54, true)]
        [Tooltip("The DOTweenPath to control when this feedback plays")]
        public DOTweenPath TargetDOTweenPath;

        [Tooltip("The action to perform on the DOTweenPath when this feedback plays")]
        public Modes Mode = Modes.DOPlay;

#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.AnimationColor;
        public override bool EvaluateRequiresSetup() => TargetDOTweenPath == null;
        public override string RequiredTargetText => TargetDOTweenPath != null ? TargetDOTweenPath.name : string.Empty;
        public override string RequiresSetupText => "This feedback requires a DOTweenPath reference. You can set one below.";
#endif

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (!Active || !FeedbackTypeAuthorized || (TargetDOTweenPath == null))
            {
                return;
            }

            switch (Mode)
            {
                case Modes.DOPlay:
                    TargetDOTweenPath.DOPlay();
                    break;
                case Modes.DOPlayBackwards:
                    TargetDOTweenPath.DOPlayBackwards();
                    break;
                case Modes.DOPlayForward:
                    TargetDOTweenPath.DOPlayForward();
                    break;
                case Modes.DOPause:
                    TargetDOTweenPath.DOPause();
                    break;
                case Modes.DOTogglePause:
                    TargetDOTweenPath.DOTogglePause();
                    break;
                case Modes.DORewind:
                    TargetDOTweenPath.DORewind();
                    break;
                case Modes.DORestart:
                    TargetDOTweenPath.DORestart();
                    break;
                case Modes.DOComplete:
                    TargetDOTweenPath.DOComplete();
                    break;
                case Modes.DOKill:
                    TargetDOTweenPath.DOKill();
                    break;
            }

        }
    }
}
