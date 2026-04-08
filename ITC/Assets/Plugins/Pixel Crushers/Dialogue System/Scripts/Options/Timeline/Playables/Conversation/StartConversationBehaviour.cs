#if USE_TIMELINE
#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class StartConversationBehaviour : PlayableBehaviour
    {

        [Tooltip("（可选）另一位参与者。")]
        public Transform conversant;

        [Tooltip("要开始的对话。")]
        [ConversationPopup(true)]
        public string conversation;

        [Tooltip("跳转到指定对话条目，而不是从该对话的 START 节点开始。")]
        public bool jumpToSpecificEntry;

        [Tooltip("要跳转到的对话条目。")]
        public int entryID;

        [Tooltip("在开始此对话前停止所有正在进行的对话。")]
        public bool exclusive = false;

        [Tooltip("（可选）如果要为此对话覆盖对话 UI，请在此分配。")]
        public AbstractDialogueUI overrideDialogueUI;

        public string GetEditorDialogueText()
        {
            var dialogueText = ConversationTimelineUtility.GetDialogueText(conversation, jumpToSpecificEntry ? entryID : -1);
            return "'" + dialogueText + "'";
        }

    }
}
#endif
#endif
