using System;
using DG.Tweening;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
    /// <summary>
    /// This feedback will let you change the color of a target Renderer over time.
    /// I would be happy to add more DOTween feedbacks or add new functionality by your request. Feel free to address me via github:@pauldyatlov
    /// </summary>
    [AddComponentMenu("")]
    [FeedbackHelp("This feedback will let you change the color of a target Renderer over time.")]
    [FeedbackPath("DOTween/Color")]
    [Serializable]
    public sealed class DOTweenColorMMFeedback : MMF_Feedback
    {
        [MMFInspectorGroup("Renderer", true, 54, true)]
        [Tooltip("The Renderer to affect when playing the feedback")]
        [SerializeField] private Renderer _renderer;

        [Tooltip("For how long the Renderer should change its color over time (per one iteration)")]
        [SerializeField] private float _duration = 0.2f;

        [Tooltip("How many times should that tick go for")]
        [SerializeField] private int _loopsCount = 1;

        [Tooltip("Ff this is true, the target will be disabled when this feedbacks is stopped")]
        [SerializeField] private bool _disableOnStop;

        [Tooltip("The color to move to")]
        [SerializeField] private Color _toColor = Color.red;

        private Tweener _colorTweener;

#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.UIColor;

        public override bool EvaluateRequiresSetup() => _renderer == null;

        public override string RequiredTargetText => _renderer != null ? _renderer.name : string.Empty;
        public override string RequiresSetupText => "This feedback requires a Renderer to be set to be able to work properly. You can set one below.";
#endif

        public override float FeedbackDuration
        {
            get => ApplyTimeMultiplier(_duration);
            set => _duration = value;
        }

        public override bool HasChannel => true;

        /// <summary>
        /// On Play we turn our Renderer on and start an over time coroutine if needed
        /// </summary>
        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (Active == false)
                return;

            Turn(true);
            EndTweener();

            var fromColor = _renderer.material.color;
            _colorTweener = _renderer.material.DOColor(_toColor, _duration)
                .SetLoops(_loopsCount)
                .OnComplete(() => _renderer.material.DOColor(fromColor, _duration));
        }

        protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
        {
            if (Active == false)
                return;

            IsPlaying = false;

            EndTweener();

            if (_disableOnStop)
                Turn(false);
        }

        private void EndTweener()
        {
            _colorTweener?.Kill(true);
        }

        /// <summary>
        /// Turns the Renderer on or off
        /// </summary>
        private void Turn(bool status)
        {
            _renderer.gameObject.SetActive(status);
            _renderer.enabled = status;
        }
    }
}