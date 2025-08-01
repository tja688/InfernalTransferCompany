// Recompile at 2025/7/1 10:25:16

// Copyright (c) Pixel Crushers. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem
{

    [AddComponentMenu("")] // Use wrapper.
    public class StandardUIMenuPanel : UIPanel
    {

        #region Serialized Fields

        [Tooltip("(Optional) Main response menu panel.")]
        public UnityEngine.UI.Graphic panel;

        [Tooltip("(Optional) Image to show PC portrait during response menu.")]
        public UnityEngine.UI.Image pcImage;

        [Tooltip("(Optional) Text element to show PC name during response menu.")]
        public UITextField pcName;

        [Tooltip("Set PC Image to actor portrait's native size. Image's Rect Transform can't use Stretch anchors.")]
        public bool usePortraitNativeSize = false;

        [Tooltip("(Optional) Slider for timed menus.")]
        public UnityEngine.UI.Slider timerSlider;

        [Tooltip("Assign design-time positioned buttons starting with first or last button.")]
        public ResponseButtonAlignment buttonAlignment = ResponseButtonAlignment.ToFirst;

        [Tooltip("Show buttons that aren't assigned to any responses. If using a 'dialogue wheel' for example, you'll want to show unused buttons so the entire wheel structure is visible.")]
        public bool showUnusedButtons = false;

        [Tooltip("Design-time positioned response buttons. (Optional if Button Template is assigned.)")]
        public StandardUIResponseButton[] buttons;

        [Tooltip("Template from which to instantiate response buttons. (Optional if using Buttons list above.)")]
        public StandardUIResponseButton buttonTemplate;

        [Tooltip("If using Button Template, instantiate buttons under this GameObject.")]
        public UnityEngine.UI.Graphic buttonTemplateHolder;

        [Tooltip("(Optional) Scrollbar to use if instantiated button holder is in a scroll rect.")]
        public UnityEngine.UI.Scrollbar buttonTemplateScrollbar;

        [Tooltip("(Optional) Component that enables or disables scrollbar as necessary for content.")]
        public UIScrollbarEnabler scrollbarEnabler;

        [Tooltip("Reset the scroll bar to this value when preparing response menu. To skip resetting the scrollbar, specify a negative value.")]
        public float buttonTemplateScrollbarResetValue = 1;

        [Tooltip("Automatically set up explicit joystick/keyboard navigation for instantiated template buttons instead of using Automatic navigation.")]
        public bool explicitNavigationForTemplateButtons = true;

        [Tooltip("If explicit navigation is enabled, loop around when navigating past end of menu.")]
        public bool loopExplicitNavigation = false;

        public UIAutonumberSettings autonumber = new UIAutonumberSettings();

        [Tooltip("If non-zero, prevent input for this duration in seconds when opening menu.")]
        public float blockInputDuration = 0;

        [Tooltip("During block input duration, keep selected response button in selected visual state.")]
        public bool showSelectionWhileInputBlocked = false;

        [Tooltip("Log a warning if a response button text is blank.")]
        public bool warnOnEmptyResponseText = false;

        public UnityEvent onContentChanged = new UnityEvent();

        [Tooltip("When focusing panel, set this animator trigger.")]
        public string focusAnimationTrigger = string.Empty;

        [Tooltip("When unfocusing panel, set this animator trigger.")]
        public string unfocusAnimationTrigger = string.Empty;

        [Tooltip("Wait for panels within this dialogue UI (not external) to close before showing menu.")]
        public bool waitForClose = false;

        /// <summary>
        /// Invoked when the subtitle panel gains focus.
        /// </summary>
        public UnityEvent onFocus = new UnityEvent();

        /// <summary>
        /// Invoked when the subtitle panel loses focus.
        /// </summary>
        public UnityEvent onUnfocus = new UnityEvent();

        #endregion

        #region Public Properties

        [SerializeField, Tooltip("Panel is currently in focused state.")]
        private bool m_hasFocus = false;
        public virtual bool hasFocus
        {
            get { return m_hasFocus; }
            protected set { m_hasFocus = value; }
        }

        public override bool waitForShowAnimation { get { return true; } }

        /// <summary>
        /// The instantiated buttons. These are only valid during a specific response menu,
        /// and only if you're using templates. Each showing of the response menu clears 
        /// this list and re-populates it with new buttons.
        /// </summary>
        public List<GameObject> instantiatedButtons { get { return m_instantiatedButtons; } }
        private List<GameObject> m_instantiatedButtons = new List<GameObject>();

        #endregion

        #region Internal Fields

        protected List<GameObject> instantiatedButtonPool { get { return m_instantiatedButtonPool; } }
        private List<GameObject> m_instantiatedButtonPool = new List<GameObject>();
        private string m_processedAutonumberFormat = string.Empty;
        private Coroutine m_scrollbarCoroutine = null;
        protected const float WaitForCloseTimeoutDuration = 8f;

        protected StandardUITimer m_timer = null;
        protected System.Action m_timeoutHandler = null;
        protected CanvasGroup m_mainCanvasGroup = null;
        protected static bool s_isInputDisabled = false;
        private StandardDialogueUI m_dialogueUI = null;
        protected StandardDialogueUI dialogueUI
        {
            get
            {
                if (m_dialogueUI == null) m_dialogueUI = GetComponentInParent<StandardDialogueUI>();
                return m_dialogueUI ?? DialogueManager.standardDialogueUI;
            }
        }

        #endregion

        #region Initialization

        public virtual void Awake()
        {
            Tools.SetGameObjectActive(buttonTemplate, false);
        }

        #endregion

        #region Show & Hide

        protected override void Update()
        {
            if (s_isInputDisabled)
            {
                if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
            }
            else
            {
                base.Update();
            }
        }

        public override void CheckFocus()
        {
            if (s_isInputDisabled) return;
            base.CheckFocus();
        }

        public virtual void SetPCPortrait(Sprite portraitSprite, string portraitName)
        {
            if (pcImage != null)
            {
                Tools.SetGameObjectActive(pcImage, portraitSprite != null);
                pcImage.sprite = portraitSprite;
                if (usePortraitNativeSize && portraitSprite != null)
                {
                    pcImage.rectTransform.sizeDelta = portraitSprite.packed ?
                        new Vector2(portraitSprite.rect.width, portraitSprite.rect.height) :
                        new Vector2(portraitSprite.texture.width, portraitSprite.texture.height);
                }
            }
            pcName.text = portraitName;
        }

        [System.Obsolete("Use SetPCPortrait(Sprite,string) instead.")]
        public virtual void SetPCPortrait(Texture2D portraitTexture, string portraitName)
        {
            SetPCPortrait(UITools.CreateSprite(portraitTexture), portraitName);
        }

        public virtual void ShowResponses(Subtitle subtitle, Response[] responses, Transform target)
        {
            if (waitForClose && dialogueUI != null)
            {
                if (dialogueUI.AreAnyPanelsClosing())
                {
                    DialogueManager.instance.StartCoroutine(ShowAfterPanelsClose(subtitle, responses, target));
                    return;
                }
            }
            CheckForBlankResponses(responses);
            ShowResponsesNow(subtitle, responses, target);
        }

        private void CheckForBlankResponses(Response[] responses)
        {
            if (!DialogueDebug.logWarnings) return;
            if (responses == null) return;
            foreach (Response response in responses)
            {
                if (string.IsNullOrEmpty(response.formattedText.text))
                {
                    Debug.LogWarning($"Dialogue System: Response [{response.destinationEntry.conversationID}:{response.destinationEntry.id}] has no text for a response button.");
                }
            }
        }

        protected virtual void ShowResponsesNow(Subtitle subtitle, Response[] responses, Transform target)
        {
            if (responses == null || responses.Length == 0)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: StandardDialogueUI ShowResponses received an empty list of responses.", this);
                return;
            }
            ClearResponseButtons();
            SetResponseButtons(responses, target);
            ActivateUIElements();
            Open();
            Focus();
            RefreshSelectablesList();
            if (blockInputDuration > 0)
            {
                DisableInput();
                if (InputDeviceManager.autoFocus) SetFocus(firstSelected);
                if (Mathf.Approximately(0, Time.timeScale))
                { 
                    StartCoroutine(EnableInputAfterDuration(blockInputDuration));
                }
                else
                {
                    Invoke(nameof(EnableInput), blockInputDuration);
                }
            }
            else
            {
                if (InputDeviceManager.autoFocus) SetFocus(firstSelected);
                if (s_isInputDisabled) EnableInput();
            }
#if TMP_PRESENT
            DialogueManager.instance.StartCoroutine(CheckTMProAutoScroll());
#endif
        }

        private IEnumerator EnableInputAfterDuration(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            EnableInput();
        }

#if TMP_PRESENT
        // Handles edge case where TMPro uses autoscroll but entry ends before typing starts.
        // In this case, this method updates the autoscroll size.
        protected IEnumerator CheckTMProAutoScroll()
        {
            var ui = GetComponentInParent<StandardDialogueUI>();
            if (ui == null || ui.conversationUIElements.defaultNPCSubtitlePanel == null || ui.conversationUIElements.defaultNPCSubtitlePanel.subtitleText == null) yield break;
            var tmp = ui.conversationUIElements.defaultNPCSubtitlePanel.subtitleText.textMeshProUGUI;
            if (tmp == null) yield break;
            var layoutElement = tmp.GetComponent<UnityEngine.UI.LayoutElement>();
            if (layoutElement != null) layoutElement.preferredHeight = -1;
            var uiScrollbarEnabler = GetComponentInParent<UIScrollbarEnabler>();
            if (uiScrollbarEnabler != null)
            {
                yield return null;
                uiScrollbarEnabler.CheckScrollbarWithResetValue(buttonTemplateScrollbarResetValue);
            }
        }
#endif

        protected virtual IEnumerator ShowAfterPanelsClose(Subtitle subtitle, Response[] responses, Transform target)
        {
            if (dialogueUI != null)
            {
                float safeguardTime = Time.realtimeSinceStartup + WaitForCloseTimeoutDuration;
                while (dialogueUI.AreAnyPanelsClosing() && Time.realtimeSinceStartup < safeguardTime)
                {
                    yield return null;
                }
            }
            ShowResponsesNow(subtitle, responses, target);
        }

        public virtual void HideResponses()
        {
            StopTimer();
            Unfocus();
            Close();
        }

        public override void Close()
        {
            if (isOpen) base.Close();
        }

        public virtual void Focus()
        {
            if (hasFocus) return;
            if (panelState == PanelState.Opening && enabled && gameObject.activeInHierarchy)
            {
                StartCoroutine(FocusWhenOpen());
            }
            else
            {
                FocusNow();
            }
        }

        protected IEnumerator FocusWhenOpen()
        {
            float timeout = Time.realtimeSinceStartup + 5f;
            while (panelState != PanelState.Open && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
            FocusNow();
        }

        protected virtual void FocusNow()
        {
            panelState = PanelState.Open;
            animatorMonitor.SetTrigger(focusAnimationTrigger, null, false);
            UITools.EnableInteractivity(gameObject);
            if (hasFocus) return;
            if (string.IsNullOrEmpty(focusAnimationTrigger))
            {
                OnFocused();
            }
            else
            {
                animatorMonitor.SetTrigger(focusAnimationTrigger, OnFocused, true);
            }
            onFocus.Invoke();
        }

        private void OnFocused()
        {
            hasFocus = true;
        }

        public virtual void Unfocus()
        {
            if (!hasFocus) return;
            hasFocus = false;
            animatorMonitor.SetTrigger(unfocusAnimationTrigger, null, false);
            onUnfocus.Invoke();
        }

        protected void ActivateUIElements()
        {
            SetUIElementsActive(true);
        }

        protected void DeactivateUIElements()
        {
            SetUIElementsActive(false);
        }

        protected virtual void SetUIElementsActive(bool value)
        {
            Tools.SetGameObjectActive(panel, value);
            Tools.SetGameObjectActive(pcImage, value && pcImage != null && pcImage.sprite != null);
            pcName.SetActive(value);
            Tools.SetGameObjectActive(timerSlider, false); // Let StartTimer activate if needed.
            if (value == false) ClearResponseButtons();
        }

        public virtual void HideImmediate()
        {
            OnHidden();
        }

        protected virtual void ClearResponseButtons()
        {
            DestroyInstantiatedButtons();
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == null) continue;
                    buttons[i].Reset();
                    buttons[i].isVisible = showUnusedButtons;
                    buttons[i].gameObject.SetActive(showUnusedButtons);
                }
            }
        }

        /// <summary>
        /// Sets the response buttons.
        /// </summary>
        /// <param name='responses'>Responses.</param>
        /// <param name='target'>Target that will receive OnClick events from the buttons.</param>
        protected virtual void SetResponseButtons(Response[] responses, Transform target)
        {
            firstSelected = null;
            DestroyInstantiatedButtons();
            var hasDisabledButton = false;

            // Prep autonumber format:
            if (autonumber.enabled)
            {
                m_processedAutonumberFormat = FormattedText.Parse(autonumber.format.Replace("\\t", "\t").Replace("\\n", "\n")).text;
            }

            if ((buttons != null) && (responses != null))
            {
                // Add explicitly-positioned buttons:
                int buttonNumber = 0;
                for (int i = 0; i < responses.Length; i++)
                {
                    if (responses[i].formattedText.position != FormattedText.NoAssignedPosition)
                    {
                        int position = responses[i].formattedText.position;
                        if (0 <= position && position < buttons.Length && buttons[position] != null)
                        {
                            SetResponseButton(buttons[position], responses[i], target, buttonNumber++);
                        }
                        else
                        {
                            Debug.LogWarning("Dialogue System: Buttons list doesn't contain a button for position " + position + ".", this);
                        }
                    }
                }

                if ((buttonTemplate != null) && (buttonTemplateHolder != null))
                {
                    if (scrollbarEnabler != null) CheckScrollbar();

                    // Instantiate buttons from template:
                    for (int i = 0; i < responses.Length; i++)
                    {
                        if (responses[i].formattedText.position != FormattedText.NoAssignedPosition) continue;
                        GameObject buttonGameObject = InstantiateButton();
                        if (buttonGameObject == null)
                        {
                            Debug.LogError("Dialogue System: Couldn't instantiate response button template.");
                        }
                        else
                        {
                            instantiatedButtons.Add(buttonGameObject);
                            buttonGameObject.transform.SetParent(buttonTemplateHolder.transform, false);
                            buttonGameObject.transform.SetAsLastSibling();
                            buttonGameObject.SetActive(true);
                            StandardUIResponseButton responseButton = buttonGameObject.GetComponent<StandardUIResponseButton>();
                            SetResponseButton(responseButton, responses[i], target, buttonNumber++);
                            if (responseButton != null)
                            {
                                buttonGameObject.name = "Response: " + responseButton.text;
                                if (explicitNavigationForTemplateButtons && !responseButton.isClickable) hasDisabledButton = true;
                            }
                            if (firstSelected == null) firstSelected = buttonGameObject;

                        }
                    }
                }
                else
                {
                    // Auto-position remaining buttons:
                    if (buttonAlignment == ResponseButtonAlignment.ToFirst)
                    {
                        // Align to first, so add in order to front:
                        for (int i = 0; i < Mathf.Min(buttons.Length, responses.Length); i++)
                        {
                            if (responses[i].formattedText.position == FormattedText.NoAssignedPosition)
                            {
                                int position = Mathf.Clamp(GetNextAvailableResponseButtonPosition(0, 1), 0, buttons.Length - 1);
                                SetResponseButton(buttons[position], responses[i], target, buttonNumber++);
                                if (firstSelected == null) firstSelected = buttons[position].gameObject;
                            }
                        }
                    }
                    else
                    {
                        // Align to last, so add in reverse order to back:
                        for (int i = Mathf.Min(buttons.Length, responses.Length) - 1; i >= 0; i--)
                        {
                            if (responses[i].formattedText.position == FormattedText.NoAssignedPosition)
                            {
                                int position = Mathf.Clamp(GetNextAvailableResponseButtonPosition(buttons.Length - 1, -1), 0, buttons.Length - 1);
                                SetResponseButton(buttons[position], responses[i], target, buttonNumber++);
                                firstSelected = buttons[position].gameObject;
                            }
                        }
                    }
                }
            }

            if (explicitNavigationForTemplateButtons) SetupTemplateButtonNavigation(hasDisabledButton);

            NotifyContentChanged();
        }

        protected virtual void CheckScrollbar()
        {
            if (scrollbarEnabler == null) return;
            if (m_scrollbarCoroutine != null) StopCoroutine(m_scrollbarCoroutine);
            m_scrollbarCoroutine = dialogueUI.StartCoroutine(CheckScrollbarCoroutine());
        }

        protected IEnumerator CheckScrollbarCoroutine()
        {
            var timeout = Time.realtimeSinceStartup + UIAnimatorMonitor.MaxWaitDuration;
            while (!isOpen && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
            if (buttonTemplateScrollbarResetValue >= 0)
            {
                if (buttonTemplateScrollbar != null) buttonTemplateScrollbar.value = buttonTemplateScrollbarResetValue;
                if (scrollbarEnabler != null)
                {
                    scrollbarEnabler.CheckScrollbarWithResetValue(buttonTemplateScrollbarResetValue);
                }
            }
            else if (scrollbarEnabler != null)
            {
                scrollbarEnabler.CheckScrollbar();
            }
        }

        protected virtual void SetResponseButton(StandardUIResponseButton button, Response response, Transform target, int buttonNumber)
        {
            if (button != null)
            {
                button.response = response;
                button.gameObject.SetActive(true);
                button.isVisible = true;
                button.isClickable = response.enabled;
                button.target = target;
                if (response != null)
                {
                    if (warnOnEmptyResponseText && DialogueDebug.logWarnings && string.IsNullOrEmpty(response.formattedText.text))
                    {
                        Debug.LogWarning($"Dialogue System: Response entry [{response.destinationEntry.id}] menu text is blank.", button);
                    }
                    button.SetFormattedText(response.formattedText);
                }

                // Auto-number:
                if (autonumber.enabled)
                {
                    button.text = string.Format(m_processedAutonumberFormat, buttonNumber + 1, button.text);
                    // Add UIButtonKeyTrigger(s) if needed:
                    var numKeyTriggersNeeded = 0;
                    if (autonumber.regularNumberHotkeys) numKeyTriggersNeeded++;
                    if (autonumber.numpadHotkeys) numKeyTriggersNeeded++;
                    var keyTriggers = button.GetComponents<UIButtonKeyTrigger>();
                    if (keyTriggers.Length < numKeyTriggersNeeded)
                    {
                        for (int i = keyTriggers.Length; i < numKeyTriggersNeeded; i++)
                        {
                            button.gameObject.AddComponent<UIButtonKeyTrigger>();
                        }
                        keyTriggers = button.GetComponents<UIButtonKeyTrigger>();
                    }
                    int index = 0;
                    if (autonumber.regularNumberHotkeys)
                    {
                        keyTriggers[index++].key = (KeyCode)((int)KeyCode.Alpha1 + buttonNumber);
                    }
                    if (autonumber.numpadHotkeys)
                    {
                        keyTriggers[index].key = (KeyCode)((int)KeyCode.Keypad1 + buttonNumber);
                    }
                }
            }
        }

        protected int GetNextAvailableResponseButtonPosition(int start, int direction)
        {
            if (buttons != null)
            {
                int position = start;
                while ((0 <= position) && (position < buttons.Length))
                {
                    if (buttons[position].isVisible && buttons[position].response != null)
                    {
                        position += direction;
                    }
                    else
                    {
                        return position;
                    }
                }
            }
            return 5;
        }

        public virtual void SetupTemplateButtonNavigation(bool hasDisabledButton)
        {
            // Assumes buttons are active (since uses GetComponent), so call after activating panel.
            if (instantiatedButtons == null || instantiatedButtons.Count == 0) return;
            var buttons = new List<GameObject>();
            if (hasDisabledButton)
            {
                // If some buttons are disabled, make a list of only the clickable ones:
                buttons.AddRange(instantiatedButtons.FindAll(x => x.GetComponent<StandardUIResponseButton>().isClickable));
            }
            else
            {
                buttons.AddRange(instantiatedButtons);
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i].GetComponent<UnityEngine.UI.Button>();
                if (button == null) continue;
                var above = (i == 0) ? (loopExplicitNavigation ? buttons[buttons.Count - 1].GetComponent<UnityEngine.UI.Button>() : null)
                    : buttons[i - 1].GetComponent<UnityEngine.UI.Button>();
                var below = (i == buttons.Count - 1) ? (loopExplicitNavigation ? buttons[0].GetComponent<UnityEngine.UI.Button>() : null)
                    : buttons[i + 1].GetComponent<UnityEngine.UI.Button>();
                var navigation = new UnityEngine.UI.Navigation();

                navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                navigation.selectOnUp = above;
                navigation.selectOnLeft = above;
                navigation.selectOnDown = below;
                navigation.selectOnRight = below;
                button.navigation = navigation;
            }
        }

        protected virtual GameObject InstantiateButton()
        {
            // Try to pull from pool first:
            if (m_instantiatedButtonPool.Count > 0)
            {
                var button = m_instantiatedButtonPool[0];
                m_instantiatedButtonPool.RemoveAt(0);
                return button;
            }
            else
            {
                return GameObject.Instantiate(buttonTemplate.gameObject) as GameObject;
            }
        }

        public void DestroyInstantiatedButtons()
        {
            // Return buttons to pool:
            for (int i = 0; i < instantiatedButtons.Count; i++)
            {
                instantiatedButtons[i].SetActive(false);
            }
            m_instantiatedButtonPool.AddRange(instantiatedButtons);

            instantiatedButtons.Clear();
            NotifyContentChanged();
        }

        /// <summary>
        /// Makes the panel's buttons non-clickable.
        /// Typically called by the dialogue UI as soon as a button has been
        /// clicked to make sure the player can't click another one while the
        /// menu is playing its hide animation.
        /// </summary>
        public virtual void MakeButtonsNonclickable()
        {
            for (int i = 0; i < instantiatedButtons.Count; i++)
            {
                var responseButton = (instantiatedButtons[i] != null) ? instantiatedButtons[i].GetComponent<StandardUIResponseButton>() : null;
                if (responseButton != null) responseButton.isClickable = false;
            }
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null) buttons[i].isClickable = false;
            }
        }

        protected void NotifyContentChanged()
        {
            onContentChanged.Invoke();
        }

        protected void DisableInput()
        {
            SetInput(false);
        }

        protected void EnableInput()
        {
            SetInput(true);
        }

        protected void SetInput(bool value)
        {
            s_isInputDisabled = (value == false);
            if (m_mainCanvasGroup == null)
            {
                // Try to get dialogue UI's main panel:
                var ui = GetComponentInParent<StandardDialogueUI>();
                if (ui != null && ui.conversationUIElements.mainPanel != null)
                {
                    var mainPanel = ui.conversationUIElements.mainPanel;
                    m_mainCanvasGroup = mainPanel.GetComponent<CanvasGroup>() ?? mainPanel.gameObject.AddComponent<CanvasGroup>();
                }
                else
                {
                    // Otherwise try the menu's panel:
                    var menuPanel = panel;
                    if (menuPanel == null) menuPanel = buttonTemplateHolder;
                    if (menuPanel != null)
                    {
                        m_mainCanvasGroup = menuPanel.GetComponent<CanvasGroup>() ?? menuPanel.gameObject.AddComponent<CanvasGroup>();
                    }
                }
            }
            if (m_mainCanvasGroup != null) m_mainCanvasGroup.interactable = value;
            if (value == false)
            {
                // If auto focus, show firstSelected in selected state:
                if (InputDeviceManager.autoFocus && firstSelected != null)
                {
                    var button = firstSelected.GetComponent<UnityEngine.UI.Button>();
                    MethodInfo methodInfo = typeof(UnityEngine.UI.Button).GetMethod("DoStateTransition", BindingFlags.Instance | BindingFlags.NonPublic);
                    methodInfo.Invoke(button, new object[] { 3, true }); // 3 = SelectionState.Selected
                }
            }
            if (eventSystem != null)
            {
                var inputModule = eventSystem.GetComponent<UnityEngine.EventSystems.PointerInputModule>();
                if (inputModule != null) inputModule.enabled = value;
            }
            UIButtonKeyTrigger.monitorInput = value;
            if (value == true)
            {
                RefreshSelectablesList();
                CheckFocus();
                if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
                { // Also show in focused/selected state:
                    UIUtility.Select(eventSystem.currentSelectedGameObject.GetComponent<UnityEngine.UI.Selectable>());
                }
            }
        }
        #endregion

        #region Timer

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <param name='timeout'>Timeout duration in seconds.</param>
        /// <param name="timeoutHandler">Invoke this handler on timeout.</param>
        public virtual void StartTimer(float timeout, System.Action timeoutHandler)
        {
            if (m_timer == null)
            {
                if (timerSlider != null)
                {
                    Tools.SetGameObjectActive(timerSlider, true);
                    m_timer = timerSlider.GetComponent<StandardUITimer>();
                    if (m_timer == null) m_timer = timerSlider.gameObject.AddComponent<StandardUITimer>();
                }
                else
                {
                    m_timer = GetComponentInChildren<StandardUITimer>();
                    if (m_timer == null) m_timer = gameObject.AddComponent<StandardUITimer>();
                }
            }
            Tools.SetGameObjectActive(m_timer, true);
            m_timer.StartCountdown(timeout, timeoutHandler);
        }

        public virtual void StopTimer()
        {
            if (m_timer != null)
            {
                m_timer.StopCountdown();
                Tools.SetGameObjectActive(m_timer, false);
            }
        }

        #endregion

    }

}
