// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This script replaces the normal continue button functionality with
    /// a two-stage process. If the typewriter effect is still playing, it
    /// simply stops the effect. Otherwise it sends OnContinue to the UI.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class StandardUIContinueButtonFastForward : MonoBehaviour
    {

        [Tooltip("继续按钮所作用的 Dialogue UI。")]
        public StandardDialogueUI dialogueUI;

        [Tooltip("如果打字机效果尚未播放完，则将其快进。")]
        public AbstractTypewriterEffect typewriterEffect;

#if USE_STM
        [Tooltip("若使用 SuperTextMesh，请改用此项，而不是 typewriter effect。")]
        public SuperTextMesh superTextMesh;
#endif

        [Tooltip("继续时隐藏继续按钮。")]
        public bool hideContinueButtonOnContinue = false;

        [Tooltip("如果正在显示 subtitle，则继续跳过它。")]
        public bool continueSubtitlePanel = true;

        [Tooltip("如果正在显示 alert，则继续跳过它。")]
        public bool continueAlertPanel = true;

        protected UnityEngine.UI.Button continueButton;

        protected virtual AbstractDialogueUI runtimeDialogueUI
        {
            get
            {
                if (dialogueUI != null) return dialogueUI;
                var panel = GetComponentInParent<StandardUISubtitlePanel>();
                if (panel != null) return panel.dialogueUI;
                else return GetComponentInParent<AbstractDialogueUI>() ?? DialogueManager.dialogueUI as AbstractDialogueUI;
            }
        }

        public virtual void Awake()
        {
            if (typewriterEffect == null)
            {
                typewriterEffect = GetComponentInChildren<UnityUITypewriterEffect>();
            }
            continueButton = GetComponent<UnityEngine.UI.Button>();
        }

        public virtual void OnFastForward()
        {
            if ((typewriterEffect != null) && typewriterEffect.isPlaying)
            {
                typewriterEffect.Stop();
            }
#if USE_STM
            else if (superTextMesh != null && superTextMesh.reading)
            {
                superTextMesh.SkipToEnd();
            }
#endif
            else
            {
                if (hideContinueButtonOnContinue && continueButton != null)
                {
                    continueButton.gameObject.SetActive(false);
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                }
                if (runtimeDialogueUI != null)
                {
                    if (continueSubtitlePanel && continueAlertPanel) runtimeDialogueUI.OnContinue();
                    else if (continueSubtitlePanel) runtimeDialogueUI.OnContinueConversation();
                    else if (continueAlertPanel) runtimeDialogueUI.OnContinueAlert();
                }
            }
        }

    }

}
