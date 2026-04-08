#if USE_TIMELINE
#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class ContinueConversationBehaviour : PlayableBehaviour
    {
        public enum Operation { Continue, ClearSubtitleText }

        [Tooltip("继续当前字幕，或仅清空字幕面板中的文本。")]
        public Operation operation = Operation.Continue;

        [Tooltip("如果 Operation 是 Clear Subtitle Text，则清除这些面板。")]
        public int clearPanelNumber = 0;

        public bool clearAllPanels = false;
    }
}
#endif
#endif
