
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace MoreMountains.Feedbacks
{
    [AddComponentMenu("")]
    [FeedbackPath("Hedium/CUIAnimator")]
    [FeedbackHelp("Controls a CUIGraphicOmniAnimator using MMF_Feedback interface.")]
    public class MMF_CUIOmniAnimator : MMF_Feedback
    {
        public static bool FeedbackTypeAuthorized = true;

#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.UIColor;
#endif
        [MMFInspectorGroup("Target", true, 10, true)]
        [Tooltip("The CUIGraphicOmniAnimator to control")]
        public CUIGraphicOmniAnimator TargetAnimator;

        [MMFInspectorGroup("Animation Settings", true, 11, true)]
        [Tooltip("Which animation to play")]
        public AnimationType Animation = AnimationType.Bounce;

        [MMFInspectorGroup("Animation Settings", true, 12, true)]
        [Tooltip("Duration of the animation in seconds")]
        public float Duration = 1f;

        [MMFInspectorGroup("Animation Settings", true, 13, true)]
        [Tooltip("Enable smooth interpolation")]
        public bool Interpolate = true;

        [MMFInspectorGroup("Animation Settings", true, 14, true)]
        [Tooltip("Pulse frequency if Pulse animation is selected")]
        public float PulseFrequency = 1f;
       

        // no local coroutines anymore; animations are driven directly on the target animator
        [SerializeField]
        public enum AnimationType
        {
            HorizontalStretch,
            VerticalSquash,
            Bulge,
            Shrink,
            Bounce,
            Wave,
            Jiggle,
            Pulse,
            ToOriginal
        }

        public override float FeedbackDuration
        {
            get
            {
                if (!Interpolate) return 0f;
                return Duration;
            }
            set
            {
                Duration = value;
            }
        }

        protected override void CustomInitialization(MMF_Player owner)
        {
            base.CustomInitialization(owner);
            if (TargetAnimator != null)
            {
                TargetAnimator.SaveOriginalPoints();
            }
        }

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!Active || !FeedbackTypeAuthorized || TargetAnimator == null)
                return;

            // 停止目标动画（如果有），直接在 TargetAnimator 上调用动画方法
            TargetAnimator.StopCurrentAnimation();

            switch (Animation)
            {
                case AnimationType.HorizontalStretch:
                    TargetAnimator.AnimateHorizontalStretch(Duration);
                    break;
                case AnimationType.VerticalSquash:
                    TargetAnimator.AnimateVerticalSquash(Duration);
                    break;
                case AnimationType.Bulge:
                    TargetAnimator.AnimateBulge(Duration);
                    break;
                case AnimationType.Shrink:
                    TargetAnimator.AnimateShrink(Duration);
                    break;
                case AnimationType.Bounce:
                    TargetAnimator.AnimateBounce(Duration);
                    break;
                case AnimationType.Wave:
                    TargetAnimator.AnimateWave(Duration);
                    break;
                case AnimationType.Jiggle:
                    TargetAnimator.AnimateJiggle(Duration * 1.5f);
                    break;
                case AnimationType.Pulse:
                    TargetAnimator.StartPulseAnimation(PulseFrequency);
                    break;
                case AnimationType.ToOriginal:
                    TargetAnimator.AnimateToOriginal(Duration);
                    break;
            }
        }

        protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!Active || !FeedbackTypeAuthorized || TargetAnimator == null)
                return;

            // 停止目标上的动画
            TargetAnimator.StopCurrentAnimation();
        }

        protected override void CustomRestoreInitialValues()
        {
            if (TargetAnimator != null)
            {
                TargetAnimator.RestoreOriginalPoints();
            }
        }



      
    }
}
