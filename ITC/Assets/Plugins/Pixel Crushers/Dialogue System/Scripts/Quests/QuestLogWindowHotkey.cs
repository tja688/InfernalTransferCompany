// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Allows toggling of the quest log window using a key or button,
    /// or by calling ToggleQuestLogWindow.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class QuestLogWindowHotkey : MonoBehaviour
    {

        [Tooltip("按下此按键时切换任务日志窗口。")]
        public KeyCode key = KeyCode.J;

        [Tooltip("按下此输入按钮时切换任务日志窗口。")]
        public string buttonName = string.Empty;

        [Tooltip("（可选）使用此任务日志窗口。未分配时，会自动在场景中查找任务日志窗口。如果要指定窗口，请指定场景实例，而不是未实例化的 prefab。")]
        public QuestLogWindow questLogWindow;

        public QuestLogWindow runtimeQuestLogWindow
        {
            get
            {
                if (questLogWindow == null) questLogWindow = PixelCrushers.GameObjectUtility.FindFirstObjectByType<QuestLogWindow>();
                return questLogWindow;
            }
        }

        private void Awake()
        {
            if (questLogWindow == null) questLogWindow = PixelCrushers.GameObjectUtility.FindFirstObjectByType<QuestLogWindow>();
        }

#if USE_NEW_INPUT

        public UnityEngine.InputSystem.InputActionReference inputAction;

        protected virtual void OnEnable()
        {
            if (inputAction != null)
            {
                inputAction.action.Enable();
                inputAction.action.performed += OnInputActionPerformed;
            }
        }

        protected virtual void OnDisable()
        {
            if (inputAction != null)
            {
                inputAction.action.performed -= OnInputActionPerformed;
            }
        }

        private void OnInputActionPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (runtimeQuestLogWindow == null) return;
            if (DialogueManager.IsDialogueSystemInputDisabled()) return;
            ToggleQuestLogWindow();
        }

#endif

        private void Update()
        {
            if (runtimeQuestLogWindow == null) return;
            if (DialogueManager.IsDialogueSystemInputDisabled()) return;
            if (InputDeviceManager.IsKeyDown(key) ||
                (!string.IsNullOrEmpty(buttonName) && DialogueManager.getInputButtonDown(buttonName)))
            {
                ToggleQuestLogWindow();
            }
        }

        public void ToggleQuestLogWindow()
        {
            if (runtimeQuestLogWindow == null) return;
            if (runtimeQuestLogWindow.isOpen)
            {
                runtimeQuestLogWindow.Close();
            }
            else
            {
                runtimeQuestLogWindow.Open();
            }
        }

    }

}
