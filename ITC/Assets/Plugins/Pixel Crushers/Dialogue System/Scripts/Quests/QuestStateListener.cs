// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Add this to a GameObject such as an NPC that wants to know about quest state changes
    /// to a specific quest. You can add multiple QuestStateListener components to listen
    /// to multiple quests.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class QuestStateListener : MonoBehaviour
    {

        [QuestPopup(true)]
        public string questName;

        [Serializable]
        public class QuestStateIndicatorLevel
        {
            [Tooltip("要监听的任务状态。")]
            public QuestState questState;

            [Tooltip("还必须满足的条件。")]
            public Condition condition;

            [Tooltip("达到此任务状态时使用的指示级别。")]
            public int indicatorLevel;

            public UnityEvent onEnterState = new UnityEvent();
        }

        public QuestStateIndicatorLevel[] questStateIndicatorLevels = new QuestStateIndicatorLevel[0];

        [Serializable]
        public class QuestEntryStateIndicatorLevel
        {
            [Tooltip("任务条目编号。")]
            public int entryNumber;

            [Tooltip("要监听的任务条目状态。")]
            public QuestState questState;

            [Tooltip("还必须满足的条件。")]
            public Condition condition;

            [Tooltip("达到此任务状态时使用的指示级别。")]
            public int indicatorLevel;

            public UnityEvent onEnterState = new UnityEvent();
        }

        public QuestEntryStateIndicatorLevel[] questEntryStateIndicatorLevels = new QuestEntryStateIndicatorLevel[0];

        [Tooltip("启动组件时，不要调用任何 OnEnterState() 事件。")]
        public bool suppressOnEnterStateEventsOnStart = false;

        [Tooltip("如果已指定，则使用此 Quest State Indicator 组件。否则会自动在此 GameObject 或其父级/子级中查找 Quest State Indicator。")]
        [SerializeField] protected QuestStateIndicator m_questStateIndicator;
        protected QuestStateIndicator questStateIndicator
        {
            get
            {
                if (m_questStateIndicator == null) m_questStateIndicator = GetComponentInParent<QuestStateIndicator>() ?? GetComponentInChildren<QuestStateIndicator>();
                return m_questStateIndicator;
            }
        }

        protected QuestStateDispatcher m_questStateDispatcher;
        protected QuestStateDispatcher questStateDispatcher
        {
            get
            {
                if (m_questStateDispatcher == null)
                {
                    if (DialogueManager.instance != null)
                    {
                        m_questStateDispatcher = DialogueManager.instance.GetComponent<QuestStateDispatcher>();
                        if (m_questStateDispatcher == null)
                        {
                            m_questStateDispatcher = PixelCrushers.GameObjectUtility.FindFirstObjectByType<QuestStateDispatcher>();
                            if (m_questStateDispatcher == null)
                            {
                                m_questStateDispatcher = DialogueManager.instance.gameObject.AddComponent<QuestStateDispatcher>();
                            }
                        }
                    }
                    else
                    {
                        m_questStateDispatcher = PixelCrushers.GameObjectUtility.FindFirstObjectByType<QuestStateDispatcher>();
                        if (m_questStateDispatcher == null)
                        {
                            var go = new GameObject("QuestStateDispatcher");
                            DontDestroyOnLoad(go);
                            m_questStateDispatcher = go.AddComponent<QuestStateDispatcher>();
                        }
                    }
                }
                return m_questStateDispatcher;
            }
        }
        private bool m_started = false;
        protected bool started
        {
            get { return m_started; }
            set { m_started = value; }
        }

        protected bool m_suppressOnEnterStateEvent = false;

        protected virtual void OnApplicationQuit()
        {
            enabled = false;
        }

        protected virtual IEnumerator Start()
        {
            if (enabled)
            {
                if (DialogueDebug.logInfo) Debug.Log("Dialogue System: " + name + ": Listening for state changes to quest '" + questName + "'.", this);
                started = true;
                if (questStateDispatcher == null)
                {
                    if (DialogueDebug.logErrors) Debug.LogWarning("Dialogue System: Unexpected error. Quest State Listener on " + name + " can't find or create a Quest State Dispatcher.", this);
                }
                else
                {
                    questStateDispatcher.AddListener(this);
                }
                yield return null;
                m_suppressOnEnterStateEvent = suppressOnEnterStateEventsOnStart;
                UpdateIndicator();
                m_suppressOnEnterStateEvent = false;
            }
        }

        protected virtual void OnEnable()
        {
            if (started)
            {
                questStateDispatcher.AddListener(this);
                UpdateIndicator();
            }
        }

        protected virtual void OnDisable()
        {
            if (m_questStateDispatcher != null) m_questStateDispatcher.RemoveListener(this); // Use private; don't create new quest state dispatcher.
        }

        public virtual void OnChange()
        {
            UpdateIndicator();
        }

        /// <summary>
        /// Update the current quest state indicator based on the specified quest state indicator 
        /// levels and quest entry state indicator levels.
        /// </summary>
        public virtual void UpdateIndicator()
        {
            // Check quest state:
            var questState = QuestLog.GetQuestState(questName);
            for (int i = 0; i < questStateIndicatorLevels.Length; i++)
            {
                var questStateIndicatorLevel = questStateIndicatorLevels[i];
                if (((questState & questStateIndicatorLevel.questState) != 0) && questStateIndicatorLevel.condition.IsTrue(null))
                {
                    if (DialogueDebug.logInfo) Debug.Log("Dialogue System: " + name + ": Quest '" + questName + "' changed to state " + questState + ".", this);
                    if (questStateIndicator != null) questStateIndicator.SetIndicatorLevel(this, questStateIndicatorLevel.indicatorLevel);
                    if (!m_suppressOnEnterStateEvent)
                    {
                        questStateIndicatorLevel.onEnterState.Invoke();
                    }
                }
            }

            // Check quest entry states:
            for (int i = 0; i < questEntryStateIndicatorLevels.Length; i++)
            {
                var questEntryStateIndicatorLevel = questEntryStateIndicatorLevels[i];
                var questEntryState = QuestLog.GetQuestEntryState(questName, questEntryStateIndicatorLevel.entryNumber);
                if (((questEntryState & questEntryStateIndicatorLevel.questState) != 0) && questEntryStateIndicatorLevel.condition.IsTrue(null))
                {
                    if (DialogueDebug.logInfo) Debug.Log("Dialogue System: " + name + ": Quest '" + questName + "' entry " + questEntryStateIndicatorLevel.entryNumber + " changed to state " + questEntryState + ".", this);
                    if (questStateIndicator != null) questStateIndicator.SetIndicatorLevel(this, questEntryStateIndicatorLevel.indicatorLevel);
                    if (!m_suppressOnEnterStateEvent)
                    {
                        questEntryStateIndicatorLevel.onEnterState.Invoke();
                    }
                }
            }
        }

    }
}