using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace MoreMountains.Feedbacks
{
    [AddComponentMenu("")]
    [FeedbackHelp("This feedback will let you set a property on the target image's material")]
    [FeedbackPath("Hedium/Image/Material")]
    
    public class MMF_ImageMaterial :  MMF_Feedback
    {
        /// a static bool used to disable all feedbacks of this type at once
        public static bool FeedbackTypeAuthorized = true;

#if UNITY_EDITOR
        public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
        public override bool EvaluateRequiresSetup() { return (TargetImage == null); }
        public override string RequiredTargetText { get { return TargetImage != null ? TargetImage.name : ""; } }
        public override string RequiresSetupText { get { return "This feedback requires that a TargetImage be set to be able to work properly. You can set one below."; } }
#endif

        public override bool HasRandomness => true;
        public override bool HasCustomInspectors => true;
        public override bool HasAutomatedTargetAcquisition => true;
        protected override void AutomateTargetAcquisition() => TargetImage = FindAutomatedTarget<Image>();

        public enum PropertyTypes { Color, Float, Integer, Texture, TextureOffset, TextureScale, Vector }

        [Serializable]
        public class ExtraImageData
        {
            public Image TargetImage;
        }

        [MMFInspectorGroup("Material", true, 12, true)]
        /// the Image to change the material on
        [Tooltip("the Image to change the material on")]
        public Image TargetImage;
        /// a list of extra Images to change the material on
        [Tooltip("a list of extra Images to change the material on")]
        public List<ExtraImageData> ExtraTargetImages;
        /// the ID of the property to set, as exposed in the shader
        [Tooltip("the ID of the property to set, as exposed in the shader")]
        public string PropertyID;
        /// the type of the property to set
        [Tooltip("the type of the property to set")]
        public PropertyTypes PropertyType = PropertyTypes.Float;

        /// if the property is a color, the new color to set
        [Tooltip("if the property is a color, the new color to set")]
        [MMFEnumCondition("PropertyType", (int)PropertyTypes.Color)]
        [ColorUsage(true, true)]
        public Color NewColor = Color.red;
        /// if the property is a float, the new float to set
        [Tooltip("if the property is a float, the new float to set")]
        [MMFEnumCondition("PropertyType", (int)PropertyTypes.Float)]
        public float NewFloat = 1f;
        /// if the property is an int, the new int to set
        [Tooltip("if the property is an int, the new int to set")]
        [MMFEnumCondition("PropertyType", (int)PropertyTypes.Integer)]
        public int NewInt;
        /// if the property is a texture, the new texture to set
        [Tooltip("if the property is a texture, the new texture to set")]
        [MMFEnumCondition("PropertyType", (int)PropertyTypes.Texture)]
        public Texture NewTexture;
        /// if the property is a texture offset, the new offset to set
        [Tooltip("if the property is a texture offset, the new offset to set")]
        [MMFEnumCondition("PropertyType", (int)PropertyTypes.TextureOffset)]
        public Vector2 NewOffset;
        /// if the property is a texture scale, the new scale to set
        [Tooltip("if the property is a texture scale, the new scale to set")]
        [MMFEnumCondition("PropertyType", (int)PropertyTypes.TextureScale)]
        public Vector2 NewScale;
        /// if the property is a vector, the new vector4 to set
        [Tooltip("if the property is a vector4, the new vector4 to set")]
        [MMFEnumCondition("PropertyType", (int)PropertyTypes.Vector)]
        public Vector4 NewVector;

        [Header("Interpolation")]
        /// whether or not to interpolate the value over time. If set to false, the change will be instant
        [Tooltip("whether or not to interpolate the value over time. If set to false, the change will be instant")]
        public bool InterpolateValue = false;
        /// the duration of the interpolation
        [Tooltip("the duration of the interpolation")]
        [MMFCondition("InterpolateValue", true)]
        public float Duration = 2f;
        /// the curve over which to interpolate the value
        [Tooltip("the curve over which to interpolate the value")]
        public MMTweenType InterpolationCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)), "InterpolateValue");

        public override float FeedbackDuration { get { return (InterpolateValue) ? ApplyTimeMultiplier(Duration) : 0f; } set { if (InterpolateValue) { Duration = value; } } }

        protected int _propertyID;
        protected Color _initialColor;
        protected float _initialFloat;
        protected int _initialInt;
        protected Texture _initialTexture;
        protected Vector2 _initialOffset;
        protected Vector2 _initialScale;
        protected Vector4 _initialVector;
        protected Coroutine _coroutine;
        protected Color _newColor;
        protected Vector2 _newVector2;
        protected Vector4 _newVector4;

        /// <summary>
        /// On init we store the initial material property values
        /// </summary>
        /// <param name="owner"></param>
        protected override void CustomInitialization(MMF_Player owner)
        {
            base.CustomInitialization(owner);

            if (string.IsNullOrEmpty(PropertyID))
            {
                Debug.LogWarning("PropertyID is empty. Please set a valid shader property name.");
                return;
            }

            _propertyID = Shader.PropertyToID(PropertyID);

            // we store the initial value of the property based on its type
            if (Active && TargetImage != null)
            {
                Material material = TargetImage.materialForRendering ?? TargetImage.material;

                switch (PropertyType)
                {
                    case PropertyTypes.Color:
                        _initialColor = material.GetColor(_propertyID);
                        break;
                    case PropertyTypes.Float:
                        _initialFloat = material.GetFloat(_propertyID);
                        break;
                    case PropertyTypes.Integer:
                        _initialInt = material.GetInt(_propertyID);
                        break;
                    case PropertyTypes.Texture:
                        _initialTexture = material.GetTexture(_propertyID);
                        break;
                    case PropertyTypes.TextureOffset:
                        _initialOffset = material.GetTextureOffset(_propertyID);
                        break;
                    case PropertyTypes.TextureScale:
                        _initialScale = material.GetTextureScale(_propertyID);
                        break;
                    case PropertyTypes.Vector:
                        _initialVector = material.GetVector(_propertyID);
                        break;
                }
            }
        }

        /// <summary>
        /// On play we set the material property
        /// </summary>
        /// <param name="position"></param>
        /// <param name="feedbacksIntensity"></param>
        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (!Active || !FeedbackTypeAuthorized || TargetImage == null)
            {
                return;
            }

            if (InterpolateValue)
            {
                if (_coroutine != null)
                {
                    Owner.StopCoroutine(_coroutine);
                }
                _coroutine = Owner.StartCoroutine(InterpolationSequence(feedbacksIntensity));
            }
            else
            {
                switch (PropertyType)
                {
                    case PropertyTypes.Color:
                        SetColor(NewColor);
                        break;
                    case PropertyTypes.Float:
                        SetFloat(NewFloat);
                        break;
                    case PropertyTypes.Integer:
                        SetInt(NewInt);
                        break;
                    case PropertyTypes.Texture:
                        SetTexture(NewTexture);
                        break;
                    case PropertyTypes.TextureOffset:
                        SetTextureOffset(NewOffset);
                        break;
                    case PropertyTypes.TextureScale:
                        SetTextureScale(NewScale);
                        break;
                    case PropertyTypes.Vector:
                        SetVector(NewVector);
                        break;
                }
            }
        }

        /// <summary>
        /// An internal coroutine used to interpolate the value over time
        /// </summary>
        /// <param name="intensityMultiplier"></param>
        /// <returns></returns>
        protected virtual IEnumerator InterpolationSequence(float intensityMultiplier)
        {
            IsPlaying = true;
            float journey = NormalPlayDirection ? 0f : FeedbackDuration;

            while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
            {
                float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);
                SetValueAtTime(remappedTime, intensityMultiplier);

                journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
                yield return null;
            }

            SetValueAtTime(FinalNormalizedTime, intensityMultiplier);
            _coroutine = null;
            IsPlaying = false;
            yield return null;
        }

        /// <summary>
        /// Sets the value of the property at a certain time
        /// </summary>
        /// <param name="t"></param>
        /// <param name="intensityMultiplier"></param>
        protected virtual void SetValueAtTime(float t, float intensityMultiplier)
        {
            switch (PropertyType)
            {
                case PropertyTypes.Color:
                    float evaluated = MMTween.Tween(t, 0f, 1f, 0f, 1f, InterpolationCurve);
                    _newColor = Color.Lerp(_initialColor, NewColor, evaluated);
                    SetColor(_newColor);
                    break;
                case PropertyTypes.Float:
                    float newFloatValue = MMTween.Tween(t, 0f, 1f, _initialFloat, NewFloat, InterpolationCurve);
                    SetFloat(newFloatValue);
                    break;
                case PropertyTypes.Integer:
                    int newIntValue = (int)MMTween.Tween(t, 0f, 1f, _initialInt, NewInt, InterpolationCurve);
                    SetInt(newIntValue);
                    break;
                case PropertyTypes.Texture:
                    SetTexture(NewTexture);
                    break;
                case PropertyTypes.TextureOffset:
                    _newVector2 = MMTween.Tween(t, 0f, 1f, _initialOffset, NewOffset, InterpolationCurve);
                    SetTextureOffset(_newVector2);
                    break;
                case PropertyTypes.TextureScale:
                    _newVector2 = MMTween.Tween(t, 0f, 1f, _initialScale, NewScale, InterpolationCurve);
                    SetTextureScale(_newVector2);
                    break;
                case PropertyTypes.Vector:
                    _newVector4 = MMTween.Tween(t, 0f, 1f, _initialVector, NewVector, InterpolationCurve);
                    SetVector(_newVector4);
                    break;
            }
        }

        /// <summary>
        /// Stops this feedback
        /// </summary>
        /// <param name="position"></param>
        /// <param name="feedbacksIntensity"></param>
        protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
        {
            if (!Active || !FeedbackTypeAuthorized || (_coroutine == null))
            {
                return;
            }

            base.CustomStopFeedback(position, feedbacksIntensity);
            IsPlaying = false;
            Owner.StopCoroutine(_coroutine);
            _coroutine = null;
        }

        /// <summary>
        /// On restore, we restore our initial state
        /// </summary>
        protected override void CustomRestoreInitialValues()
        {
            if (!Active || !FeedbackTypeAuthorized || TargetImage == null)
            {
                return;
            }

            // we restore initial values based on the property type
            switch (PropertyType)
            {
                case PropertyTypes.Color:
                    SetColor(_initialColor);
                    break;
                case PropertyTypes.Float:
                    SetFloat(_initialFloat);
                    break;
                case PropertyTypes.Integer:
                    SetInt(_initialInt);
                    break;
                case PropertyTypes.Texture:
                    SetTexture(_initialTexture);
                    break;
                case PropertyTypes.TextureOffset:
                    SetTextureOffset(_initialOffset);
                    break;
                case PropertyTypes.TextureScale:
                    SetTextureScale(_initialScale);
                    break;
                case PropertyTypes.Vector:
                    SetVector(_initialVector);
                    break;
            }
        }

        protected virtual void SetColor(Color newColor)
        {
            if (TargetImage != null)
            {
                TargetImage.material.SetColor(_propertyID, newColor);
            }
            

            foreach (ExtraImageData data in ExtraTargetImages)
            {
                if (data.TargetImage != null)
                {
                    data.TargetImage.material.SetColor(_propertyID, newColor);
                }
            }
        }

        protected virtual void SetFloat(float newFloat)
        {
            if (TargetImage != null)
            {
                TargetImage.material.SetFloat(_propertyID, newFloat);
            }

            foreach (ExtraImageData data in ExtraTargetImages)
            {
                if (data.TargetImage != null)
                {
                    data.TargetImage.material.SetFloat(_propertyID, newFloat);
                }
            }
        }

        protected virtual void SetInt(int newInt)
        {
            if (TargetImage != null)
            {
                TargetImage.material.SetInt(_propertyID, newInt);
            }

            foreach (ExtraImageData data in ExtraTargetImages)
            {
                if (data.TargetImage != null)
                {
                    data.TargetImage.material.SetInt(_propertyID, newInt);
                }
            }
        }

        protected virtual void SetTexture(Texture newTexture)
        {
            if (TargetImage != null)
            {
                TargetImage.material.SetTexture(_propertyID, newTexture);
            }

            foreach (ExtraImageData data in ExtraTargetImages)
            {
                if (data.TargetImage != null)
                {
                    data.TargetImage.material.SetTexture(_propertyID, newTexture);
                }
            }
        }

        protected virtual void SetTextureOffset(Vector2 newOffset)
        {
            if (TargetImage != null)
            {
                TargetImage.material.SetTextureOffset(_propertyID, newOffset);
            }

            foreach (ExtraImageData data in ExtraTargetImages)
            {
                if (data.TargetImage != null)
                {
                    data.TargetImage.material.SetTextureOffset(_propertyID, newOffset);
                }
            }
        }

        protected virtual void SetTextureScale(Vector2 newScale)
        {
            if (TargetImage != null)
            {
                TargetImage.material.SetTextureScale(_propertyID, newScale);
            }

            foreach (ExtraImageData data in ExtraTargetImages)
            {
                if (data.TargetImage != null)
                {
                    data.TargetImage.material.SetTextureScale(_propertyID, newScale);
                }
            }
        }

        protected virtual void SetVector(Vector4 newVector)
        {
            if (TargetImage != null)
            {
                TargetImage.material.SetVector(_propertyID, newVector);
            }

            foreach (ExtraImageData data in ExtraTargetImages)
            {
                if (data.TargetImage != null)
                {
                    data.TargetImage.material.SetVector(_propertyID, newVector);
                }
            }
        }

        /// <summary>
        /// On Validate, we migrate our deprecated animation curves to our tween types if needed
        /// </summary>
        public override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(InterpolationCurve.ConditionPropertyName))
            {
                InterpolationCurve.ConditionPropertyName = "InterpolateValue";
            }
        }
    }
}