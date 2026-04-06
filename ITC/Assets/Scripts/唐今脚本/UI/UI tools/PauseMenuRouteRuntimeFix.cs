using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 为旧版暂停菜单补回 Save/Load 路由与子菜单显隐链路。
/// 当前场景里 Pause 菜单仍在使用 PanelManager，但存储/加载按钮与子菜单关闭按钮的绑定已经缺失。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(5000)]
public sealed class PauseMenuRouteRuntimeFix : MonoBehaviour
{
    private const string PausePanelName = "Pause Panel";
    private const string SavePanelName = "Save Panel";
    private const string LoadPanelName = "Load Panel";
    private static readonly Vector2 SaveContentVisiblePosition = new Vector2(-265f, -16f);
    private static readonly Vector2 LoadContentVisiblePosition = new Vector2(-265f, -16f);

    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject saveCanvas;
    [SerializeField] private GameObject loadCanvas;
    [SerializeField] private GraphicRaycaster pauseRaycaster;
    [SerializeField] private SlotMachinePicker pausePicker;
    [SerializeField] private RectTransform saveContentRoot;
    [SerializeField] private RectTransform loadContentRoot;

    private string _lastAppliedPanel = string.Empty;
    private PanelSlideAnimator _saveAnimator;
    private PanelSlideAnimator _loadAnimator;
    private LegacyPanelAnimator _saveLegacyAnimator;
    private LegacyPanelAnimator _loadLegacyAnimator;
    private Coroutine _saveHideCoroutine;
    private Coroutine _loadHideCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var rootCanvas = GameObject.Find("根Canvas");
        if (rootCanvas == null)
        {
            return;
        }

        var fix = rootCanvas.GetComponent<PauseMenuRouteRuntimeFix>();
        if (fix == null)
        {
            fix = rootCanvas.AddComponent<PauseMenuRouteRuntimeFix>();
        }

