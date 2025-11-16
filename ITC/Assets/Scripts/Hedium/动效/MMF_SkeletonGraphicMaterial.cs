using MoreMountains.Tools;
using Spine.Unity;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
    [AddComponentMenu("")]
    [FeedbackHelp("This feedback will let you change the material of the target SkeletonGraphic everytime it's played.")]
    [FeedbackPath("Hedium/SkeletonGraphic(UI)/Material")]
    public class MMF_SkeletonGraphicMaterial : MMF_Feedback
    {
        /// a static bool used to disable all feedbacks of this type at once
        public static bool FeedbackTypeAuthorized = true;

        /// use this override to specify the duration of your feedback
        public override float FeedbackDuration { get { return 0f; } }

        /// pick a color here for your feedback's inspector
#if UNITY_EDITOR
        public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
#endif

        [Header("Skeleton Graphic Material")]
        [MMFInspectorGroup("Target Settings", true, 10)]

        /// the target SkeletonGraphic component
        [Tooltip("the target SkeletonGraphic component")]
        public SkeletonGraphic TargetSkeletonGraphic;

        /// the material to apply
        [Tooltip("the material to apply")]
        public Material Material;

        protected Material _initialMaterial;
        protected bool _initialized = false;

        protected override void CustomInitialization(MMF_Player owner)
        {
            base.CustomInitialization(owner);

            // 确保 TargetSkeletonGraphic 不为空
            if (TargetSkeletonGraphic == null)
            {
                return;
            }

            // 保存初始材质
            _initialMaterial = TargetSkeletonGraphic.material;
            _initialized = true;
        }

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (!Active || !FeedbackTypeAuthorized)
            {
                return;
            }

            if (TargetSkeletonGraphic == null)
            {
                return;
            }

            if (Material == null)
            {
                return;
            }

            // 确保已经初始化
            if (!_initialized)
            {
                _initialMaterial = TargetSkeletonGraphic.material;
                _initialized = true;
            }

            // 应用新材质
            TargetSkeletonGraphic.material = Material;

            // 强制刷新渲染
            TargetSkeletonGraphic.UpdateMesh();
        }

        protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
        {
            if (!Active || !FeedbackTypeAuthorized)
            {
                return;
            }

            if (TargetSkeletonGraphic != null && _initialized)
            {
                TargetSkeletonGraphic.material = _initialMaterial;
                TargetSkeletonGraphic.UpdateMesh();
            }
        }

        protected override void CustomRestoreInitialValues()
        {
            if (!Active || !FeedbackTypeAuthorized)
            {
                return;
            }

            if (TargetSkeletonGraphic != null && _initialized)
            {
                TargetSkeletonGraphic.material = _initialMaterial;
                TargetSkeletonGraphic.UpdateMesh();
            }
        }
    }
}