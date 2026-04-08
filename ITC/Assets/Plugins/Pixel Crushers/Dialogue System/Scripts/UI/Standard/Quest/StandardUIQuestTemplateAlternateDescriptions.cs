// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class StandardUIQuestTemplateAlternateDescriptions
    {

        [Tooltip("（可选）如果已设置，则在状态为 success 时使用。")]
        public UITextField successDescription;

        [Tooltip("（可选）如果已设置，则在状态为 failure 时使用。")]
        public UITextField failureDescription;

        public void SetActive(bool value)
        {
            successDescription.SetActive(value);
            failureDescription.SetActive(value);
        }

    }

}
