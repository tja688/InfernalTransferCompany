#if USE_TIMELINE
#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class PlaySequenceBehaviour : PlayableBehaviour
    {

        [Tooltip("播放此 Sequence。")]
        [TextArea(5, 5)]
        public string sequence;

        [Tooltip("（可选）Sequence 中的另一方主体。")]
        public Transform listener;

    }
}
#endif
#endif
