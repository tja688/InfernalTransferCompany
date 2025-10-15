using System.Collections.Generic;
using UnityEngine;

namespace DirectorUI
{
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class UIView : MonoBehaviour
    {
        [Tooltip("此視圖的唯一標識符")]
        [SerializeField] private string viewId = string.Empty;

        public string ViewId => viewId;
        public CanvasGroup CanvasGroup { get; private set; }

        private readonly Dictionary<string, UITransitionElement> elementRegistry = new();

        private void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            RebuildElementRegistry();
            UIDirector.RegisterViewWhenReady(this);
        }

        private void OnDestroy()
        {
            if (UIDirector.HasInstance)
            {
                UIDirector.Instance.UnregisterView(this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            RebuildElementRegistry();
        }
#endif

        public void RebuildElementRegistry()
        {
            elementRegistry.Clear();
            var elements = GetComponentsInChildren<UITransitionElement>(true);
            foreach (var element in elements)
            {
                if (element == null) continue;
                var id = element.ElementId;
                if (string.IsNullOrEmpty(id)) continue;
                if (!elementRegistry.ContainsKey(id))
                {
                    elementRegistry.Add(id, element);
                }
                else
                {
                    Debug.LogWarning($"[UIView] Duplicate elementId '{id}' detected under view '{viewId}'.", element);
                }
            }
        }

        public UITransitionElement GetElement(string elementId)
        {
            if (string.IsNullOrEmpty(elementId)) return null;
            elementRegistry.TryGetValue(elementId, out var element);
            return element;
        }

        public void PrepareForIntro()
        {
            gameObject.SetActive(true);
            CanvasGroup.alpha = 0f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}
