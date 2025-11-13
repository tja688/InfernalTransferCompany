/// Credit - Custom animator for CUIGraphicOmniDirectional
/// Provides preset animation controls for common deformation scenarios

using System.Collections;
using UnityEngine;

namespace UnityEngine.UI.Extensions
{
    /// <summary>
    /// 为CUIGraphicOmniDirectional提供常见的预设动画控制
    /// 包括挤压、拉伸、弹跳等动画效果
    /// </summary>
    [RequireComponent(typeof(CUIGraphicOmniDirectional))]
    [AddComponentMenu("UI/Effects/Extensions/CUI Graphic Omni Animator")]
    public class CUIGraphicOmniAnimator : MonoBehaviour
    {
        #region Inspector Settings

        [Header("组件引用")]
        [Tooltip("自动获取或手动指定CUIGraphicOmniDirectional组件")]
        public CUIGraphicOmniDirectional cuiGraphic;

        [Header("动画设置")]
        [Tooltip("默认动画持续时间（秒）")]
        [Range(0.01f, 2f)]
        public float defaultDuration = 0.3f;

        [Tooltip("使用的缓动曲线")]
        public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("形变强度")]
        [Tooltip("水平形变强度（0-1）")]
        [Range(0f, 1f)]
        public float horizontalStrength = 0.2f;

        [Tooltip("垂直形变强度（0-1）")]
        [Range(0f, 1f)]
        public float verticalStrength = 0.2f;

        #endregion

        #region Private Fields

        private Vector3[,] originalControlPoints;
        private Coroutine currentAnimation;
        private bool isAnimating = false;

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            if (cuiGraphic == null)
                cuiGraphic = GetComponent<CUIGraphicOmniDirectional>();

