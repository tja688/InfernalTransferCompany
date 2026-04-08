// Copyright (c) Pixel Crushers. All rights reserved.

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Add this to the Dialogue Manager and/or participants to hook into various Dialogue System events.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class DialogueSystemEvents : MonoBehaviour
    {

        [System.Serializable]
        public class StringEvent : UnityEvent<string> { }

        [System.Serializable]
        public class TransformEvent : UnityEvent<Transform> { }

        [System.Serializable]
        public class SubtitleEvent : UnityEvent<Subtitle> { }

        [System.Serializable]
        public class ResponsesEvent : UnityEvent<Response[]> { }

        [System.Serializable]
        public class ConversationEvents
        {
            [Tooltip("当对话开始时调用。Transform 是主参与者（通常是玩家）。")]
            public TransformEvent onConversationStart = new TransformEvent();

            [Tooltip("当对话结束时调用。Transform 是主参与者（通常是玩家）。")]
            public TransformEvent onConversationEnd = new TransformEvent();

            [Tooltip("在帧结束时运行 OnConversationEnd() 事件，让其他脚本先完成本帧处理。")]
            public bool runOnConversationEndEventsAtEndOfFrame = false;

            [Tooltip("当对话被取消时调用。Transform 是主参与者（通常是玩家）。")]
            public TransformEvent onConversationCancelled = new TransformEvent();

            [Tooltip("在台词发出前、OnConversationLine 之前调用。传递 Subtitle。")]
            public SubtitleEvent onConversationLineEarly = new SubtitleEvent();

            [Tooltip("在台词发出前、OnConversationLineEarly 之后调用。传递 Subtitle。")]
            public SubtitleEvent onConversationLine = new SubtitleEvent();

            [Tooltip("当一行结束时调用。传递 Subtitle。")]
            public SubtitleEvent onConversationLineEnd = new SubtitleEvent();

            [Tooltip("当玩家在台词显示过程中按下取消按钮时调用。")]
            public SubtitleEvent onConversationLineCancelled = new SubtitleEvent();

            [Tooltip("显示 Response Menu 时调用。传递 Response[] 数组。")]
            public ResponsesEvent onConversationResponseMenu = new ResponsesEvent();

            [Tooltip("当 Response Menu 超时调用。")]
            public UnityEvent onConversationResponseMenuTimeout = new UnityEvent();

            [Tooltip("当对话通过链接跳转到另一个对话时调用。Transform 是主参与者（通常是玩家）。")]
            public TransformEvent onLinkedConversationStart = new TransformEvent();
        }

        [System.Serializable]
        public class BarkEvents
        {
            [Tooltip("当 bark 开始时调用。Transform 是 bark 的接收者。")]
            public TransformEvent onBarkStart = new TransformEvent();

            [Tooltip("当 bark 结束时调用。Transform 是 bark 的接收者。")]
            public TransformEvent onBarkEnd = new TransformEvent();

            [Tooltip("在 bark 台词发出前调用。传递 Subtitle。")]
            public SubtitleEvent onBarkLine = new SubtitleEvent();
        }

        [System.Serializable]
        public class SequenceEvents
        {
            [Tooltip("当 Sequence 开始时调用。Transform 是该 Sequence 的 'listener'。")]
            public TransformEvent onSequenceStart = new TransformEvent();

            [Tooltip("当 Sequence 结束时调用。Transform 是该 Sequence 的 'listener'。")]
            public TransformEvent onSequenceEnd = new TransformEvent();
        }

        [System.Serializable]
        public class QuestEvents
        {
            [Tooltip("当任务状态或任务条目状态变化时调用。字符串是任务标题。")]
            public StringEvent onQuestStateChange = new StringEvent();

            [Tooltip("当某个任务启用 tracking 时调用。字符串是任务标题。")]
            public StringEvent onQuestTrackingEnabled = new StringEvent();

            [Tooltip("当某个任务禁用 tracking 时调用。字符串是任务标题。")]
            public StringEvent onQuestTrackingDisabled = new StringEvent();

            [Tooltip("当更新 quest tracker 时调用。")]
            public UnityEvent onUpdateQuestTracker = new UnityEvent();
        }

        [System.Serializable]
        public class PauseEvents
        {
            [Tooltip("当调用 DialogueManager.Pause() 时调用。")]
            public UnityEvent onDialogueSystemPause = new UnityEvent();

            [Tooltip("当调用 DialogueManager.Unpause() 时调用。")]
            public UnityEvent onDialogueSystemUnpause = new UnityEvent();
        }


        public ConversationEvents conversationEvents = new ConversationEvents();

        public BarkEvents barkEvents = new BarkEvents();

        public SequenceEvents sequenceEvents = new SequenceEvents();

        public QuestEvents questEvents = new QuestEvents();

        public PauseEvents pauseEvents = new PauseEvents();

        private WaitForEndOfFrame endOfFrame = CoroutineUtility.endOfFrame;

        #region Conversation Events

        public void OnConversationStart(Transform actor)
        {
            conversationEvents.onConversationStart.Invoke(actor);
        }

        public void OnConversationEnd(Transform actor)
        {
            if (conversationEvents.runOnConversationEndEventsAtEndOfFrame)
            {
                StartCoroutine(InvokeOnConversationEndAtEndOfFrame(actor));
            }
            else
            {
                conversationEvents.onConversationEnd.Invoke(actor);
            }
        }

        private IEnumerator InvokeOnConversationEndAtEndOfFrame(Transform actor)
        {
            yield return endOfFrame;
            conversationEvents.onConversationEnd.Invoke(actor);
        }

        public void OnConversationCancelled(Transform actor)
        {
            conversationEvents.onConversationCancelled.Invoke(actor);
        }

        public void OnConversationLineEarly(Subtitle subtitle)
        {
            conversationEvents.onConversationLineEarly.Invoke(subtitle);
        }

        public void OnConversationLine(Subtitle subtitle)
        {
            conversationEvents.onConversationLine.Invoke(subtitle);
        }

        public void OnConversationLineEnd(Subtitle subtitle)
        {
            conversationEvents.onConversationLineEnd.Invoke(subtitle);
        }

        public void OnConversationLineCancelled(Subtitle subtitle)
        {
            conversationEvents.onConversationLineCancelled.Invoke(subtitle);
        }

        public void OnConversationResponseMenu(Response[] responses)
        {
            conversationEvents.onConversationResponseMenu.Invoke(responses);
        }

        public void OnConversationTimeout()
        {
            conversationEvents.onConversationResponseMenuTimeout.Invoke();
        }

        public void OnLinkedConversationStart(Transform actor)
        {
            conversationEvents.onLinkedConversationStart.Invoke(actor);
        }

        #endregion

        #region Bark Events

        public void OnBarkStart(Transform actor)
        {
            barkEvents.onBarkStart.Invoke(actor);
        }

        public void OnBarkEnd(Transform actor)
        {
            barkEvents.onBarkEnd.Invoke(actor);
        }

        public void OnBarkLine(Subtitle subtitle)
        {
            barkEvents.onBarkLine.Invoke(subtitle);
        }
        #endregion

        #region Sequence Events

        public void OnSequenceStart(Transform actor)
        {
            sequenceEvents.onSequenceStart.Invoke(actor);
        }

        public void OnSequenceEnd(Transform actor)
        {
            sequenceEvents.onSequenceEnd.Invoke(actor);
        }

        #endregion

        #region Quest Events

        public void OnQuestStateChange(string title)
        {
            questEvents.onQuestStateChange.Invoke(title);
        }

        public void OnQuestTrackingEnabled(string title)
        {
            questEvents.onQuestTrackingEnabled.Invoke(title);
        }

        public void OnQuestTrackingDisabled(string title)
        {
            questEvents.onQuestTrackingDisabled.Invoke(title);
        }

        public void UpdateTracker()
        {
            questEvents.onUpdateQuestTracker.Invoke();
        }

        #endregion

        #region Pause Events

        public void OnDialogueSystemPause()
        {
            pauseEvents.onDialogueSystemPause.Invoke();
        }

        public void OnDialogueSystemUnpause()
        {
            pauseEvents.onDialogueSystemUnpause.Invoke();
        }

        #endregion

    }

}
