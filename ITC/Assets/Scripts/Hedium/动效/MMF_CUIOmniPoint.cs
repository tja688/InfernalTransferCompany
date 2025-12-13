using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
namespace MoreMountains.Feedbacks
{
    [AddComponentMenu("")]
    [FeedbackHelp("Animates all 16 control points on CUIGraphicOmniDirectional. When StopIsCapture is true, stopping will capture current positions.")]
    [FeedbackPath("Hedium/CUI/CurvePoints")]
    public class MMF_CUIOmniPoint : MMF_Feedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
        public override bool EvaluateRequiresSetup() { return TargetComponent == null; }
        public override string RequiredTargetText { get { return TargetComponent != null ? TargetComponent.name : ""; } }
        public override string RequiresSetupText { get { return "TargetComponent (CUIGraphicOmniDirectional) must be set. Stop can capture positions if enabled."; } }
        public override bool HasCustomInspectors { get { return false; } }
#endif
        public override bool HasRandomness { get { return false; } }
        public override bool HasAutomatedTargetAcquisition { get { return true; } }
        protected override void AutomateTargetAcquisition() { TargetComponent = FindAutomatedTarget<CUIGraphicOmniDirectional>(); }

        [Serializable]
        public class ExtraCUIComponentData { public CUIGraphicOmniDirectional TargetComponent; }

        [MMFInspectorGroup("Target", true, 12)]
        public CUIGraphicOmniDirectional TargetComponent;
        public List<ExtraCUIComponentData> ExtraTargetComponents;

        [MMFInspectorGroup("Capture Settings", true, 13)]
        [Tooltip("When true, calling Stop on this feedback will capture current positions into NewPositions array.")]
        public bool StopIsCapture = false;

        [MMFInspectorGroup("Target Positions (16 Points)", true, 14)]
        [Tooltip("Target positions for all 16 control points [Curve0..3, Point0..3].")]
        public Vector3[] NewPositions = new Vector3[16];

