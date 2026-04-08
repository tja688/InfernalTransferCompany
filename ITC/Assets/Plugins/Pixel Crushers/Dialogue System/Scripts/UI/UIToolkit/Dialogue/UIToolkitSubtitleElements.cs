#if UNITY_2021_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PixelCrushers.DialogueSystem.UIToolkit
{

    /// <summary>
    /// Manages a subtitle panel for UIToolkitDialogueUI.
    /// </summary>
    [Serializable]
    public class UIToolkitSubtitleElements : AbstractUISubtitleControls
    {

        [Tooltip("subtitle 的容器面板。")]
        [SerializeField] private string subtitlePanelName;
        [Tooltip("字幕文本。")]
        [SerializeField] private string subtitleLabelName;
        [Tooltip("可选的说话者 portrait 名称。")]
        [SerializeField] private string portraitLabelName;
        [Tooltip("可选的说话者 portrait image。")]
        [SerializeField] private string portraitImageName;
        [Tooltip("用于推进对话的 continue button（如果模式要求点击 continue button）。")]
        [SerializeField] private string continueButtonName;
        [Tooltip("指定面板何时显示/隐藏。")]
        [SerializeField] private UIVisibility visibility;

        public bool IsSamePanel(UIToolkitSubtitleElements panel) => panel.subtitlePanelName == this.subtitlePanelName;
        public string SubtitlePanelName => subtitlePanelName;
        public UIVisibility Visibility => visibility;

        protected UIDocument Document { get; set; }
        protected VisualElement SubtitlePanel => UIToolkitDialogueUI.GetVisualElement<VisualElement>(Document, subtitlePanelName);
        protected Label SubtitleLabel => UIToolkitDialogueUI.GetVisualElement<Label>(Document, subtitleLabelName);
        protected Label PortraitLabel => UIToolkitDialogueUI.GetVisualElement<Label>(Document, portraitLabelName);
        protected VisualElement PortraitImage => UIToolkitDialogueUI.GetVisualElement<VisualElement>(Document, portraitImageName);
        protected Button ContinueButton => UIToolkitDialogueUI.GetVisualElement<Button>(Document, continueButtonName);

        public bool ShouldStayVisible => Visibility == UIVisibility.AlwaysFromStart || Visibility == UIVisibility.AlwaysOnceShown;

        public override bool hasText => !string.IsNullOrEmpty(SubtitleLabel.text);

        public virtual void Initialize(UIDocument document, System.Action clickedContinueAction)
        {
            Document = document;
            if (ContinueButton != null) ContinueButton.clicked += clickedContinueAction;
        }

        public override void SetActive(bool value)
        {
            UIToolkitDialogueUI.SetDisplay(SubtitlePanel, value);
            HideContinueButton();
        }

        public virtual void OpenOnStartConversation(Sprite portraitSprite, string portraitName, DialogueActor dialogueActor)
        {
            SetActive(true);
            var actorName = portraitName;
            var actorSprite = portraitSprite;
            if (dialogueActor != null)
            {
                actorName = dialogueActor.GetActorName();
                var dialogueActorSprite = dialogueActor.GetPortraitSprite();
                if (dialogueActorSprite != null) actorSprite = dialogueActorSprite;
            }
            SetActorPortraitSprite(actorName, actorSprite);
            if (SubtitleLabel != null) SubtitleLabel.text = string.Empty;
        }

        public override void ClearSubtitle()
        {
            if (SubtitleLabel != null) SubtitleLabel.text = string.Empty;
            HideContinueButton();
        }

        public override void SetSubtitle(Subtitle subtitle)
        {
            SetActive(true);
            if (SubtitleLabel != null) SubtitleLabel.text = subtitle.formattedText.text;
            SetActorPortraitSprite(subtitle.speakerInfo.Name, subtitle.GetSpeakerPortrait());
        }

        public override void SetActorPortraitSprite(string actorName, Sprite sprite)
        {
            if (PortraitLabel != null) PortraitLabel.text = actorName;
            if (PortraitImage != null)
            {
                var hasSprite = sprite != null;
                UIToolkitDialogueUI.SetDisplay(PortraitImage, hasSprite);
                if (hasSprite) PortraitImage.style.backgroundImage = new StyleBackground(sprite);
            }
        }

        public override void ShowContinueButton() => UIToolkitDialogueUI.SetDisplay(ContinueButton, true, InputDeviceManager.autoFocus);
        public override void HideContinueButton() => UIToolkitDialogueUI.SetDisplay(ContinueButton, false);

    }

}
#endif
