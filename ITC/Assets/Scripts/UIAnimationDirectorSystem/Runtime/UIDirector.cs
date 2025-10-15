using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DirectorUI
{
    /// <summary>
    /// 全局導演，負責視圖註冊與過渡調度。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UITweenPlayer))]
    public class UIDirector : MonoBehaviour
    {
        public static UIDirector Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        private static readonly List<UIView> PendingViews = new();

        [Tooltip("項目中所有的過渡票據 ScriptableObjects")]
        [SerializeField] private List<UITransitionTicket> transitionTickets = new();

        [Tooltip("指定初始顯示的視圖ID")]
        [SerializeField] private string startingViewId = string.Empty;

        [Tooltip("用於播放動畫預設的播放器")]
        [SerializeField] private UITweenPlayer tweenPlayer;

        private readonly Dictionary<string, UITransitionTicket> ticketLibrary = new();
        private readonly Dictionary<string, UIView> viewRegistry = new();
        private readonly Stack<UIView> viewStack = new();

        public UIView CurrentView { get; private set; }
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[UIDirector] Duplicate instance detected, destroying the newest one.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            if (tweenPlayer == null)
            {
                tweenPlayer = GetComponent<UITweenPlayer>();
            }

            BuildTicketLibrary();

            foreach (var view in PendingViews)
            {
                RegisterView(view);
            }
            PendingViews.Clear();
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(startingViewId) && viewRegistry.TryGetValue(startingViewId, out var startView))
            {
                ForceShowView(startView);
            }
            else if (viewStack.Count == 0 && viewRegistry.Count > 0)
            {
                foreach (var view in viewRegistry.Values)
                {
                    ForceHideView(view);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (tweenPlayer == null)
            {
                tweenPlayer = GetComponent<UITweenPlayer>();
            }

            if (!Application.isPlaying)
            {
                BuildTicketLibrary();
            }
        }
#endif

        public static void RegisterViewWhenReady(UIView view)
        {
            if (view == null) return;
            if (HasInstance)
            {
                Instance.RegisterView(view);
            }
            else if (!PendingViews.Contains(view))
            {
                PendingViews.Add(view);
            }
        }

        public static string BuildTicketKey(string fromId, string toId)
        {
            return $"{fromId ?? string.Empty}_{toId ?? string.Empty}";
        }

        public void RegisterView(UIView view)
        {
            if (view == null) return;
            if (string.IsNullOrEmpty(view.ViewId))
            {
                Debug.LogWarning("[UIDirector] Attempted to register a view without viewId.", view);
                return;
            }

            if (viewRegistry.TryGetValue(view.ViewId, out var existing) && existing != view)
            {
                Debug.LogWarning($"[UIDirector] Duplicate viewId detected: {view.ViewId}", view);
                viewRegistry[view.ViewId] = view;
            }
            else
            {
                viewRegistry[view.ViewId] = view;
            }
        }

        public void UnregisterView(UIView view)
        {
            if (view == null) return;
            if (viewRegistry.TryGetValue(view.ViewId, out var existing) && existing == view)
            {
                viewRegistry.Remove(view.ViewId);
            }

            var remaining = new Stack<UIView>(viewStack.Count);
            foreach (var item in viewStack)
            {
                if (item != view)
                {
                    remaining.Push(item);
                }
            }
            viewStack.Clear();
            foreach (var item in remaining)
            {
                viewStack.Push(item);
            }

            if (CurrentView == view)
            {
                CurrentView = null;
            }
        }

        private void BuildTicketLibrary()
        {
            ticketLibrary.Clear();
            foreach (var ticket in transitionTickets)
            {
                if (ticket == null) continue;
                if (string.IsNullOrEmpty(ticket.fromViewId) || string.IsNullOrEmpty(ticket.toViewId))
                {
                    Debug.LogWarning($"[UIDirector] Ticket '{ticket.name}' has empty view id.");
                    continue;
                }

                var key = ticket.TicketKey;
                if (ticketLibrary.ContainsKey(key))
                {
                    Debug.LogWarning($"[UIDirector] Duplicate transition ticket detected for {key}.");
                    continue;
                }

                ticketLibrary.Add(key, ticket);
            }
        }

        public void ExecuteTransition(string toViewId)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("[UIDirector] Transition is already running.");
                return;
            }

            if (string.IsNullOrEmpty(toViewId))
            {
                Debug.LogWarning("[UIDirector] ExecuteTransition called with empty toViewId.");
                return;
            }

            if (!viewRegistry.TryGetValue(toViewId, out var targetView))
            {
                Debug.LogWarning($"[UIDirector] Target view '{toViewId}' not registered.");
                return;
            }

            if (CurrentView != null && CurrentView == targetView)
            {
                return;
            }

            UITransitionTicket ticket = null;
            if (CurrentView != null)
            {
                var key = BuildTicketKey(CurrentView.ViewId, toViewId);
                ticketLibrary.TryGetValue(key, out ticket);
            }

            StartCoroutine(PerformTransition(CurrentView, targetView, ticket, true));
        }

        public void GoBack()
        {
            if (IsTransitioning)
            {
                return;
            }

            if (viewStack.Count <= 1)
            {
                Debug.LogWarning("[UIDirector] No previous view in stack.");
                return;
            }

            var current = viewStack.Pop();
            var previous = viewStack.Peek();

            UITransitionTicket ticket = null;
            var key = BuildTicketKey(current.ViewId, previous.ViewId);
            ticketLibrary.TryGetValue(key, out ticket);

            StartCoroutine(PerformTransition(current, previous, ticket, false));
        }

        private IEnumerator PerformTransition(UIView fromView, UIView toView, UITransitionTicket ticket, bool pushToStack)
        {
            IsTransitioning = true;

            if (toView != null)
            {
                toView.PrepareForIntro();
            }

            var window = new TransitionWindow();

            if (ticket != null)
            {
                window.Extend(PlayStuntDoubleTransitions(fromView, toView, ticket));
                window.Extend(PlayAnimationCollection(fromView, ticket.outroAnimations));
                window.Extend(PlayAnimationCollection(toView, ticket.introAnimations));
            }

            if (window.HasDuration)
            {
                yield return WaitForWindow(window);
            }

            if (fromView != null)
            {
                fromView.Hide();
            }

            if (toView != null)
            {
                toView.Show();
            }

            CurrentView = toView;

            if (pushToStack && toView != null)
            {
                if (viewStack.Count == 0 || viewStack.Peek() != toView)
                {
                    viewStack.Push(toView);
                }
            }

            IsTransitioning = false;
        }

        private TransitionWindow PlayAnimationCollection(UIView view, List<UITransitionTicket.AnimationStep> steps)
        {
            var window = new TransitionWindow();
            if (view == null || steps == null || steps.Count == 0) return window;

            foreach (var step in steps)
            {
                if (step == null || step.animationPreset == null) continue;
                var element = view.GetElement(step.elementId);
                if (element == null)
                {
                    Debug.LogWarning($"[UIDirector] Element '{step.elementId}' not found in view '{view.ViewId}'.");
                    continue;
                }

                StartCoroutine(PlayPresetWithDelay(element.gameObject, step.animationPreset, step.delay));

                var duration = Mathf.Max(0f, step.delay) + EstimatePresetDuration(step.animationPreset);
                if (step.animationPreset.unscaledTime)
                {
                    window.unscaled = Mathf.Max(window.unscaled, duration);
                }
                else
                {
                    window.scaled = Mathf.Max(window.scaled, duration);
                }
            }

            return window;
        }

        private TransitionWindow PlayStuntDoubleTransitions(UIView fromView, UIView toView, UITransitionTicket ticket)
        {
            var window = new TransitionWindow();
            if (fromView == null || toView == null || ticket.stuntDoubleTransitions == null || ticket.stuntDoubleTransitions.Count == 0)
            {
                return window;
            }

            foreach (var step in ticket.stuntDoubleTransitions)
            {
                if (step == null || step.flyingAnimationPreset == null) continue;
                var original = fromView.GetElement(step.elementId);
                var stunt = toView.GetElement(step.elementId);
                if (original == null || stunt == null)
                {
                    Debug.LogWarning($"[UIDirector] Stunt step '{step.elementId}' missing element in views.");
                    continue;
                }

                SetElementAlpha(stunt.gameObject, 0f);

                StartCoroutine(PlayPresetWithDelay(original.gameObject, step.flyingAnimationPreset, 0f, () =>
                {
                    SetElementAlpha(original.gameObject, 0f);
                    SetElementAlpha(stunt.gameObject, 1f);
                }));

                var duration = EstimatePresetDuration(step.flyingAnimationPreset);
                if (step.flyingAnimationPreset.unscaledTime)
                {
                    window.unscaled = Mathf.Max(window.unscaled, duration);
                }
                else
                {
                    window.scaled = Mathf.Max(window.scaled, duration);
                }
            }

            return window;
        }

        private IEnumerator PlayPresetWithDelay(GameObject target, UITweenPreset preset, float delay, System.Action onComplete = null)
        {
            if (preset == null || target == null)
            {
                yield break;
            }

            if (delay > 0f)
            {
                if (preset.unscaledTime)
                {
                    yield return new WaitForSecondsRealtime(delay);
                }
                else
                {
                    yield return new WaitForSeconds(delay);
                }
            }

            if (tweenPlayer == null)
            {
                Debug.LogWarning("[UIDirector] Tween player not configured.");
                onComplete?.Invoke();
                yield break;
            }

            tweenPlayer.Play(target, preset, onComplete);
        }

        private void ForceShowView(UIView view)
        {
            foreach (var v in viewRegistry.Values)
            {
                if (v != view)
                {
                    ForceHideView(v);
                }
            }

            view.Show();
            if (!viewStack.Contains(view))
            {
                viewStack.Push(view);
            }
            CurrentView = view;
        }

        private void ForceHideView(UIView view)
        {
            view.Hide();
        }

        private void SetElementAlpha(GameObject target, float alpha)
        {
            if (target == null) return;
            if (target.TryGetComponent<CanvasGroup>(out var cg))
            {
                cg.alpha = alpha;
            }
            else
            {
                var graphics = target.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                foreach (var graphic in graphics)
                {
                    var color = graphic.color;
                    color.a = alpha;
                    graphic.color = color;
                }
            }
        }

        private static IEnumerator WaitForWindow(TransitionWindow window)
        {
            if (window.scaled > 0f)
            {
                yield return new WaitForSeconds(window.scaled);
            }

            if (window.unscaled > 0f)
            {
                float remaining = Mathf.Max(0f, window.unscaled - Mathf.Max(window.scaled, 0f));

                if (window.scaled <= 0f)
                {
                    yield return new WaitForSecondsRealtime(window.unscaled);
                }
                else if (remaining > 0f)
                {
                    yield return new WaitForSecondsRealtime(remaining);
                }
            }
        }

        private static float EstimatePresetDuration(UITweenPreset preset)
        {
            if (preset == null) return 0f;
            var duration = Mathf.Max(0f, preset.duration);
            var loops = Mathf.Max(0, preset.loops);
            var total = duration * (loops + 1);
            return Mathf.Max(0f, preset.delay) + total;
        }

        private struct TransitionWindow
        {
            public float scaled;
            public float unscaled;

            public void Extend(TransitionWindow other)
            {
                scaled = Mathf.Max(scaled, other.scaled);
                unscaled = Mathf.Max(unscaled, other.unscaled);
            }

            public bool HasDuration => scaled > 0f || unscaled > 0f;
        }
    }
}