        [MMFInspectorGroup("Interpolation", true, 15)]
        public bool InterpolateValue = true;
        [MMFCondition("InterpolateValue", true)]
        public float Duration = 2f;
        [MMFCondition("InterpolateValue", true)]
        public float ValueMultiple = 1f;
        public MMTweenType InterpolationCurve = new MMTweenType(
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)),
            "InterpolateValue"
        );

        public override float FeedbackDuration { get { return InterpolateValue ? ApplyTimeMultiplier(Duration) : 0f; } set { if (InterpolateValue) Duration = value; } }

        protected Vector3[] _initialPositions = new Vector3[16];
        protected Coroutine _coroutine;

        /// <summary>
        /// 捕获当前所有点位到NewPositions
        /// </summary>
        public virtual void CaptureCurrentPositions()
        {
            if (TargetComponent == null)
            {
                Debug.LogError("TargetComponent is null! Cannot capture positions.");
                return;
            }

            Vector3[] captured;
            if (ValidateStructure(TargetComponent, out captured))
            {
                NewPositions = captured;
                Debug.Log(string.Format("Captured 16 positions from {0}", TargetComponent.name));
            }
        }

        /// <summary>
        /// 验证结构并存储位置到指定数组
        /// </summary>
        protected bool ValidateStructure(CUIGraphicOmniDirectional component, out Vector3[] positions)
        {
            positions = new Vector3[16];

            if (component == null || component.RefCurves == null || component.RefCurves.Length != 4)
            {
                Debug.LogError(string.Format("{0} must have exactly 4 RefCurves!",
                    component != null ? component.name : "Null Component"));
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                if (component.RefCurves[i] == null || component.RefCurves[i].ControlPoints == null ||
                    component.RefCurves[i].ControlPoints.Length != 4)
                {
                    Debug.LogError(string.Format("{0}.RefCurves[{1}] must have exactly 4 ControlPoints!",
                        component.name, i));
                    return false;
                }

                for (int j = 0; j < 4; j++)
                {
                    positions[i * 4 + j] = component.RefCurves[i].ControlPoints[j];
                }
            }
            return true;
        }

        protected override void CustomInitialization(MMF_Player owner)
        {
            base.CustomInitialization(owner);
            if (TargetComponent == null) return;

            // 自动读取初始位置
            ValidateStructure(TargetComponent, out _initialPositions);
            _coroutine = null;

            // 如果NewPositions全为0，自动捕获作为默认值
            bool allZero = true;
            for (int i = 0; i < NewPositions.Length; i++)
            {
                if (NewPositions[i] != Vector3.zero) { allZero = false; break; }
            }
            if (allZero)
            {
                Debug.Log(string.Format("Auto-capturing current positions for {0}", TargetComponent.name));
                CaptureCurrentPositions();
            }
        }

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!Active || !FeedbackTypeAuthorized || TargetComponent == null) return;

            if (InterpolateValue)
            {
                if (_coroutine != null) Owner.StopCoroutine(_coroutine);
                _coroutine = Owner.StartCoroutine(InterpolationSequence());
            }
            else
            {
                ApplyPositions(NewPositions);
            }
        }

        protected virtual IEnumerator InterpolationSequence()
        {
            IsPlaying = true;
            float journey = NormalPlayDirection ? 0f : FeedbackDuration;

            while (journey >= 0 && journey <= FeedbackDuration && FeedbackDuration > 0)
            {
                float t = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);
                float evaluated = MMTween.Tween(t, 0f, 1f, 0, ValueMultiple, InterpolationCurve);

                for (int i = 0; i < 16; i++)
                {
                    Vector3 newPos = Vector3.LerpUnclamped(_initialPositions[i], NewPositions[i], evaluated);
                    SetControlPoint(i, newPos);
                }

                journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
                yield return null;
            }

            _coroutine = null;
            IsPlaying = false;
        }

        protected virtual void ApplyPositions(Vector3[] positions)
        {
            for (int i = 0; i < 16; i++) SetControlPoint(i, positions[i]);
        }

        protected virtual void SetControlPoint(int index, Vector3 position)
        {
            int curve = index / 4;
            int point = index % 4;
            // Use CUIGraphicOmniDirectional API to set control points instead of modifying arrays directly
            if (TargetComponent != null)
            {
                TargetComponent.SetControlPoint(curve, point, position);
            }

            if (ExtraTargetComponents != null)
            {
                foreach (var extra in ExtraTargetComponents)
                {
                    if (extra.TargetComponent != null)
                    {
                        extra.TargetComponent.SetControlPoint(curve, point, position);
                    }
                }
            }
        }

        protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
        {
            if (!Active || !FeedbackTypeAuthorized) return;

            // 当StopIsCapture为true时，捕获当前位置
            if (StopIsCapture && TargetComponent != null)
            {
                Debug.Log(string.Format("StopIsCapture is true: Capturing current positions from {0}", TargetComponent.name));
                CaptureCurrentPositions();
            }

            // 停止协程
            if (_coroutine != null)
            {
                base.CustomStopFeedback(position, feedbacksIntensity);
                Owner.StopCoroutine(_coroutine);
                _coroutine = null;
                IsPlaying = false;
            }
        }

        protected override void CustomRestoreInitialValues()
        {
            if (!Active || !FeedbackTypeAuthorized || TargetComponent == null) return;
            ApplyPositions(_initialPositions);
        }

        public override void OnValidate()
        {
            base.OnValidate();
            if (NewPositions == null || NewPositions.Length != 16)
            {
                Vector3[] old = NewPositions;
                NewPositions = new Vector3[16];
                if (old != null)
                {
                    for (int i = 0; i < Mathf.Min(old.Length, 16); i++)
                        NewPositions[i] = old[i];
                }
            }
        }
    }
}