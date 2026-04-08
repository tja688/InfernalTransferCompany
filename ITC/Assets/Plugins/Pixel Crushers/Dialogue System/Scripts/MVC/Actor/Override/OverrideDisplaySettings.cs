// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Overrides the display settings for conversations involving the game object. To use this
    /// component, add it to a game object. When the game object is a conversant, the conversation
    /// will use the display settings on this component instead of the settings on the 
    /// DialogueManager.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class OverrideDisplaySettings : OverrideUIBase
    {

        /// <summary>
        /// The display settings to use for the game object this component is attached to.
        /// </summary>
        [Tooltip("当此 GameObject 参与对话时使用这些显示设置。")]
        public DisplaySettings displaySettings;

    }

}
