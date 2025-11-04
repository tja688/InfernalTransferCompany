using UnityEngine;

/// <summary>
/// Forces the attached UI element to remain the last sibling inside its parent canvas hierarchy.
/// </summary>
[DisallowMultipleComponent]
public class CanvasTopLayerEnforcer : MonoBehaviour
{
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
    }

    private void OnEnable()
    {
        if (_rectTransform == null)
        {
            _rectTransform = transform as RectTransform;
        }

        MoveToTop();
    }

    private void LateUpdate()
    {
        MoveToTop();
    }

    private void OnTransformParentChanged()
    {
        MoveToTop();
    }

    private void MoveToTop()
    {
        if (_rectTransform == null)
        {
            return;
        }

        _rectTransform.SetAsLastSibling();
    }
}