using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Serializable snapshot of a RectTransform's geometric state. Used to describe anchor targets for actors.
    /// </summary>
    [System.Serializable]
    public struct RectTransformSnapshot
    {
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
        public Vector3 eulerAngles;
        public Vector2 pivot;
        public Vector2 anchorMin;
        public Vector2 anchorMax;

        public static RectTransformSnapshot FromRectTransform(RectTransform rect)
        {
            return new RectTransformSnapshot
            {
                anchoredPosition = rect.anchoredPosition,
                sizeDelta = rect.sizeDelta,
                localScale = rect.localScale,
                eulerAngles = rect.localEulerAngles,
                pivot = rect.pivot,
                anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax
            };
        }

        public void ApplyTo(RectTransform rect)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.localEulerAngles = eulerAngles;
            rect.localScale = localScale;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }
    }
}
