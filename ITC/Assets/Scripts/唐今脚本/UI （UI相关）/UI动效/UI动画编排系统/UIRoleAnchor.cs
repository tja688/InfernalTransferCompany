using UnityEngine;

namespace ITC.UI.Choreography
{
    /// <summary>
    /// Lightweight marker component that marks a RectTransform as an anchor for a specific role/state binding.
    /// Anchors can live anywhere in the hierarchy (hidden or visible) and are used to provide positional targets.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UIRoleAnchor : MonoBehaviour
    {
        [SerializeField]
        private string _anchorId;

        [SerializeField]
        private bool _autoRegister = true;

        private RectTransform _rectTransform;

        public string AnchorId => _anchorId;

        private void OnEnable()
        {
            EnsureRectTransform();
            if (_autoRegister)
            {
                UIAnchorRegistry.Register(this);
            }
        }

        private void OnDisable()
        {
            if (_autoRegister)
            {
                UIAnchorRegistry.Unregister(this);
            }
        }

        private void Reset()
        {
            EnsureRectTransform();
            if (string.IsNullOrEmpty(_anchorId) && _rectTransform != null)
            {
                _anchorId = gameObject.name;
            }
        }

        private void EnsureRectTransform()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        }

        public RectTransform GetRectTransform()
        {
            EnsureRectTransform();
            return _rectTransform;
        }
    }
}
