// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Serialization;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Overrides the dialogue UI for conversations involving the game object. To use this
    /// component, add it to a game object. When the game object is a conversant, the conversation
    /// will use the dialogue UI on this component instead of the UI on the DialogueManager.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class OverrideDialogueUI : OverrideUIBase
    {

        /// <summary>
        /// The dialogue UI to use for the game object this component is attached to.
        /// </summary>
        [Tooltip("当此 GameObject 参与对话时使用此对话 UI。")]
        public GameObject ui;

        [Tooltip("如果实例化的是 prefab，则在对话结束时保留在内存中，而不是销毁。")]
        [FormerlySerializedAs("dontDestroyPrefabIntance")]
        public bool dontDestroyPrefabInstance = true;

        protected virtual void OnDestroy()
        {
            if (dontDestroyPrefabInstance) return;
            if (!Tools.IsPrefab(ui)) Destroy(ui);
        }

    }

}
