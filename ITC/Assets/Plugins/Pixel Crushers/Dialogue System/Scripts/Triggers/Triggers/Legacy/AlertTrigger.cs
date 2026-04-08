using UnityEngine;
using System.Collections;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Deprecated by DialogueSystemTrigger.
    /// The Alert Trigger component shows a gameplay alert when a trigger event occurs.
    /// If a LocalizedTextTable has been asssigned to the Dialogue Manager, it will use
    /// localized version of the alert message.
    /// </summary>
    [AddComponentMenu("")] // Deprecated
    public class AlertTrigger : DialogueEventStarter
    {

        /// <summary>
        /// The trigger that shows the alert.
        /// </summary>
        [DialogueTriggerEvent]
        public DialogueTriggerEvent trigger = DialogueTriggerEvent.OnUse;

        /// <summary>
        /// If localizedTextTable is assigned, message is the field name of a field
        /// in the localized text table.
        /// </summary>
        [Tooltip("可选的本地化文本表；若已分配，Message 就是表中的字段。")]
        public LocalizedTextTable localizedTextTable;

        /// <summary>
        /// The message to show.
        /// </summary>
        [Tooltip("要显示的消息，其中可以包含 [var=varName] 等标签。")]
        public string message;

        /// <summary>
        /// The duration to show the message.
        /// </summary>
        [Tooltip("消息显示时长（秒）。如果为 0，则使用 Dialogue Manager 中定义的默认值。")]
        public float duration = 5f;

        /// <summary>
        /// The condition required for the alert.
        /// </summary>
        public Condition condition;

        private bool tryingToStart = false;

        public void OnBarkEnd(Transform actor)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnBarkEnd)) TryStart(actor);
        }

        public void OnConversationEnd(Transform actor)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnConversationEnd)) TryStart(actor);
        }

        public void OnSequenceEnd(Transform actor)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnSequenceEnd)) TryStart(actor);
        }

        public void OnUse(Transform actor)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnUse)) TryStart(actor);
        }

        public void OnUse(string message)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnUse)) TryStart(null);
        }

        public void OnUse()
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnUse)) TryStart(null);
        }

        public void OnTriggerEnter(Collider other)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerEnter)) TryStart(other.transform);
        }

        public void OnTriggerExit(Collider other)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerExit)) TryStart(other.transform);
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnCollisionEnter)) TryStart(collision.collider.transform);
        }

        public void OnCollisionExit(Collision collision)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerExit)) TryStart(collision.collider.transform);
        }

#if USE_PHYSICS2D || !UNITY_2018_1_OR_NEWER

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerEnter)) TryStart(other.transform);
        }

        public void OnTriggerExit2D(Collider2D other)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerExit)) TryStart(other.transform);
        }

        public void OnCollisionEnter2D(Collision2D collision)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerEnter)) TryStart(collision.collider.transform);
        }

        public void OnCollisionExit2D(Collision2D collision)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerExit)) TryStart(collision.collider.transform);
        }

#endif

        public void Start()
        {
            // Waits one frame to allow all other components to finish their Start() methods.
            if (trigger == DialogueTriggerEvent.OnStart) StartCoroutine(StartAfterOneFrame());
        }

        private bool listenForOnDestroy = false;

        public void OnEnable()
        {
            listenForOnDestroy = true;
            // Waits one frame to allow all other components to finish their OnEnable() methods.
            if (trigger == DialogueTriggerEvent.OnEnable) StartCoroutine(StartAfterOneFrame());
        }

        public void OnDisable()
        {
            if (!listenForOnDestroy) return;
            if (trigger == DialogueTriggerEvent.OnDisable) TryStart(null);
        }

        public void OnLevelWillBeUnloaded()
        {
            listenForOnDestroy = false;
        }

        public void OnApplicationQuit()
        {
            listenForOnDestroy = false;
        }

        public void OnDestroy()
        {
            if (!listenForOnDestroy) return;
            if (trigger == DialogueTriggerEvent.OnDestroy) TryStart(null);
        }

        private IEnumerator StartAfterOneFrame()
        {
            yield return null;
            TryStart(null);
        }

        /// <summary>
        /// Show the alert if the condition is true.
        /// </summary>
        public void TryStart(Transform actor)
        {
            if (tryingToStart) return;
            tryingToStart = true;
            try
            {
                if (((condition == null) || condition.IsTrue(actor)) && !string.IsNullOrEmpty(message))
                {
                    string actualMessage = message;
                    if ((localizedTextTable != null) && localizedTextTable.ContainsField(message))
                    {
                        actualMessage = localizedTextTable[message];
                    }
                    string text = FormattedText.Parse(actualMessage, DialogueManager.masterDatabase.emphasisSettings).text;
                    if (Mathf.Approximately(0, duration))
                    {
                        DialogueManager.ShowAlert(text);
                    }
                    else
                    {
                        DialogueManager.ShowAlert(text, duration);
                    }
                    DestroyIfOnce();
                }
            }
            finally
            {
                tryingToStart = false;
            }
        }

    }

}
