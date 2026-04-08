#if USE_TIMELINE
#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class BarkBehaviour : PlayableBehaviour
    {

        [Tooltip("从对话中获取 Bark 文本。")]
        public bool useConversation = true;

        [Tooltip("从此对话中获取 Bark 文本。")]
        [ConversationPopup(true)]
        public string conversation;

        [Tooltip("对一个特定的对话条目执行旁白，而不是从对话的 START 节点开始。")]
        public bool barkSpecificEntry;

        [Tooltip("要旁白的对话条目。")]
        public int entryID;

        [Tooltip("对这段文本执行旁白。")]
        public string text;

        [Tooltip("（可选）Barker 正在对这个监听者进行旁白。")]
        public Transform listener;

        public string GetEditorBarkText()
        {
            return useConversation 
                ? ("[" + conversation + "] '" + ConversationTimelineUtility.GetDialogueText(conversation, -1) + "'")
                : text;
        }

    }
}
#endif
#endif
