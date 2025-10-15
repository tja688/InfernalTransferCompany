using System.Collections.Generic;
using UnityEngine;

namespace DirectorUI
{
    [CreateAssetMenu(fileName = "UITransitionTicket", menuName = "DirectorUI/Transition Ticket")]
    public class UITransitionTicket : ScriptableObject
    {
        [Tooltip("過渡的起始視圖ID")]
        public string fromViewId;
        [Tooltip("過渡的目標視圖ID")]
        public string toViewId;

        [Header("動畫編排")]
        public List<AnimationStep> outroAnimations = new();
        public List<AnimationStep> introAnimations = new();
        public List<StuntDoubleStep> stuntDoubleTransitions = new();

        public string TicketKey => UIDirector.BuildTicketKey(fromViewId, toViewId);

        [System.Serializable]
        public class AnimationStep
        {
            [Tooltip("目標元素的ID")]
            public string elementId;
            [Tooltip("要播放的動畫預設")]
            public UITweenPreset animationPreset;
            [Tooltip("延遲播放時間（秒）")]
            public float delay = 0f;
        }

        [System.Serializable]
        public class StuntDoubleStep
        {
            [Tooltip("原身和替身共同的元素ID")]
            public string elementId;
            [Tooltip("原身飛向替身位置的動畫預設")]
            public UITweenPreset flyingAnimationPreset;
        }
    }
}
