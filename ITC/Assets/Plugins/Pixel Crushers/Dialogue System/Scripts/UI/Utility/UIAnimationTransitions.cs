// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class UIAnimationTransitions
    {
        [Tooltip("显示面板时播放此状态/触发器。")]
        public string showTrigger = "Show";

        [Tooltip("隐藏面板时播放此状态/触发器。")]
        public string hideTrigger = "Hide";

        [Tooltip("指定 Show Trigger 和 Hide Trigger 是 Animator 状态还是触发器参数。")]
        public UIShowHideController.TransitionMode transitionMode = UIShowHideController.TransitionMode.State;

        public bool debug = false;

        public void ClearTriggers(UIShowHideController showHideController)
        {
            if (showHideController != null && transitionMode == UIShowHideController.TransitionMode.Trigger)
            {
                showHideController.ClearTrigger(showTrigger);
                showHideController.ClearTrigger(hideTrigger);
            }
        }
    }

}
