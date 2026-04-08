#if UNITY_2021_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PixelCrushers.DialogueSystem.UIToolkit
{

    /// <summary>
    /// Manages a response menu for UIToolkitDialogueUI.
    /// </summary>
    [Serializable]
    public class UIToolkitResponseMenuElements : AbstractUIResponseMenuControls
    {
        // [TODO] Options to consider: Autonumber

        [Tooltip("Response Menu 的容器面板。")]
        [SerializeField] private string responseMenuPanelName;
        [Tooltip("可选计时器的进度条。取值范围应为 0-1。")]
        [SerializeField] private string timerProgressBarName;
        [Tooltip("可选的玩家 portrait 名称。")]
        [SerializeField] private string portraitLabelName;
        [Tooltip("可选的玩家 portrait image。")]
        [SerializeField] private string portraitImageName;
        [Tooltip("所有可用 response button 的列表。dialogue UI 会使用这些按钮填充菜单。")]
        [SerializeField] private List<string> responseButtonNames;

        protected UIDocument Document { get; set; }
        public override AbstractUISubtitleControls subtitleReminderControls => null;
        protected VisualElement ResponseMenuPanel => UIToolkitDialogueUI.GetVisualElement<VisualElement>(Document, responseMenuPanelName);
        protected ProgressBar TimerProgressBar => UIToolkitDialogueUI.GetVisualElement<ProgressBar>(Document, timerProgressBarName);
        protected Label PortraitLabel => UIToolkitDialogueUI.GetVisualElement<Label>(Document, portraitLabelName);
        protected VisualElement PortraitImage => UIToolkitDialogueUI.GetVisualElement<VisualElement>(Document, portraitImageName);
        protected virtual Button GetResponseButton(int index) => UIToolkitDialogueUI.GetVisualElement<Button>(Document, responseButtonNames[index]);

        protected float TimerSecondsMax { get; set; }
        protected float TimerSecondsLeft { get;set; }
        protected System.Action<object> ClickedResponseAction { get; set; }

        protected Dictionary<int, Response> ResponsesByButtonIndex = new Dictionary<int, Response>();

        public virtual void Initialize(UIDocument document, System.Action<object> clickedResponseAction)
        {
            Document = document;
            ClickedResponseAction = clickedResponseAction;
            UIToolkitDialogueUI.SetDisplay(ResponseMenuPanel, false);
            for (int i = 0; i < responseButtonNames.Count; i++)
            {
                var index = i;
                GetResponseButton(i).clicked += () => OnClickResponse(index);
            }
        }

        public virtual void DoUpdate()
        {
            UpdateTimer();
        }

        public override void SetActive(bool value)
        {
            UIToolkitDialogueUI.SetDisplay(ResponseMenuPanel, value);
            UIToolkitDialogueUI.SetDisplay(TimerProgressBar, false);
        }

        public override void SetPCPortrait(Sprite sprite, string portraitName)
        {
            if (PortraitLabel != null) PortraitLabel.text = portraitName;
            if (PortraitImage != null)
            {
                var hasSprite = sprite != null;
                UIToolkitDialogueUI.SetDisplay(PortraitImage, hasSprite);
                if (hasSprite) PortraitImage.style.backgroundImage = new StyleBackground(sprite);
            }
        }

        protected override void ClearResponseButtons()
        {
            ResponsesByButtonIndex.Clear();
            for (int i = 0; i < responseButtonNames.Count; i++)
            {
                UIToolkitDialogueUI.SetDisplay(GetResponseButton(i), false);
            }
        }

        public override void ShowResponses(Subtitle subtitle, Response[] responses, Transform target)
        {
            if ((responses != null) && (responses.Length > 0))
            {
                ClearResponseButtons();
                SetResponseButtons(responses, target);
                Show();
            }
            else
            {
                Hide();
            }
        }

        protected override void SetResponseButtons(Response[] responses, Transform target)
        {
            var maxResponses = Mathf.Min(responses.Length, responseButtonNames.Count);
            int numUnusedButtons = responseButtonNames.Count - maxResponses;

            // Fill in buttons using specified positions & alignment:
            var needToFocusAButton = InputDeviceManager.autoFocus;
            for (int i = 0; i < responses.Length; i++)
            {
                var response = responses[i];
                var index = (response.formattedText.position != FormattedText.NoAssignedPosition)
                    ? response.formattedText.position
                    : (buttonAlignment == ResponseButtonAlignment.ToFirst)
                        ? i
                        : numUnusedButtons + i;

                ResponsesByButtonIndex[index] = response;
                var button = GetResponseButton(index);
                if (button == null) continue;
                button.text = response.formattedText.text;
                button.SetEnabled(response.enabled);
                var focusThisButton = false;
                if (needToFocusAButton && response.enabled)
                {
                    focusThisButton = true;
                    needToFocusAButton = false;
                }
                UIToolkitDialogueUI.SetDisplay(button, true, focusThisButton);
            }

            // If specified, show unused buttons with no text:
            if (showUnusedButtons)
            {
                var firstUnusedIndex = (buttonAlignment == ResponseButtonAlignment.ToFirst) ? maxResponses : 0;
                for (int i = firstUnusedIndex; i < (firstUnusedIndex + numUnusedButtons); i++)
                {
                    var button = GetResponseButton(i);
                    if (button == null) continue;
                    button.text = string.Empty;
                    UIToolkitDialogueUI.SetDisplay(button, true);
                }
            }
        }

        protected virtual void OnClickResponse(int index)
        {
            if (ResponsesByButtonIndex.TryGetValue(index, out var response))
            {
                Hide();
                ClickedResponseAction(response);
            }
        }

        public override void StartTimer(float timeout)
        {
            if (TimerProgressBar == null) return;
            UIToolkitDialogueUI.SetDisplay(TimerProgressBar, true);
            TimerSecondsLeft = TimerSecondsMax = timeout;
            TimerProgressBar.value = 1;
        }

        protected virtual void UpdateTimer()
        {
            if (TimerSecondsMax <= 0) return;
            TimerSecondsLeft -= DialogueTime.deltaTime;
            TimerProgressBar.value = Mathf.Clamp01(TimerSecondsLeft / TimerSecondsMax);
            Debug.Log($"{TimerSecondsLeft} --> {TimerProgressBar.value}");

            if (TimerSecondsLeft <= 0)
            {
                TimerSecondsMax = 0;
                OnTimedOut();
            }
        }

        private void OnTimedOut()
        {
            DialogueManager.instance.SendMessage(DialogueSystemMessages.OnConversationTimeout);
        }

    }

}
#endif
