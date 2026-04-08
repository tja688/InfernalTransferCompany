// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Abstract base class for OverrideDialogueUI and OverrideDisplaySettings.
    /// </summary>
    public abstract class OverrideUIBase : MonoBehaviour
    {

        /// <summary>
        /// When both participants have overrides, the higher priority takes precedence.
        /// </summary>
        [Tooltip("当双方都具有 override 时，优先级更高者优先。")]
        public int priority = 0;

    }

}
