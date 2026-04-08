// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This component hooks up the elements of a Unity UI quest group template.
    /// Add it to your quest group template and assign the properties.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class UnityUIQuestGroupTemplate : MonoBehaviour
    {

        [Header("任务组标题")]
        [Tooltip("任务组名称")]
        public UnityEngine.UI.Text heading;

        public bool ArePropertiesAssigned
        {
            get
            {
                return (heading != null);
            }
        }

        public void Initialize() { }

    }

}
