// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Controls for StandardDialogueUI's alert message.
    /// </summary>
    [Serializable]
    public class StandardUIAlertControls : AbstractUIAlertControls
    {

        [Tooltip("主提示面板（可选）。")]
        public UIPanel panel;

        [Tooltip("提示文本。")]
        public UITextField alertText;

        [Tooltip("在显示新提示前等待上一个提示结束；如果未勾选，新提示会替换旧提示。")]
        public bool queueAlerts = false;

        [Tooltip("如果已有消息排队显示，则不要再排队新的消息。")]
        public bool dontQueueDuplicates = false;

        [Tooltip("在显示下一个排队提示前，等待上一个提示的 Hide 动画结束。")]
        public bool waitForHideAnimation = false;

        [Tooltip("如果消息包含 [f]，则立即显示而不是排队。")]
        public bool allowForceImmediate = false;

        /// <summary>
        /// Is an alert currently showing?
        /// </summary>
        public override bool isVisible { get { return (panel != null) ? panel.isOpen : (alertText != null && alertText.activeInHierarchy); } }

        /// <summary>
        /// Is the panel currently playing the Hide animation?
        /// </summary>
        public bool isHiding { get { return (panel != null && string.Equals(panel.animatorMonitor.currentTrigger, panel.hideAnimationTrigger)); } }

        private bool m_initializedAnimator = false;

        /// <summary>
        /// Sets the alert controls active.
        /// </summary>
        public override void SetActive(bool value)
        {
            if (panel != null)
            {
                if (!m_initializedAnimator && value == false)
                {
                    if (panel.deactivateOnHidden)
                    {
                        panel.gameObject.SetActive(false);
                    }
                }
                else
                {
                    panel.SetOpen(value);
                }
            }
            m_initializedAnimator = true;
            if (value == true || panel == null) alertText.SetActive(true);
        }

        /// <summary>
        /// Hide without playing animation.
        /// </summary>
        public void HideImmediate()
        {
            alertText.SetActive(false);
        }

        /// <summary>
        /// Sets the alert message UI Text.
        /// </summary>
        /// <param name='message'>Alert message.</param>
        /// <param name='duration'>Duration to show message.</param>
        public override void SetMessage(string message, float duration)
        {
            alertText.text = FormattedText.Parse(message).text;
        }

    }

}
