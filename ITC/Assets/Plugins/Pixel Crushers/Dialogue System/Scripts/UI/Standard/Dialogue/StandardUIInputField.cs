// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// StandardDialogueUI input field implementation.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class StandardUIInputField : UIPanel, ITextFieldUI
    {

        [Tooltip("（可选）文本字段面板。")]
        public UnityEngine.UI.Graphic panel;

        [Tooltip("（可选）提示文本元素。")]
        public UITextField label;

        [Tooltip("输入字段。")]
        public UIInputField inputField;

        [Tooltip("（可选）接受用户文本输入的按键代码。")]
        public KeyCode acceptKey = KeyCode.Return;

        [Tooltip("（可选）接受用户文本输入的输入按钮。")]
        public string acceptButton = string.Empty;

        [Tooltip("（可选）取消用户文本输入的按键代码。")]
        public KeyCode cancelKey = KeyCode.Escape;

        [Tooltip("（可选）取消用户文本输入的输入按钮。")]
        public string cancelButton = string.Empty;

        [Tooltip("自动打开触摸屏键盘。")]
        public bool showTouchScreenKeyboard = false;

        [Tooltip("允许空白文本输入。")]
        public bool allowBlankInput = true;

        public UnityEvent onAccept = new UnityEvent();

        public UnityEvent onCancel = new UnityEvent();

        /// <summary>
        /// Call this delegate when the player accepts the input in the text field.
        /// </summary>
        protected AcceptedTextDelegate m_acceptedText = null;

        protected bool m_isAwaitingInput = false;

        protected TouchScreenKeyboard m_touchScreenKeyboard = null;

        protected bool m_isQuitting = false;

        protected virtual void OnApplicationQuit()
        {
            m_isQuitting = true;
        }

        protected override void Start()
        {
            if (DialogueDebug.logWarnings && (inputField == null)) Debug.LogWarning("Dialogue System: No InputField is assigned to the text field UI " + name + ". TextInput() sequencer commands or [var?=] won't work.");
            SetActive(false);
        }

        /// <summary>
        /// Starts the text input field.
        /// </summary>
        /// <param name="labelText">The label text.</param>
        /// <param name="text">The current value to use for the input field.</param>
        /// <param name="maxLength">Max length, or <c>0</c> for unlimited.</param>
        /// <param name="acceptedText">The delegate to call when accepting text.</param>
        public virtual void StartTextInput(string labelText, string text, int maxLength, AcceptedTextDelegate acceptedText)
        {
            if (label != null)
            {
                label.text = labelText;
            }
            if (inputField != null)
            {
                inputField.text = text;
                inputField.characterLimit = maxLength;
            }
            m_acceptedText = acceptedText;
            m_isAwaitingInput = true;
            Show();
        }

        protected override void Update()
        {
            if (m_isAwaitingInput && !DialogueManager.IsDialogueSystemInputDisabled())
            {
                if (InputDeviceManager.IsKeyDown(acceptKey) || InputDeviceManager.IsButtonDown(acceptButton) ||
                    IsTouchScreenDone())
                {
                    AcceptTextInput();
                }
                else if (InputDeviceManager.IsKeyDown(cancelKey) || InputDeviceManager.IsButtonDown(cancelButton) ||
                    IsTouchScreenCancelled())
                {
                    CancelTextInput();
                }
            }
        }

        protected virtual bool IsTouchScreenDone()
        {
            if (m_touchScreenKeyboard == null) return false;
            try
            {
                return m_touchScreenKeyboard.status == TouchScreenKeyboard.Status.Done;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        protected virtual bool IsTouchScreenCancelled()
        {
            if (m_touchScreenKeyboard == null) return false;
            try
            {
                return m_touchScreenKeyboard.status == TouchScreenKeyboard.Status.Canceled;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        protected virtual bool IsTouchScreenCanceled()
        {
            if (m_touchScreenKeyboard == null) return false;
            try
            {
                return m_touchScreenKeyboard.status == TouchScreenKeyboard.Status.Canceled;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Cancels the text input field.
        /// </summary>
        public virtual void CancelTextInput()
        {
            m_isAwaitingInput = false;
            Hide();
            onCancel.Invoke();
        }

        /// <summary>
        /// Accepts the text input and calls the accept handler delegate.
        /// </summary>
        public virtual void AcceptTextInput()
        {
            if (!CanAcceptInput()) return;
            m_isAwaitingInput = false;
            if (m_acceptedText != null)
            {
                if (inputField != null) m_acceptedText(inputField.text);
                m_acceptedText = null;
            }
            Hide();
            onAccept.Invoke();
        }

        protected virtual bool CanAcceptInput()
        {
            return allowBlankInput || !string.IsNullOrWhiteSpace(inputField.text);
        }

        protected virtual void Show()
        {
            SetActive(true);
            Open();
            if (showTouchScreenKeyboard) ShowTouchScreenKeyboard();
            if (inputField != null)
            {
                inputField.ActivateInputField();
                if (eventSystem != null)
                {
                    eventSystem.SetSelectedGameObject(inputField.gameObject);
                }
            }
        }

        protected virtual void ShowTouchScreenKeyboard()
        {
            m_touchScreenKeyboard = TouchScreenKeyboard.Open(inputField.text);
        }

        protected virtual void Hide()
        {
            if (m_isQuitting) return;
            Close();
            SetActive(false);
            if (m_touchScreenKeyboard != null)
            {
                try
                {
                    m_touchScreenKeyboard.active = false;
                }
                catch (System.Exception) { }
                m_touchScreenKeyboard = null;
            }
        }

        protected virtual void SetActive(bool value)
        {
            if (panel != null) panel.gameObject.SetActive(value);
            if (panel == null || value == true)
            {
                if (label != null) label.SetActive(value);
                if (inputField != null) inputField.SetActive(value);
            }
        }

    }

}
