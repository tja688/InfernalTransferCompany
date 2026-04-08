// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// The Bark On Idle component can be used to make an NPC bark on timed intervals.
    /// Barks don't occur while a conversation is active.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class BarkOnIdle : BarkStarter
    {

        [Tooltip("此组件第一次启动时立即旁白。")]
        public bool barkOnStart = false;

        [Tooltip("组件启用时旁白。如果先禁用再重新启用，也会再次旁白。")]
        public bool barkOnEnable = false;

        /// <summary>
        /// The minimum seconds between barks.
        /// </summary>
        [Tooltip("两次旁白之间的最短秒数。")]
        public float minSeconds = 5f;

        /// <summary>
        /// The maximum seconds between barks.
        /// </summary>
        [Tooltip("两次旁白之间的最长秒数。")]
        public float maxSeconds = 10f;

        /// <summary>
        /// The target to bark at. Leave unassigned to just bark into the air.
        /// </summary>
        [Tooltip("旁白的目标对象。留空则只向空中发声。")]
        public Transform target;

        protected override bool useOnce { get { return false; } } // Removed confusing Once checkbox.

        private bool started = false;

        protected override void Start()
        {
            base.Start();
            started = true;
            StartBarkLoop();
            if (barkOnStart && !barkOnEnable) TryIdleBark();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StartBarkLoop();
            if (barkOnEnable) TryIdleBark();
        }

        /// <summary>
        /// Starts the bark loop. Normally this is started in the Start() method. If you need to
        /// restart it for some reason, call this method.
        /// </summary>
        public virtual void StartBarkLoop()
        {
            if (!started) return;
            StopAllCoroutines();
            StartCoroutine(BarkLoop());
        }

        protected virtual IEnumerator BarkLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minSeconds, maxSeconds));
                TryIdleBark();
            }
        }

        protected virtual void TryIdleBark()
        {
            if (enabled && (!DialogueManager.isConversationActive || allowDuringConversations) && !DialogueTime.isPaused)
            {
                TryBark(target);
            }
        }

    }

}
