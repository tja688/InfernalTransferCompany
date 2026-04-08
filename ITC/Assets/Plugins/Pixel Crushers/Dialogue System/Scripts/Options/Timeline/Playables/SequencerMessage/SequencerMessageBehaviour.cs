#if USE_TIMELINE
#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class SequencerMessageBehaviour : PlayableBehaviour
    {

        [Tooltip("要发送给 Dialogue System 的 Sequence 消息。")]
        public string message;

    }
}
#endif
#endif
