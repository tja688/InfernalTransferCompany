// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    [System.Serializable]
    public class UnityUIQuestTemplateAlternateDescriptions
    {

        [Tooltip("（可选）如果已设置，则在状态为 success 时使用。")]
        public UnityEngine.UI.Text successDescription;

        [Tooltip("（可选）如果已设置，则在状态为 failure 时使用。")]
        public UnityEngine.UI.Text failureDescription;

        public void SetActive(bool value)
        {
            if (successDescription != null) successDescription.gameObject.SetActive(value);
            if (failureDescription != null) failureDescription.gameObject.SetActive(value);
        }

    }

}