            SaveOriginalPoints();
        }

        void OnEnable()
        {
            SaveOriginalPoints();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 保存当前控制点为原始状态
        /// </summary>
        public void SaveOriginalPoints()
        {
            if (cuiGraphic == null || cuiGraphic.RefCurves == null)
                return;

            originalControlPoints = new Vector3[4, 4];
            for (int c = 0; c < 4; c++)
            {
                for (int p = 0; p < 4; p++)
                {
                    originalControlPoints[c, p] = cuiGraphic.GetControlPoint(c, p);
                }
            }
        }

        /// <summary>
        /// 恢复到原始控制点
        /// </summary>
        public void RestoreOriginalPoints()
        {
            if (originalControlPoints == null)
                return;

            for (int c = 0; c < 4; c++)
            {
                for (int p = 0; p < 4; p++)
                {
                    cuiGraphic.SetControlPoint(c, p, originalControlPoints[c, p]);
                }
            }
        }

        /// <summary>
        /// 停止当前动画
        /// </summary>
        public void StopCurrentAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }
            isAnimating = false;
        }

        #endregion

        #region Animation Methods

        /// <summary>
        /// 横向拉伸动画（模拟冲刺）
        /// </summary>
        public void AnimateHorizontalStretch(float duration = -1)
        {
            if (duration < 0) duration = defaultDuration;
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(HorizontalStretchCoroutine(duration));
        }

        /// <summary>
        /// 纵向挤压动画（模拟刹车或着地）
        /// </summary>
        public void AnimateVerticalSquash(float duration = -1)
        {
            if (duration < 0) duration = defaultDuration;
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(VerticalSquashCoroutine(duration));
        }

        /// <summary>
        /// 膨胀动画
        /// </summary>
        public void AnimateBulge(float duration = -1)
        {
            if (duration < 0) duration = defaultDuration;
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(BulgeCoroutine(duration));
        }

        /// <summary>
        /// 收缩动画
        /// </summary>
        public void AnimateShrink(float duration = -1)
        {
            if (duration < 0) duration = defaultDuration;
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(ShrinkCoroutine(duration));
        }

        /// <summary>
        /// 弹跳动画（完整的挤压拉伸循环）
        /// </summary>
        public void AnimateBounce(float duration = -1)
        {
            if (duration < 0) duration = defaultDuration;
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(BounceCoroutine(duration));
        }

        /// <summary>
        /// 波浪动画
        /// </summary>
        public void AnimateWave(float duration = -1)
        {
            if (duration < 0) duration = defaultDuration;
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(WaveCoroutine(duration));
        }

        /// <summary>
        /// 果冻抖动动画
        /// </summary>
        public void AnimateJiggle(float duration = -1)
        {
            if (duration < 0) duration = defaultDuration * 1.5f;
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(JiggleCoroutine(duration));
        }

        /// <summary>
        /// 脉冲动画（循环）
        /// </summary>
        public void StartPulseAnimation(float frequency = 1f)
        {
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(PulseCoroutine(frequency));
        }

        /// <summary>
        /// 恢复到原始状态（带动画）
        /// </summary>
        public void AnimateToOriginal(float duration = -1)
        {
            if (duration < 0) duration = defaultDuration;
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(ToOriginalCoroutine(duration));
        }

        #endregion

        #region Coroutines

        private IEnumerator HorizontalStretchCoroutine(float duration)
        {
            isAnimating = true;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);

                ApplyHorizontalDeformation(t * horizontalStrength, -t * verticalStrength * 0.5f);

                yield return null;
            }

            isAnimating = false;
            currentAnimation = null;
        }

        private IEnumerator VerticalSquashCoroutine(float duration)
        {
            isAnimating = true;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);

                ApplyHorizontalDeformation(-t * horizontalStrength * 0.5f, t * verticalStrength);

                yield return null;
            }

            isAnimating = false;
            currentAnimation = null;
        }

        private IEnumerator BulgeCoroutine(float duration)
        {
            isAnimating = true;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);

                ApplyBulgeDeformation(t * horizontalStrength, t * verticalStrength);

                yield return null;
            }

            isAnimating = false;
            currentAnimation = null;
        }

        private IEnumerator ShrinkCoroutine(float duration)
        {
            isAnimating = true;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);

                ApplyBulgeDeformation(-t * horizontalStrength, -t * verticalStrength);

                yield return null;
            }

            isAnimating = false;
            currentAnimation = null;
        }

        private IEnumerator BounceCoroutine(float duration)
        {
            isAnimating = true;
            float halfDuration = duration * 0.5f;

            // 第一阶段：纵向挤压
            float elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = easeCurve.Evaluate(elapsed / halfDuration);

                ApplyHorizontalDeformation(t * horizontalStrength * 0.3f, t * verticalStrength);

                yield return null;
            }

            // 第二阶段：纵向拉伸
            elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - easeCurve.Evaluate(elapsed / halfDuration);

                ApplyHorizontalDeformation(-t * horizontalStrength * 0.2f, -t * verticalStrength);

                yield return null;
            }

            RestoreOriginalPoints();
            isAnimating = false;
            currentAnimation = null;
        }

        private IEnumerator WaveCoroutine(float duration)
        {
            isAnimating = true;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                ApplyWaveDeformation(t * Mathf.PI * 2);

                yield return null;
            }

            RestoreOriginalPoints();
            isAnimating = false;
            currentAnimation = null;
        }

        private IEnumerator JiggleCoroutine(float duration)
        {
            isAnimating = true;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float damping = 1f - t; // 衰减

                float jiggle = Mathf.Sin(t * Mathf.PI * 10) * damping;

                ApplyHorizontalDeformation(jiggle * horizontalStrength * 0.5f, jiggle * verticalStrength * 0.5f);

                yield return null;
            }

            RestoreOriginalPoints();
            isAnimating = false;
            currentAnimation = null;
        }

        private IEnumerator PulseCoroutine(float frequency)
        {
            isAnimating = true;

            while (isAnimating)
            {
                float pulse = Mathf.Sin(Time.time * frequency * Mathf.PI * 2) * 0.5f + 0.5f;

                ApplyBulgeDeformation(pulse * horizontalStrength * 0.3f, pulse * verticalStrength * 0.3f);

                yield return null;
            }

            RestoreOriginalPoints();
            currentAnimation = null;
        }

        private IEnumerator ToOriginalCoroutine(float duration)
        {
            isAnimating = true;
            float elapsed = 0;

            Vector3[,] startPoints = new Vector3[4, 4];
            for (int c = 0; c < 4; c++)
            {
                for (int p = 0; p < 4; p++)
                {
                    startPoints[c, p] = cuiGraphic.GetControlPoint(c, p);
                }
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);

                for (int c = 0; c < 4; c++)
                {
                    for (int p = 0; p < 4; p++)
                    {
                        Vector3 point = Vector3.Lerp(startPoints[c, p], originalControlPoints[c, p], t);
                        cuiGraphic.SetControlPoint(c, p, point);
                    }
                }

                yield return null;
            }

            RestoreOriginalPoints();
            isAnimating = false;
            currentAnimation = null;
        }

        #endregion

        #region Deformation Helpers

        private void ApplyHorizontalDeformation(float horizontalAmount, float verticalAmount)
        {
            if (originalControlPoints == null)
                return;

            float width = cuiGraphic.RectTrans.rect.width;
            float height = cuiGraphic.RectTrans.rect.height;

            // 左边
            for (int p = 1; p < 3; p++)
            {
                Vector3 point = originalControlPoints[1, p];
                point.x += horizontalAmount * width;
                cuiGraphic.SetControlPoint(1, p, point);
            }

            // 右边
            for (int p = 1; p < 3; p++)
            {
                Vector3 point = originalControlPoints[3, p];
                point.x -= horizontalAmount * width;
                cuiGraphic.SetControlPoint(3, p, point);
            }

            // 顶部
            for (int p = 1; p < 3; p++)
            {
                Vector3 point = originalControlPoints[2, p];
                point.y -= verticalAmount * height;
                cuiGraphic.SetControlPoint(2, p, point);
            }

            // 底部
            for (int p = 1; p < 3; p++)
            {
                Vector3 point = originalControlPoints[0, p];
                point.y += verticalAmount * height;
                cuiGraphic.SetControlPoint(0, p, point);
            }
        }

        private void ApplyBulgeDeformation(float horizontalAmount, float verticalAmount)
        {
            if (originalControlPoints == null)
                return;

            float width = cuiGraphic.RectTrans.rect.width;
            float height = cuiGraphic.RectTrans.rect.height;

            // 底部向下
            for (int p = 1; p < 3; p++)
            {
                Vector3 point = originalControlPoints[0, p];
                point.y -= verticalAmount * height;
                cuiGraphic.SetControlPoint(0, p, point);
            }

            // 顶部向上
            for (int p = 1; p < 3; p++)
            {
                Vector3 point = originalControlPoints[2, p];
                point.y += verticalAmount * height;
                cuiGraphic.SetControlPoint(2, p, point);
            }

            // 左边向左
            for (int p = 1; p < 3; p++)
            {
                Vector3 point = originalControlPoints[1, p];
                point.x -= horizontalAmount * width;
                cuiGraphic.SetControlPoint(1, p, point);
            }

            // 右边向右
            for (int p = 1; p < 3; p++)
            {
                Vector3 point = originalControlPoints[3, p];
                point.x += horizontalAmount * width;
                cuiGraphic.SetControlPoint(3, p, point);
            }
        }

        private void ApplyWaveDeformation(float phase)
        {
            if (originalControlPoints == null)
                return;

            float width = cuiGraphic.RectTrans.rect.width;
            float height = cuiGraphic.RectTrans.rect.height;

            // 顶部波浪
            Vector3 topP1 = originalControlPoints[2, 1];
            topP1.y += Mathf.Sin(phase) * height * verticalStrength;
            cuiGraphic.SetControlPoint(2, 1, topP1);

            Vector3 topP2 = originalControlPoints[2, 2];
            topP2.y += Mathf.Sin(phase + Mathf.PI) * height * verticalStrength;
            cuiGraphic.SetControlPoint(2, 2, topP2);

            // 底部波浪（反相）
            Vector3 bottomP1 = originalControlPoints[0, 1];
            bottomP1.y -= Mathf.Sin(phase) * height * verticalStrength;
            cuiGraphic.SetControlPoint(0, 1, bottomP1);

            Vector3 bottomP2 = originalControlPoints[0, 2];
            bottomP2.y -= Mathf.Sin(phase + Mathf.PI) * height * verticalStrength;
            cuiGraphic.SetControlPoint(0, 2, bottomP2);
        }

        #endregion

        #region Public Properties

        public bool IsAnimating
        {
            get { return isAnimating; }
        }

        #endregion
    }
}

