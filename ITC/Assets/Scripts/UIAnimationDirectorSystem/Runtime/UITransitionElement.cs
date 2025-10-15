using UnityEngine;

namespace DirectorUI
{
    /// <summary>
    /// Identifies a transitionable UI element inside a <see cref="UIView"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class UITransitionElement : MonoBehaviour
    {
        [Tooltip("此元素在其所屬 UIView 內的唯一標識符")]
        [SerializeField] private string elementId = string.Empty;

        /// <summary>
        /// Unique identifier for this element inside its parent view.
        /// </summary>
        public string ElementId => elementId;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(elementId))
            {
                elementId = gameObject.name;
            }
        }
#endif
    }
}
