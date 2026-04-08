// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Controls for UnityUIDialogueUI's alert message.
    /// </summary>
    [System.Serializable]
    public class UnityUIAlertControls : AbstractUIAlertControls
    {

        /// <summary>
        /// The panel containing the alert controls. A panel is optional, but you may want one
        /// so you can include a background image, panel-wide effects, etc.
        /// </summary>
        [Tooltip("可选面板，包含 alert line；也可以包含其他装饰和效果。")]
        public UnityEngine.UI.Graphic panel;

        /// <summary>
        /// The label used to show the alert message text.
        /// </summary>
        [Tooltip("显示 alert message 文本。")]
        public UnityEngine.UI.Text line;

        /// <summary>
        /// Optional continue button to close the alert immediately.
        /// </summary>
        [Tooltip("可选的 continue button；将 OnClick 配置为调用 dialogue UI 的 OnContinue 方法。")]
        public UnityEngine.UI.Button continueButton;

        [Tooltip("先等待前一个 alert 结束，再显示新 alert；如果未勾选，新 alert 会替换旧 alert。")]
        public bool queueAlerts = false;

        [Tooltip("等待前一个 alert 的 Hide 动画完成后，再显示下一个排队的 alert。")]
        public bool waitForHideAnimation = false;

        [Tooltip("可选的动画过渡；面板应包含 Animator。")]
        public UIAnimationTransitions animationTransitions = new UIAnimationTransitions();

        /// <summary>
        /// Is an alert currently showing?
        /// </summary>
        /// <value>
        /// <c>true</c> if showing; otherwise, <c>false</c>.
        /// </value>
        public override bool isVisible { get { return showHideController.state != UIShowHideController.State.Hidden; } }

        public bool IsHiding { get { return showHideController.state == UIShowHideController.State.Hiding; } }

        private UIShowHideController m_showHideController = null;
        private UIShowHideController showHideController
        {
            get
            {
                if (m_showHideController == null) m_showHideController = new UIShowHideController(null, panel, animationTransitions.transitionMode, animationTransitions.debug);
                return m_showHideController;
            }
        }

        /// <summary>
        /// Sets the alert controls active. If a hide animation is available, this method
        /// depends on the hide animation to hide the controls.
        /// </summary>
        /// <param name='value'>
        /// <c>true</c> for active.
        /// </param>
        public override void SetActive(bool value)
        {
            if (value == true)
            {
                if (showHideController.state != UIShowHideController.State.Showing) ShowPanel();
            }
            else
            {
                if (showHideController.state != UIShowHideController.State.Hiding) HidePanel();
            }
        }

        private void ShowPanel()
        {
            ActivateUIElements();
            animationTransitions.ClearTriggers(showHideController);
            showHideController.Show(animationTransitions.showTrigger, false, null);
        }

        private void HidePanel()
        {
            animationTransitions.ClearTriggers(showHideController);
            showHideController.Hide(animationTransitions.hideTrigger, DeactivateUIElements);
        }

        public void ActivateUIElements()
        {
            Tools.SetGameObjectActive(panel, true);
            Tools.SetGameObjectActive(line, true);
        }

        public void DeactivateUIElements()
        {
            Tools.SetGameObjectActive(panel, false);
            Tools.SetGameObjectActive(line, false);
        }

        /// <summary>
        /// Sets the alert message UI Text.
        /// </summary>
        /// <param name='message'>
        /// Alert message.
        /// </param>
        /// <param name='duration'>
        /// Duration to show message.
        /// </param>
        public override void SetMessage(string message, float duration)
        {
            if (line != null) line.text = FormattedText.Parse(message, DialogueManager.masterDatabase.emphasisSettings).text;
        }

        /// <summary>
        /// Auto-focuses the continue button. Useful for gamepads.
        /// </summary>
        public void AutoFocus(bool allowStealFocus = true)
        {
            UITools.Select(continueButton, allowStealFocus);
        }

    }

}