        fix.Rebind();
    }

    private void Awake()
    {
        Rebind();
    }

    private void OnEnable()
    {
        ApplyPanelState(true);
    }

    private void Update()
    {
        ApplyPanelState(false);
    }

    public void RefreshNow()
    {
        ApplyPanelState(true);
    }

    public void RefreshAnimatedNow()
    {
        ApplyExplicitAnimatedState();
    }

    public void JumpToPanel(string targetPanel)
    {
        if (PanelManager.Instance == null || string.IsNullOrEmpty(targetPanel))
        {
            return;
        }

        PanelManager.Instance.ChangePanel(targetPanel);

        if (string.Equals(targetPanel, SavePanelName, StringComparison.Ordinal))
        {
            CancelCanvasHide(ref _saveHideCoroutine);
            CancelCanvasHide(ref _loadHideCoroutine);
            SetCanvasVisible(saveCanvas, true);
            SetCanvasInteraction(saveCanvas, true);
            SetCanvasInteraction(loadCanvas, false);

            if (_saveLegacyAnimator != null && _saveLegacyAnimator.IsBound) _saveLegacyAnimator.PlayEnter();
            else _saveAnimator?.PlayEnter();

            if (_loadLegacyAnimator != null && _loadLegacyAnimator.IsBound) _loadLegacyAnimator.ResetHidden();
            else _loadAnimator?.ResetHidden();
        }
        else if (string.Equals(targetPanel, LoadPanelName, StringComparison.Ordinal))
        {
            CancelCanvasHide(ref _saveHideCoroutine);
            CancelCanvasHide(ref _loadHideCoroutine);
            SetCanvasVisible(loadCanvas, true);
            SetCanvasInteraction(saveCanvas, false);
            SetCanvasInteraction(loadCanvas, true);

            if (_saveLegacyAnimator != null && _saveLegacyAnimator.IsBound) _saveLegacyAnimator.ResetHidden();
            else _saveAnimator?.ResetHidden();

            if (_loadLegacyAnimator != null && _loadLegacyAnimator.IsBound) _loadLegacyAnimator.PlayEnter();
            else _loadAnimator?.PlayEnter();
        }
        else if (string.Equals(targetPanel, PausePanelName, StringComparison.Ordinal))
        {
            if (_saveLegacyAnimator != null && _saveLegacyAnimator.IsBound) _saveLegacyAnimator.PlayExit();
            else _saveAnimator?.PlayExit();

            if (_loadLegacyAnimator != null && _loadLegacyAnimator.IsBound) _loadLegacyAnimator.PlayExit();
            else _loadAnimator?.PlayExit();

            SetCanvasInteraction(saveCanvas, false);
            SetCanvasInteraction(loadCanvas, false);
            QueueCanvasHide(saveCanvas, ref _saveHideCoroutine, GetExitHideDelay(_saveLegacyAnimator, _saveAnimator));
            QueueCanvasHide(loadCanvas, ref _loadHideCoroutine, GetExitHideDelay(_loadLegacyAnimator, _loadAnimator));
        }
        else
        {
            ApplyPanelState(false);
            return;
        }

        if (pauseRaycaster != null)
        {
            pauseRaycaster.enabled = string.Equals(targetPanel, PausePanelName, StringComparison.Ordinal);
        }

        if (pausePicker != null)
        {
            pausePicker.MouseInputEnabled = string.Equals(targetPanel, PausePanelName, StringComparison.Ordinal);
        }

        _lastAppliedPanel = targetPanel;
    }

    public void Rebind()
    {
        pauseCanvas = FindChild("暂停菜单Canvas")?.gameObject;
        saveCanvas = FindChild("存档菜单Canvas")?.gameObject;
        loadCanvas = FindChild("加载菜单Canvas")?.gameObject;
        saveContentRoot = FindChild("存档菜单Canvas/存档菜单面板/界面组件") as RectTransform;
        loadContentRoot = FindChild("加载菜单Canvas/存档菜单面板/界面组件") as RectTransform;

        if (pauseCanvas != null)
        {
            pauseRaycaster = pauseCanvas.GetComponent<GraphicRaycaster>();
            pausePicker = pauseCanvas.GetComponentInChildren<SlotMachinePicker>(true);
        }
        else
        {
            pauseRaycaster = null;
            pausePicker = null;
        }

        BindPanelButton("暂停菜单Canvas/暂停菜单面板/暂停菜单按钮/View/暂停菜单按钮集合/存储", SavePanelName);
        BindPanelButton("暂停菜单Canvas/暂停菜单面板/暂停菜单按钮/View/暂停菜单按钮集合/加载", LoadPanelName);
        BindPanelButton("存档菜单Canvas/存档菜单面板/关闭按钮", PausePanelName, true);
        BindPanelButton("加载菜单Canvas/存档菜单面板/关闭按钮", PausePanelName, true);

        _saveAnimator ??= new PanelSlideAnimator(this, SavePanelName);
        _loadAnimator ??= new PanelSlideAnimator(this, LoadPanelName);
        _saveAnimator.Rebind(saveCanvas, saveContentRoot, SaveContentVisiblePosition);
        _loadAnimator.Rebind(loadCanvas, loadContentRoot, LoadContentVisiblePosition);
        _saveLegacyAnimator ??= new LegacyPanelAnimator(this, SavePanelName);
        _loadLegacyAnimator ??= new LegacyPanelAnimator(this, LoadPanelName);
        _saveLegacyAnimator.Rebind(saveCanvas, BuildSaveLegacyBindings());
        _loadLegacyAnimator.Rebind(loadCanvas, BuildLoadLegacyBindings());

        ApplyPanelState(true);
    }

    private void ApplyPanelState(bool force)
    {
        var panelManager = PanelManager.Instance;
        if (panelManager == null)
        {
            return;
        }

        string currentPanel = panelManager.CurrentPanel;
        if (!force && string.Equals(_lastAppliedPanel, currentPanel, System.StringComparison.Ordinal))
        {
            force = !IsVisualStateConsistent(currentPanel);
            if (!force)
            {
                return;
            }
        }

        string previousPanel = _lastAppliedPanel;
        _lastAppliedPanel = currentPanel;

        bool saveVisible = string.Equals(currentPanel, SavePanelName, System.StringComparison.Ordinal);
        bool loadVisible = string.Equals(currentPanel, LoadPanelName, System.StringComparison.Ordinal);
        bool saveWasVisible = string.Equals(previousPanel, SavePanelName, System.StringComparison.Ordinal);
        bool loadWasVisible = string.Equals(previousPanel, LoadPanelName, System.StringComparison.Ordinal);
        bool pauseCanvasRaycastEnabled = !saveVisible && !loadVisible;
        bool pausePickerInputEnabled = string.Equals(currentPanel, PausePanelName, System.StringComparison.Ordinal);

        ApplyAnimatedState(_saveLegacyAnimator, _saveAnimator, saveVisible, saveWasVisible, force);
        ApplyAnimatedState(_loadLegacyAnimator, _loadAnimator, loadVisible, loadWasVisible, force);

        if (pauseRaycaster != null)
        {
            pauseRaycaster.enabled = pauseCanvasRaycastEnabled;
        }

        if (pausePicker != null)
        {
            pausePicker.MouseInputEnabled = pausePickerInputEnabled;
        }
    }

    private bool IsVisualStateConsistent(string currentPanel)
    {
        bool saveVisible = string.Equals(currentPanel, SavePanelName, System.StringComparison.Ordinal);
        bool loadVisible = string.Equals(currentPanel, LoadPanelName, System.StringComparison.Ordinal);

        return IsAnimatorStateConsistent(_saveLegacyAnimator, _saveAnimator, saveVisible)
            && IsAnimatorStateConsistent(_loadLegacyAnimator, _loadAnimator, loadVisible);
    }

    private void BindPanelButton(string relativePath, string targetPanel, bool addButtonIfMissing = false)
    {
        Transform target = FindChild(relativePath);
        if (target == null)
        {
            return;
        }

        var button = target.GetComponent<Button>();
        if (button == null && addButtonIfMissing)
        {
            button = target.gameObject.AddComponent<Button>();
            button.targetGraphic = target.GetComponent<Graphic>();
        }

        if (button == null)
        {
            return;
        }

        if (button.targetGraphic == null)
        {
            button.targetGraphic = target.GetComponent<Graphic>();
        }

        var jumpButton = target.GetComponent<PauseMenuPanelJumpButton>();
        if (jumpButton == null)
        {
            jumpButton = target.gameObject.AddComponent<PauseMenuPanelJumpButton>();
        }

        jumpButton.Configure(button, targetPanel);
    }

    private void SetCanvasVisible(GameObject canvasObject, bool visible)
    {
        if (canvasObject == null || canvasObject.activeSelf == visible)
        {
            return;
        }

        canvasObject.SetActive(visible);
    }

    private void SetCanvasInteraction(GameObject canvasObject, bool enabled)
    {
        if (canvasObject == null)
        {
            return;
        }

        var raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = enabled;
        }
    }

    private Transform FindChild(string relativePath)
    {
        return transform.Find(relativePath);
    }

    private void QueueCanvasHide(GameObject canvasObject, ref Coroutine routine, float delay)
    {
        if (canvasObject == null)
        {
            return;
        }

        CancelCanvasHide(ref routine);
        routine = StartCoroutine(HideCanvasAfterDelay(canvasObject, delay));
    }

    private void CancelCanvasHide(ref Coroutine routine)
    {
        if (routine == null)
        {
            return;
        }

        StopCoroutine(routine);
        routine = null;
    }

    private IEnumerator HideCanvasAfterDelay(GameObject canvasObject, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        SetCanvasVisible(canvasObject, false);
    }

    private void ApplyExplicitAnimatedState()
    {
        var panelManager = PanelManager.Instance;
        if (panelManager == null)
        {
            return;
        }

        string currentPanel = panelManager.CurrentPanel;
        bool showSave = string.Equals(currentPanel, SavePanelName, StringComparison.Ordinal);
        bool showLoad = string.Equals(currentPanel, LoadPanelName, StringComparison.Ordinal);

        if (showSave)
        {
            PlayAnimatorEnter(_saveLegacyAnimator, _saveAnimator);
            ResetAnimatorHidden(_loadLegacyAnimator, _loadAnimator);
        }
        else if (showLoad)
        {
            ResetAnimatorHidden(_saveLegacyAnimator, _saveAnimator);
            PlayAnimatorEnter(_loadLegacyAnimator, _loadAnimator);
        }
        else
        {
            PlayAnimatorExit(_saveLegacyAnimator, _saveAnimator);
            PlayAnimatorExit(_loadLegacyAnimator, _loadAnimator);
        }

        if (pauseRaycaster != null)
        {
            pauseRaycaster.enabled = !showSave && !showLoad;
        }

        if (pausePicker != null)
        {
            pausePicker.MouseInputEnabled = string.Equals(currentPanel, PausePanelName, StringComparison.Ordinal);
        }

        _lastAppliedPanel = currentPanel;
    }

    private List<LegacyTweenBinding> BuildSaveLegacyBindings()
    {
        var bindings = new List<LegacyTweenBinding>(5);
        TryAddLegacyBinding(bindings, "存档菜单Canvas/存档菜单面板/背景板/纯黑背景", "BG in");
        TryAddLegacyBinding(bindings, "存档菜单Canvas/存档菜单面板/界面组件", "Save Main In");
        TryAddLegacyBinding(bindings, "存档菜单Canvas/存档菜单面板/背景板/SAVE字符", "Button left");
        TryAddLegacyBinding(bindings, "存档菜单Canvas/存档菜单面板/关闭按钮", "Button down");
        TryAddLegacyBinding(bindings, "存档菜单Canvas/存档菜单面板/WHERE TO SAVE IT", "Button down");
        return bindings;
    }

    private List<LegacyTweenBinding> BuildLoadLegacyBindings()
    {
        var bindings = new List<LegacyTweenBinding>(5);
        TryAddLegacyBinding(bindings, "加载菜单Canvas/存档菜单面板/背景板/纯黑背景", "BG in");
        TryAddLegacyBinding(bindings, "加载菜单Canvas/存档菜单面板/界面组件", "Read Main In");

        if (!TryAddLegacyBinding(bindings, "加载菜单Canvas/存档菜单面板/背景板/LOAD字符", "Button left")
            && !TryAddLegacyBinding(bindings, "加载菜单Canvas/存档菜单面板/背景板/SAVE字符", "Button left"))
        {
            TryAddLegacyBinding(bindings, "加载菜单Canvas/存档菜单面板/背景板/提示图翻牌器-加载菜单", "Button right");
        }

        TryAddLegacyBinding(bindings, "加载菜单Canvas/存档菜单面板/关闭按钮", "Button down");
        TryAddLegacyBinding(bindings, "加载菜单Canvas/存档菜单面板/WHERE TO READ", "Button down");
        return bindings;
    }

    private bool TryAddLegacyBinding(List<LegacyTweenBinding> bindings, string relativePath, string presetName, float delayAfterPlay = 0.1f)
    {
        Transform target = FindChild(relativePath);
        if (target == null)
        {
            return false;
        }

        var player = target.GetComponent<UITweenPlayer>();
        if (!LegacyTweenBinding.TryCreate(player, presetName, delayAfterPlay, out var binding))
        {
            return false;
        }

        bindings.Add(binding);
        return true;
    }

    private static void ApplyAnimatedState(LegacyPanelAnimator legacyAnimator, PanelSlideAnimator fallbackAnimator, bool shouldBeVisible, bool wasVisible, bool force)
    {
        if (!IsAnimatorBound(legacyAnimator, fallbackAnimator))
        {
            return;
        }

        if (force)
        {
            if (shouldBeVisible)
            {
                SnapAnimatorVisible(legacyAnimator, fallbackAnimator);
            }
            else
            {
                ResetAnimatorHidden(legacyAnimator, fallbackAnimator);
            }

            return;
        }

        if (shouldBeVisible)
        {
            PlayAnimatorEnter(legacyAnimator, fallbackAnimator);
            return;
        }

        if (wasVisible || IsAnimatorInExpectedState(legacyAnimator, fallbackAnimator, true))
        {
            PlayAnimatorExit(legacyAnimator, fallbackAnimator);
        }
        else
        {
            ResetAnimatorHidden(legacyAnimator, fallbackAnimator);
        }
    }

    private static bool IsAnimatorStateConsistent(LegacyPanelAnimator legacyAnimator, PanelSlideAnimator fallbackAnimator, bool shouldBeVisible)
    {
        return !IsAnimatorBound(legacyAnimator, fallbackAnimator)
            || IsAnimatorInExpectedState(legacyAnimator, fallbackAnimator, shouldBeVisible);
    }

    private static bool IsAnimatorBound(LegacyPanelAnimator legacyAnimator, PanelSlideAnimator fallbackAnimator)
    {
        return (legacyAnimator != null && legacyAnimator.IsBound)
            || (fallbackAnimator != null && fallbackAnimator.IsBound);
    }

    private static float GetExitHideDelay(LegacyPanelAnimator legacyAnimator, PanelSlideAnimator fallbackAnimator)
    {
        if (legacyAnimator != null && legacyAnimator.IsBound)
        {
            return legacyAnimator.EstimatedExitDuration;
        }

        return fallbackAnimator != null && fallbackAnimator.IsBound ? fallbackAnimator.EstimatedExitDuration : 0f;
    }

    private static bool IsAnimatorInExpectedState(LegacyPanelAnimator legacyAnimator, PanelSlideAnimator fallbackAnimator, bool shouldBeVisible)
    {
        if (legacyAnimator != null && legacyAnimator.IsBound)
        {
            return legacyAnimator.IsInExpectedState(shouldBeVisible);
        }

        return fallbackAnimator == null || !fallbackAnimator.IsBound || fallbackAnimator.IsInExpectedState(shouldBeVisible);
    }

    private static void SnapAnimatorVisible(LegacyPanelAnimator legacyAnimator, PanelSlideAnimator fallbackAnimator)
    {
        if (legacyAnimator != null && legacyAnimator.IsBound)
        {
            legacyAnimator.SnapVisible();
            return;
        }

        fallbackAnimator?.SnapVisible();
    }

    private static void ResetAnimatorHidden(LegacyPanelAnimator legacyAnimator, PanelSlideAnimator fallbackAnimator)
    {
        if (legacyAnimator != null && legacyAnimator.IsBound)
        {
            legacyAnimator.ResetHidden();
            return;
        }

        fallbackAnimator?.ResetHidden();
    }

    private static void PlayAnimatorEnter(LegacyPanelAnimator legacyAnimator, PanelSlideAnimator fallbackAnimator)
    {
        if (legacyAnimator != null && legacyAnimator.IsBound)
        {
            legacyAnimator.PlayEnter();
            return;
        }

        fallbackAnimator?.PlayEnter();
    }

    private static void PlayAnimatorExit(LegacyPanelAnimator legacyAnimator, PanelSlideAnimator fallbackAnimator)
    {
        if (legacyAnimator != null && legacyAnimator.IsBound)
        {
            legacyAnimator.PlayExit();
            return;
        }

        fallbackAnimator?.PlayExit();
    }

    private interface IPanelAnimator
    {
        bool IsBound { get; }
        bool IsInExpectedState(bool shouldBeVisible);
        void SnapVisible();
        void ResetHidden();
        void PlayEnter();
        void PlayExit();
    }

    private sealed class PanelSlideAnimator : IPanelAnimator
    {
        private const float EnterDuration = 0.48f;
        private const float ExitDuration = 0.26f;
        private const float PositionTolerance = 2f;

        private readonly MonoBehaviour _owner;

        private GameObject _canvasObject;
        private RectTransform _contentRoot;
        private Vector2 _hiddenPosition;
        private Vector2 _visiblePosition;
        private Coroutine _runningCoroutine;

        public float EstimatedExitDuration => ExitDuration;
        public bool IsBound => _canvasObject != null && _contentRoot != null;

        public PanelSlideAnimator(MonoBehaviour owner, string panelName)
        {
            _owner = owner;
        }

        public void Rebind(GameObject canvasObject, RectTransform contentRoot, Vector2 visiblePosition)
        {
            _canvasObject = canvasObject;
            _contentRoot = contentRoot;
            _visiblePosition = visiblePosition;

            if (_contentRoot != null)
            {
                _hiddenPosition = _contentRoot.anchoredPosition;
            }
        }

        public bool IsInExpectedState(bool shouldBeVisible)
        {
            if (!IsBound)
            {
                return true;
            }

            if (_runningCoroutine != null)
            {
                return true;
            }

            if (shouldBeVisible)
            {
                return _canvasObject.activeSelf
                    && Vector2.Distance(_contentRoot.anchoredPosition, _visiblePosition) <= PositionTolerance;
            }

            return !_canvasObject.activeSelf
                && Vector2.Distance(_contentRoot.anchoredPosition, _hiddenPosition) <= PositionTolerance;
        }

        public void SnapVisible()
        {
            if (!IsBound)
            {
                return;
            }

            StopRunningTween();
            _canvasObject.SetActive(true);
            _contentRoot.anchoredPosition = _visiblePosition;
        }

        public void ResetHidden()
        {
            if (!IsBound)
            {
                return;
            }

            StopRunningTween();
            _contentRoot.anchoredPosition = _hiddenPosition;
            _canvasObject.SetActive(false);
        }

        public void PlayEnter()
        {
            if (!IsBound)
            {
                return;
            }

            StopRunningTween();
            _canvasObject.SetActive(true);
            _runningCoroutine = _owner.StartCoroutine(AnimatePanel(
                _contentRoot.anchoredPosition,
                _visiblePosition,
                EnterDuration,
                EaseOutBack,
                disableOnComplete: false));
        }

        public void PlayExit()
        {
            if (!IsBound)
            {
                return;
            }

            StopRunningTween();
            _canvasObject.SetActive(true);
            _runningCoroutine = _owner.StartCoroutine(AnimatePanel(
                _contentRoot.anchoredPosition,
                _hiddenPosition,
                ExitDuration,
                EaseInCubic,
                disableOnComplete: true));
        }

        private IEnumerator AnimatePanel(
            Vector2 from,
            Vector2 to,
            float duration,
            Func<float, float> ease,
            bool disableOnComplete)
        {
            if (_contentRoot == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                _contentRoot.anchoredPosition = to;
                if (disableOnComplete && _canvasObject != null)
                {
                    _canvasObject.SetActive(false);
                }

                _runningCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            _contentRoot.anchoredPosition = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = ease(t);
                _contentRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                yield return null;
            }

            _contentRoot.anchoredPosition = to;

            if (disableOnComplete && _canvasObject != null)
            {
                _canvasObject.SetActive(false);
            }

            _runningCoroutine = null;
        }

        private void StopRunningTween()
        {
            if (_runningCoroutine == null)
            {
                return;
            }

            _owner.StopCoroutine(_runningCoroutine);
            _runningCoroutine = null;
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float shifted = t - 1f;
            return 1f + ((overshoot + 1f) * shifted * shifted * shifted) + (overshoot * shifted * shifted);
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }
    }

    private sealed class LegacyPanelAnimator : IPanelAnimator
    {
        private const float PositionTolerance = 2f;
        private const float RotationTolerance = 0.5f;
        private const float AlphaTolerance = 0.02f;

        private readonly MonoBehaviour _owner;
        private readonly string _panelName;

        private GameObject _canvasObject;
        private readonly List<LegacyTweenBinding> _bindings = new();
        private Coroutine _runningCoroutine;

        public float EstimatedExitDuration => CalculateSequenceDuration(reverse: true);
        public bool IsBound => _canvasObject != null && _bindings.Count > 0;

        public LegacyPanelAnimator(MonoBehaviour owner, string panelName)
        {
            _owner = owner;
            _panelName = panelName;
        }

        public void Rebind(GameObject canvasObject, List<LegacyTweenBinding> bindings)
        {
            StopRunningAnimation();
            _canvasObject = canvasObject;
            _bindings.Clear();

            if (bindings == null)
            {
                return;
            }

            foreach (var binding in bindings)
            {
                if (binding != null && binding.IsBound)
                {
                    _bindings.Add(binding);
                }
            }
        }

        public bool IsInExpectedState(bool shouldBeVisible)
        {
            if (!IsBound || _runningCoroutine != null)
            {
                return true;
            }

            if (shouldBeVisible)
            {
                if (!_canvasObject.activeSelf)
                {
                    return false;
                }

                foreach (var binding in _bindings)
                {
                    if (!binding.MatchesVisible(PositionTolerance, RotationTolerance, AlphaTolerance))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (_canvasObject.activeSelf)
            {
                return false;
            }

            foreach (var binding in _bindings)
            {
                if (!binding.MatchesHidden(PositionTolerance, RotationTolerance, AlphaTolerance))
                {
                    return false;
                }
            }

            return true;
        }

        public void SnapVisible()
        {
            if (!IsBound)
            {
                return;
            }

            StopRunningAnimation();
            _canvasObject.SetActive(true);
            foreach (var binding in _bindings)
            {
                binding.ApplyVisible();
            }
        }

        public void ResetHidden()
        {
            if (!IsBound)
            {
                return;
            }

            StopRunningAnimation();
            foreach (var binding in _bindings)
            {
                binding.ApplyHidden();
            }

            _canvasObject.SetActive(false);
        }

        public void PlayEnter()
        {
            if (!IsBound)
            {
                return;
            }

            StopRunningAnimation();
            _canvasObject.SetActive(true);
            ScheduleBindings(reverse: false);
            _runningCoroutine = _owner.StartCoroutine(CompleteAfterDelay(CalculateSequenceDuration(reverse: false), disableOnComplete: false));
        }

        public void PlayExit()
        {
            if (!IsBound)
            {
                return;
            }

            StopRunningAnimation();
            _canvasObject.SetActive(true);
            ScheduleBindings(reverse: true);
            _runningCoroutine = _owner.StartCoroutine(CompleteAfterDelay(CalculateSequenceDuration(reverse: true), disableOnComplete: true));
        }

        private void ScheduleBindings(bool reverse)
        {
            float startedAt = 0f;

            if (reverse)
            {
                for (int i = _bindings.Count - 1; i >= 0; i--)
                {
                    LegacyTweenBinding binding = _bindings[i];
                    binding.PlayReverse(startedAt);

                    if (i > 0)
                    {
                        startedAt += Mathf.Max(0f, binding.DelayAfterPlay);
                    }
                }
            }
            else
            {
                for (int i = 0; i < _bindings.Count; i++)
                {
                    LegacyTweenBinding binding = _bindings[i];
                    binding.PlayForward(startedAt);

                    if (i < _bindings.Count - 1)
                    {
                        startedAt += Mathf.Max(0f, binding.DelayAfterPlay);
                    }
                }
            }
        }

        private IEnumerator CompleteAfterDelay(float totalDuration, bool disableOnComplete)
        {
            if (totalDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(totalDuration);
            }

            if (disableOnComplete && _canvasObject != null)
            {
                _canvasObject.SetActive(false);
            }

            _runningCoroutine = null;
        }

        private float CalculateSequenceDuration(bool reverse)
        {
            float startedAt = 0f;
            float total = 0f;

            if (reverse)
            {
                for (int i = _bindings.Count - 1; i >= 0; i--)
                {
                    LegacyTweenBinding binding = _bindings[i];
                    total = Mathf.Max(total, startedAt + binding.AnimationDuration);
                    if (i > 0)
                    {
                        startedAt += Mathf.Max(0f, binding.DelayAfterPlay);
                    }
                }
            }
            else
            {
                for (int i = 0; i < _bindings.Count; i++)
                {
                    LegacyTweenBinding binding = _bindings[i];
                    total = Mathf.Max(total, startedAt + binding.AnimationDuration);
                    if (i < _bindings.Count - 1)
                    {
                        startedAt += Mathf.Max(0f, binding.DelayAfterPlay);
                    }
                }
            }

            return total;
        }

        private void StopRunningAnimation()
        {
            if (_runningCoroutine == null)
            {
                foreach (var binding in _bindings)
                {
                    binding.Stop();
                }

                return;
            }

            _owner.StopCoroutine(_runningCoroutine);
            _runningCoroutine = null;

            foreach (var binding in _bindings)
            {
                binding.Stop();
            }
        }
    }

    private sealed class LegacyTweenBinding
    {
        private struct VisualState
        {
            public Vector2 Position;
            public Vector2 Size;
            public Vector3 EulerAngles;
            public float? Alpha;
        }

        private readonly UITweenPreset _preset;
        private readonly RectTransform _rectTransform;
        private readonly Graphic _graphic;
        private readonly CanvasGroup _canvasGroup;
        private readonly VisualState _hiddenState;
        private readonly VisualState _visibleState;
        private DG.Tweening.Tween _activeTween;

        public UITweenPlayer Player { get; }
        public string PresetName { get; }
        public float DelayAfterPlay { get; }
        public float AnimationDuration { get; }
        public bool IsBound => Player != null && _preset != null && _rectTransform != null;

        private LegacyTweenBinding(UITweenPlayer player, UITweenPreset preset, float delayAfterPlay)
        {
            Player = player;
            _preset = preset;
            PresetName = preset.presetName;
            DelayAfterPlay = delayAfterPlay;
            AnimationDuration = Mathf.Max(0.01f, preset.duration + Mathf.Max(0f, preset.delay));
            _rectTransform = player.GetComponent<RectTransform>();
            _graphic = player.GetComponent<Graphic>();
            _canvasGroup = player.GetComponent<CanvasGroup>();

            _hiddenState = CaptureCurrentState();
            _visibleState = new VisualState
            {
                Position = ResolveVisiblePosition(_hiddenState.Position, preset),
                Size = ResolveVisibleSize(_hiddenState.Size, preset),
                EulerAngles = ResolveVisibleRotation(_hiddenState.EulerAngles, preset),
                Alpha = ResolveVisibleAlpha(_hiddenState.Alpha, preset)
            };
        }

        public static bool TryCreate(UITweenPlayer player, string presetName, float delayAfterPlay, out LegacyTweenBinding binding)
        {
            binding = null;
            if (player == null || string.IsNullOrEmpty(presetName))
            {
                return false;
            }

            if (!TryResolvePreset(player, presetName, out UITweenPreset preset) || preset == null)
            {
                return false;
            }

            binding = new LegacyTweenBinding(player, preset, delayAfterPlay);
            return binding.IsBound;
        }

        public void PlayForward(float extraDelay = 0f)
        {
            StartTween(CaptureCurrentState(), _visibleState, extraDelay);
        }

        public void PlayReverse(float extraDelay = 0f)
        {
            StartTween(CaptureCurrentState(), _hiddenState, extraDelay);
        }

        public void ApplyVisible()
        {
            Stop();
            ApplyState(_visibleState);
        }

        public void ApplyHidden()
        {
            Stop();
            ApplyState(_hiddenState);
        }

        public bool MatchesVisible(float positionTolerance, float rotationTolerance, float alphaTolerance)
        {
            return MatchesState(_visibleState, positionTolerance, rotationTolerance, alphaTolerance);
        }

        public bool MatchesHidden(float positionTolerance, float rotationTolerance, float alphaTolerance)
        {
            return MatchesState(_hiddenState, positionTolerance, rotationTolerance, alphaTolerance);
        }

        public void Stop()
        {
            if (_activeTween == null)
            {
                return;
            }

            if (DG.Tweening.TweenExtensions.IsActive(_activeTween))
            {
                DG.Tweening.TweenExtensions.Kill(_activeTween, false);
            }

            _activeTween = null;
        }

        private void StartTween(VisualState from, VisualState to, float extraDelay)
        {
            Stop();

            if (_preset.duration <= 0f)
            {
                ApplyState(to);
                return;
            }

            var tween = DG.Tweening.DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, _preset.duration), value =>
            {
                VisualState state = LerpState(from, to, value);
                ApplyState(state);
            });
            _preset.ApplyTweenSettings(tween);
            DG.Tweening.TweenSettingsExtensions.SetUpdate(tween, _preset.unscaledTime);
            float totalDelay = Mathf.Max(0f, extraDelay) + Mathf.Max(0f, _preset.delay);
            if (totalDelay > 0f)
            {
                DG.Tweening.TweenSettingsExtensions.SetDelay(tween, totalDelay);
            }

            DG.Tweening.TweenSettingsExtensions.OnKill(tween, () =>
            {
                if (ReferenceEquals(_activeTween, tween))
                {
                    _activeTween = null;
                }
            });
            DG.Tweening.TweenSettingsExtensions.OnComplete(tween, () =>
            {
                ApplyState(to);
                if (ReferenceEquals(_activeTween, tween))
                {
                    _activeTween = null;
                }
            });

            _activeTween = tween;
        }

        private VisualState CaptureCurrentState()
        {
            return new VisualState
            {
                Position = _rectTransform != null ? _rectTransform.anchoredPosition : Vector2.zero,
                Size = _rectTransform != null ? _rectTransform.sizeDelta : Vector2.zero,
                EulerAngles = _rectTransform != null ? _rectTransform.localEulerAngles : Vector3.zero,
                Alpha = ReadCurrentAlpha(_canvasGroup, _graphic)
            };
        }

        private static VisualState LerpState(VisualState from, VisualState to, float t)
        {
            return new VisualState
            {
                Position = Vector2.LerpUnclamped(from.Position, to.Position, t),
                Size = Vector2.LerpUnclamped(from.Size, to.Size, t),
                EulerAngles = Vector3.LerpUnclamped(from.EulerAngles, to.EulerAngles, t),
                Alpha = from.Alpha.HasValue || to.Alpha.HasValue
                    ? Mathf.LerpUnclamped(from.Alpha ?? to.Alpha ?? 1f, to.Alpha ?? from.Alpha ?? 1f, t)
                    : (float?)null
            };
        }

        private void ApplyState(VisualState state)
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = state.Position;
                _rectTransform.sizeDelta = state.Size;
                _rectTransform.localEulerAngles = state.EulerAngles;
            }

            if (state.Alpha.HasValue)
            {
                SetAlpha(state.Alpha.Value);
            }
        }

        private bool MatchesState(VisualState state, float positionTolerance, float rotationTolerance, float alphaTolerance)
        {
            if (_rectTransform != null)
            {
                if (Vector2.Distance(_rectTransform.anchoredPosition, state.Position) > positionTolerance)
                {
                    return false;
                }

                if (Vector2.Distance(_rectTransform.sizeDelta, state.Size) > positionTolerance)
                {
                    return false;
                }

                if (Quaternion.Angle(Quaternion.Euler(_rectTransform.localEulerAngles), Quaternion.Euler(state.EulerAngles)) > rotationTolerance)
                {
                    return false;
                }
            }

            if (state.Alpha.HasValue)
            {
                float? currentAlpha = ReadCurrentAlpha(_canvasGroup, _graphic);
                if (!currentAlpha.HasValue || Mathf.Abs(currentAlpha.Value - state.Alpha.Value) > alphaTolerance)
                {
                    return false;
                }
            }

            return true;
        }

        private void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
            else if (_graphic != null)
            {
                Color color = _graphic.color;
                color.a = alpha;
                _graphic.color = color;
            }
        }

        private static Vector2 ResolveVisiblePosition(Vector2 hiddenPosition, UITweenPreset preset)
        {
            if (!preset.animatePosition)
            {
                return hiddenPosition;
            }

            return preset.useRelativeMode
                ? hiddenPosition + preset.targetAnchoredPosition
                : preset.targetAnchoredPosition;
        }

        private static Vector2 ResolveVisibleSize(Vector2 hiddenSize, UITweenPreset preset)
        {
            if (!preset.animateSize)
            {
                return hiddenSize;
            }

            return preset.useRelativeMode
                ? hiddenSize + preset.targetSizeDelta
                : preset.targetSizeDelta;
        }

        private static Vector3 ResolveVisibleRotation(Vector3 hiddenEulerAngles, UITweenPreset preset)
        {
            if (!preset.animateRotation)
            {
                return hiddenEulerAngles;
            }

            return preset.useRelativeMode
                ? hiddenEulerAngles + preset.targetEulerAngles
                : preset.targetEulerAngles;
        }

        private static float? ResolveVisibleAlpha(float? hiddenAlpha, UITweenPreset preset)
        {
            if (!preset.animateAlpha || !hiddenAlpha.HasValue)
            {
                return hiddenAlpha;
            }

            float visibleAlpha = preset.useRelativeMode
                ? hiddenAlpha.Value + preset.targetAlpha
                : preset.targetAlpha;
            return Mathf.Clamp01(visibleAlpha);
        }

        private static float? ReadCurrentAlpha(CanvasGroup canvasGroup, Graphic graphic)
        {
            if (canvasGroup != null)
            {
                return canvasGroup.alpha;
            }

            if (graphic != null)
            {
                return graphic.color.a;
            }

            return null;
        }

        private static bool TryResolvePreset(UITweenPlayer player, string presetName, out UITweenPreset preset)
        {
            preset = null;

            foreach (var item in player.presets)
            {
                if (item != null && item.presetName == presetName)
                {
                    preset = item;
                    return true;
                }
            }

            foreach (var library in player.libraries)
            {
                if (library != null && library.TryGet(presetName, out preset) && preset != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

[DisallowMultipleComponent]
public sealed class PauseMenuPanelJumpButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string targetPanel;

    private bool _registered;

    public void Configure(Button sourceButton, string panelName)
    {
        if (button != sourceButton)
        {
            Unregister();
            button = sourceButton;
        }

        targetPanel = panelName;
        Register();
    }

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void Register()
    {
        if (_registered || button == null)
        {
            return;
        }

        button.onClick.AddListener(HandleClick);
        _registered = true;
    }

    private void Unregister()
    {
        if (!_registered || button == null)
        {
            return;
        }

        button.onClick.RemoveListener(HandleClick);
        _registered = false;
    }

    private void HandleClick()
    {
        if (string.IsNullOrEmpty(targetPanel))
        {
            return;
        }

        var runtimeFix = GetComponentInParent<PauseMenuRouteRuntimeFix>();
        if (runtimeFix != null)
        {
            runtimeFix.JumpToPanel(targetPanel);
            return;
        }

        if (PanelManager.Instance == null)
        {
            return;
        }

        PanelManager.Instance.ChangePanel(targetPanel);
    }
}
