// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// References to standard UI selector elements to display the current
    /// selection instead of using Selector/ProximitySelector's default
    /// OnGUI method. The Selector/ProximitySelector should have a
    /// SelectorUseStandardUIElements component to tell it to use these
    /// UI elements.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class StandardUISelectorElements : MonoBehaviour
    {

        [HelpBox("If a GameObject with a Selector or Proximity Selector has a Selector Use Standard UI Elements component, it will use these UI elements.", HelpBoxMessageType.None)]

        /// <summary>
        /// The main graphic (optional). Assign this if you have created an entire 
        /// panel for the selector.
        /// </summary>
        [Tooltip("（可选）主面板。如果你为选择器创建了整个面板，请在此分配。")]
        public UnityEngine.UI.Graphic mainGraphic = null;

        /// <summary>
        /// The text for the name of the current selection.
        /// </summary>
        [Tooltip("当前所选对象名称的文本元素。")]
        public UITextField nameText = null;

        /// <summary>
        /// The text for the use message (e.g., "Press spacebar to use").
        /// </summary>
        [Tooltip("使用提示的文本元素，例如按空格键使用。")]
        public UITextField useMessageText = null;

        [Tooltip("使用下方定义的“在范围内”和“超出范围”文本颜色。")]
        public bool useRangeColors = true;

        [Tooltip("当 selector 在可用范围内时，将文本元素设为此颜色。")]
        public Color inRangeColor = Color.yellow;

        [Tooltip("当 selector 超出范围时，将文本元素设为此颜色。")]
        public Color outOfRangeColor = Color.gray;

        /// <summary>
        /// The graphic to show if the selection is in range.
        /// </summary>
        [Tooltip("当选择在范围内时显示的可选图像。")]
        public UnityEngine.UI.Graphic reticleInRange = null;

        /// <summary>
        /// The graphic to show if the selection is out of range.
        /// </summary>
        [Tooltip("当选择超出范围时显示的可选图像。")]
        public UnityEngine.UI.Graphic reticleOutOfRange = null;

        [Serializable]
        public class AnimationTransitions
        {
            public string showTrigger = "Show";
            public string hideTrigger = "Hide";
        }

        public AnimationTransitions animationTransitions = new AnimationTransitions();

        public Animator animator { get; private set; }

        private static List<StandardUISelectorElements> m_instances = new List<StandardUISelectorElements>();

        public static List<StandardUISelectorElements> instances
        {
            get { return m_instances; }
        }

        public static StandardUISelectorElements instance
        {
            get { return (m_instances.Count > 0) ? m_instances[0] : null; }
        }

#if UNITY_2019_3_OR_NEWER && UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStaticVariables()
        {
            m_instances = new List<StandardUISelectorElements>();
        }
#endif

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            m_instances.Add(this);
        }

        private void OnDisable()
        {
            m_instances.Remove(this);
        }
    }

}
