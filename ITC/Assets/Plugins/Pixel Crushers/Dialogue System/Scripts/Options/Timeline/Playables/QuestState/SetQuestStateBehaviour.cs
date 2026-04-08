#if USE_TIMELINE
#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class SetQuestStateBehaviour : PlayableBehaviour
    {

        [QuestPopup]
        public string quest;

        [Tooltip("更改任务的主状态。")]
        public bool setQuestState;

        [QuestState]
        public QuestState questState;

        [Tooltip("更改任务条目的状态。")]
        public bool setQuestEntryState;

        public int questEntryNumber;

        [QuestState]
        public QuestState questEntryState;

    }
}
#endif
#endif
