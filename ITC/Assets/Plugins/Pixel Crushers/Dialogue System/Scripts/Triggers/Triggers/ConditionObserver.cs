// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;
using System.Collections;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// The Condition Observer component evaluates a condition on a set frequency. When the
    /// condition is true, it sends a message to a list of GameObjects and shows a gameplay
    /// alert message.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class ConditionObserver : MonoBehaviour
    {

        /// <summary>
        /// The frequency at which to check the condition.
        /// </summary>
        [Tooltip("检查间隔（秒）。")]
        public float frequency = 1;

        /// <summary>
        /// When observed condition becomes true, run actions and then deactivate this component.
        /// </summary>
        [Tooltip("当观察到的条件为真时，执行动作，然后停用此组件。")]
        public bool once;

        /// <summary>
        /// Observe this game object when evaluating the condition.
        /// </summary>
        [Tooltip("在评估 Condition 时引用此 GameObject。")]
        public GameObject observeGameObject = null;

        /// <summary>
        /// The conditions under which the trigger will fire.
        /// </summary>
        public Condition condition = new Condition();

        /// <summary>
        /// The name of the quest to update when the condition is true. Blank for none.
        /// </summary>
        [Tooltip("当条件为真时设置此任务的状态。")]
        public string questName = string.Empty;

        /// <summary>
        /// The new state of the quest.
        /// </summary>
        [Tooltip("当条件为真时将任务设置为此状态。")]
        [QuestState]
        public QuestState questState;

        /// <summary>
        /// The lua code to run.
        /// </summary>
        [Tooltip("当条件为真时运行此 Lua 代码。留空则跳过。")]
        public string luaCode = string.Empty;

        /// <summary>
        /// The sequence to play.
        /// </summary>
        [Tooltip("当条件为真时播放此 Sequence。留空则跳过。")]
        [TextArea(1, 20)]
        public string sequence = string.Empty;

        /// <summary>
        /// An optional gameplay alert message. Leave blank for no message.
        /// </summary>
        [Tooltip("当条件为真时显示此 alert message。留空则跳过。")]
        public string alertMessage = string.Empty;

        /// <summary>
        /// An optional localized text table to use for the alert message.
        /// </summary>
        [Tooltip("用于本地化 alert message 的 Text Table。")]
        public TextTable textTable = null;

        [Serializable]
        public class SendMessageAction
        {
            public GameObject gameObject = null;
            public string message = "OnUse";
            public string parameter = string.Empty;
        }

        public SendMessageAction[] sendMessages = new SendMessageAction[0];

        [HideInInspector]
        public bool useQuestNamePicker = true;

        private bool started = false;

        private void Start()
        {
            started = true;
            StartObserving();
        }

        private void OnEnable()
        {
            if (started) StartObserving();
        }

        private void OnDisable()
        {
            StopObserving();
        }

        private void StartObserving()
        {
            StopObserving();
            StartCoroutine(Observe());
        }

        private void StopObserving()
        {
            StopAllCoroutines();
        }

        private IEnumerator Observe()
        {
            yield return new WaitForSeconds(UnityEngine.Random.value);
            while (true)
            {
                Check();
                yield return new WaitForSeconds(frequency);
            }
        }

        /// <summary>
        /// Call this method to manually check the condition and fire the action
        /// if it's true.
        /// </summary>
        public void Check()
        {
            var observeTransform = (observeGameObject == null) ? null : observeGameObject.transform;
            if (condition.IsTrue(observeTransform))
            {
                Fire();
            }
        }

        /// <summary>
        /// Sets the observed GameObject and checks the condition.
        /// </summary>
        /// <param name="gameObject">Game object.</param>
        public void Check(GameObject gameObject)
        {
            observeGameObject = gameObject;
            Check();
        }

        /// <summary>
        /// Sets the observed GameObject to the named GameObject and checks 
        /// the condition.
        /// </summary>
        /// <param name="gameObjectName">Game object name.</param>
        public void Check(string gameObjectName)
        {
            var newGameObject = Tools.GameObjectHardFind(gameObjectName);
            if (newGameObject != null) observeGameObject = newGameObject;
            Check();
        }

        /// <summary>
        /// Call this method to manually run the action.
        /// </summary>
        public void Fire()
        {
            // Quest:
            if (!string.IsNullOrEmpty(questName))
            {
                QuestLog.SetQuestState(questName, questState);
            }

            // Lua:
            if (!string.IsNullOrEmpty(luaCode))
            {
                Lua.Run(luaCode, DialogueDebug.logInfo);
                DialogueManager.CheckAlerts();
            }

            // Sequence:
            if (!string.IsNullOrEmpty(sequence))
            {
                DialogueManager.PlaySequence(sequence);
            }

            // Alert:
            if (!string.IsNullOrEmpty(alertMessage))
            {
                string localizedAlertMessage;
                if ((textTable != null) && textTable.HasFieldTextForLanguage(alertMessage, Localization.GetCurrentLanguageID(textTable)))
                {
                    localizedAlertMessage = textTable.GetFieldTextForLanguage(alertMessage, Localization.GetCurrentLanguageID(textTable));
                }
                else
                {
                    localizedAlertMessage = DialogueManager.GetLocalizedText(alertMessage);
                }
                DialogueManager.ShowAlert(localizedAlertMessage);
            }

            // Send Messages:
            foreach (var sma in sendMessages)
            {
                if (sma.gameObject != null && !string.IsNullOrEmpty(sma.message))
                {
                    sma.gameObject.SendMessage(sma.message, sma.parameter, SendMessageOptions.DontRequireReceiver);
                }
            }

            DialogueManager.SendUpdateTracker();

            if (once)
            {
                StopObserving();
                enabled = false;
            }
        }
    }

}
