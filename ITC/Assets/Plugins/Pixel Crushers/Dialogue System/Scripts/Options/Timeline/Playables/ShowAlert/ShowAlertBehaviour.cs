#if USE_TIMELINE
#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class ShowAlertBehaviour : PlayableBehaviour
    {

        [Tooltip("使用 Dialogue System 的提示面板显示此消息。")]
        public string message;

        [Tooltip("根据文本长度而不是 playable clip 的时长来显示提示。")]
        public bool useTextLengthForDuration;

    }
}
#endif
#endif
