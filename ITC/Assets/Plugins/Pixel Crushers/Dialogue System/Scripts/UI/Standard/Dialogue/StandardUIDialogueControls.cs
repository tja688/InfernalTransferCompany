// Copyright (c) Pixel Crushers. All rights reserved.

using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Contains all dialogue (conversation) controls for a Standard Dialogue UI.
    /// </summary>
    [Serializable]
    public class StandardUIDialogueControls : AbstractDialogueUIControls
    {

        #region Serialized Variables

        [Tooltip("对话 UI 的主面板（可选）。")]
        public UIPanel mainPanel;

        [Tooltip("不要停用 Main Panel。若已指定，仍会播放 show 和 hide 动画。")]
        public bool dontDeactivateMainPanel = false;

        [Tooltip("开始对话时，等待主面板打开后再显示字幕或菜单。")]
        public bool waitForMainPanelOpen = false;

        public StandardUISubtitlePanel[] subtitlePanels;

        [Tooltip("NPC 字幕的默认面板。")]
        public StandardUISubtitlePanel defaultNPCSubtitlePanel;

        [Tooltip("PC 字幕的默认面板。")]
        public StandardUISubtitlePanel defaultPCSubtitlePanel;

        [Tooltip("检查是否存在已配置为在对话开始时立即打开的字幕面板。取消勾选可跳过检查。")]
        public bool allowOpenSubtitlePanelsOnStartConversation = true;

        [Tooltip("允许 Dialogue Actor 组件使用自定义字幕和菜单面板。")]
        public bool allowDialogueActorCustomPanels = true;

        public StandardUIMenuPanel[] menuPanels;

        [Tooltip("Response Menu 的默认面板。")]
        public StandardUIMenuPanel defaultMenuPanel;

        [Tooltip("显示 Response Menu 时，使用分配给第一条回应的玩家角色的肖像信息。如果使用多个菜单面板，也使用该角色的菜单面板。")]
        public bool useFirstResponseForMenuPortrait;

        [Tooltip("关闭时，等待所有字幕面板和菜单面板关闭。")]
        public bool waitForClose = true;

        #endregion

        #region Private Variables

        private StandardUISubtitleControls m_standardSubtitleControls = new StandardUISubtitleControls();
        public StandardUISubtitleControls standardSubtitleControls { get { return m_standardSubtitleControls; } }
        public override AbstractUISubtitleControls npcSubtitleControls { get { return m_standardSubtitleControls; } }
        public override AbstractUISubtitleControls pcSubtitleControls { get { return m_standardSubtitleControls; } }
        private StandardUIResponseMenuControls m_standardMenuControls = new StandardUIResponseMenuControls();
        public StandardUIResponseMenuControls standardMenuControls { get { return m_standardMenuControls; } }
        public override AbstractUIResponseMenuControls responseMenuControls { get { return m_standardMenuControls; } }
        public StandardDialogueUI dialogueUI { get; private set; }

        private bool m_initializedAnimator = false;
        private Coroutine closeCoroutine = null;

        #endregion

        #region Initialization

        public void Initialize(StandardDialogueUI dialogueUI)
        {
            this.dialogueUI = dialogueUI;
            m_standardSubtitleControls.Initialize(subtitlePanels, defaultNPCSubtitlePanel, defaultPCSubtitlePanel, dialogueUI);
            m_standardMenuControls.Initialize(menuPanels, defaultMenuPanel, useFirstResponseForMenuPortrait);
            m_standardSubtitleControls.allowDialogueActorCustomPanels = allowDialogueActorCustomPanels;
            m_standardMenuControls.allowDialogueActorCustomPanels = allowDialogueActorCustomPanels;
        }

        public void SetDialogueUI(StandardDialogueUI dialogueUI)
        {
            this.dialogueUI = dialogueUI;
            m_standardSubtitleControls.SetDialogueUI(dialogueUI);
        }

        #endregion

        #region Show & Hide Main Panel

        public override void SetActive(bool value)
        {
            if (value == true) ShowPanel(); else HidePanel();
        }

        public override void ShowPanel()
        {
            if (closeCoroutine != null)
            {
                if (mainPanel != null) mainPanel.StopCoroutine(closeCoroutine);
                closeCoroutine = null;
            }
            m_initializedAnimator = true;
            if (mainPanel != null) mainPanel.Open();
            standardSubtitleControls.ApplyQueuedActorPanelCache();
        }

        private void HidePanel()
        {
            if (!m_initializedAnimator || (mainPanel != null && !mainPanel.gameObject.activeSelf))
            {
                HideImmediate();
                m_initializedAnimator = true;
            }
            else
            {
                standardSubtitleControls.Close();
                standardMenuControls.Close();
                if (mainPanel != null && !dontDeactivateMainPanel)
                {
                    if (waitForClose)
                    {
                        closeCoroutine = mainPanel.StartCoroutine(CloseAfterPanelsAreClosed());
                    }
                    else
                    {
                        mainPanel.Close();
                    }
                }
            }
        }

        public void ClosePanels()
        {
            standardSubtitleControls.Close();
            standardMenuControls.Close();
        }

        private IEnumerator CloseAfterPanelsAreClosed()
        {
            while (AreAnyPanelsClosing(null))
            {
                yield return null;
            }
            mainPanel.Close();
        }

        // extraSubtitlePanel may be a custom (e.g., bubble) panel that isn't part of the dialogue UI's regular list.
        public bool AreAnyPanelsClosing(StandardUISubtitlePanel extraSubtitlePanel = null)
        {
            if (extraSubtitlePanel != null && extraSubtitlePanel.panelState == UIPanel.PanelState.Closing) return true;
            if (standardSubtitleControls.AreAnyPanelsClosing()) return true;
            if (standardMenuControls.AreAnyPanelsClosing()) return true;
            if (mainPanel != null && mainPanel.panelState == UIPanel.PanelState.Closing) return true;
            return false;
        }

        public void HideImmediate()
        {
            HideSubtitlePanelsImmediate();
            HideMenuPanelsImmediate();
            if (mainPanel != null && !dontDeactivateMainPanel)
            {
                mainPanel.gameObject.SetActive(false);
                mainPanel.panelState = UIPanel.PanelState.Closed;
            }
        }

        private void HideSubtitlePanelsImmediate()
        {
            for (int i = 0; i < subtitlePanels.Length; i++)
            {
                var subtitlePanel = subtitlePanels[i];
                if (subtitlePanel != null) subtitlePanel.HideImmediate();
            }
        }

        private void HideMenuPanelsImmediate()
        {
            for (int i = 0; i < menuPanels.Length; i++)
            {
                var menuPanel = menuPanels[i];
                if (menuPanel != null) menuPanel.HideImmediate();
            }
        }

        public void OpenSubtitlePanelsOnStart(StandardDialogueUI ui)
        {
            if (allowOpenSubtitlePanelsOnStartConversation) standardSubtitleControls.OpenSubtitlePanelsOnStartConversation(ui);
        }

        public void ClearCaches()
        {
            standardSubtitleControls.ClearCache();
            standardMenuControls.ClearCache();
        }

        public virtual void ClearAllSubtitleText()
        {
            // Clear all built-in panels:
            for (int i = 0; i < subtitlePanels.Length; i++)
            {
                if (subtitlePanels[i] == null) continue;
                subtitlePanels[i].ClearText();
            }

            // Clear any custom panels:
            standardSubtitleControls.ClearSubtitlesOnCustomPanels();
        }

        public virtual void ClearSubtitleTextOnConversationStart()
        {
            // Clear all built-in panels:
            for (int i = 0; i < subtitlePanels.Length; i++)
            {
                if (subtitlePanels[i] == null) continue;
                if (subtitlePanels[i].clearTextOnConversationStart) subtitlePanels[i].ClearText();
            }
        }

        #endregion

    }
}
