#if USE_TIMELINE
#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    [AddComponentMenu("")] // Use wrapper.
    public class TimelineTrigger : DialogueEventStarter
    {

        [DialogueTriggerEvent]
        public DialogueTriggerEvent trigger = DialogueTriggerEvent.OnUse;

        [Tooltip("用于播放 Timeline 的 Playable Director。若已指定 Timeline，则在触发器触发时播放它。")]
        public PlayableDirector playableDirector;

        [Tooltip("如果上方的 Playable Director 未指定，或者 Playable Director 没有分配资产，则在触发器触发时播放此 Timeline 资产。")]
        public TimelineAsset timelineAsset;

        public Condition condition;

        [Tooltip("（可选）将这些 GameObjects 绑定到 Timeline 的轨道。")]
        public List<GameObject> bindings = new List<GameObject>();

        private bool tryingToStart = false;

        public void OnBarkEnd(Transform actor)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnBarkEnd)) TryStart(actor);
        }

        public void OnConversationEnd(Transform actor)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnConversationEnd)) TryStart(actor);
        }

        public void OnSequenceEnd(Transform actor)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnSequenceEnd)) TryStart(actor);
        }

        public void OnUse(Transform actor)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnUse)) TryStart(actor);
        }

        public void OnUse(string message)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnUse)) TryStart(null);
        }

        public void OnUse()
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnUse)) TryStart(null);
        }

        public void OnTriggerEnter(Collider other)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerEnter)) TryStart(other.transform);
        }

        public void OnTriggerExit(Collider other)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerExit)) TryStart(other.transform);
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnCollisionEnter)) TryStart(collision.collider.transform);
        }

        public void OnCollisionExit(Collision collision)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerExit)) TryStart(collision.collider.transform);
        }

#if USE_PHYSICS2D || !UNITY_2018_1_OR_NEWER

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerEnter)) TryStart(other.transform);
        }

        public void OnTriggerExit2D(Collider2D other)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerExit)) TryStart(other.transform);
        }

        public void OnCollisionEnter2D(Collision2D collision)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerEnter)) TryStart(collision.collider.transform);
        }

        public void OnCollisionExit2D(Collision2D collision)
        {
            if (enabled && (trigger == DialogueTriggerEvent.OnTriggerExit)) TryStart(collision.collider.transform);
        }

#endif

        public void Awake()
        {
            if (playableDirector == null) playableDirector = GetComponent<PlayableDirector>();
            if (playableDirector == null && timelineAsset != null) playableDirector = gameObject.AddComponent<PlayableDirector>();
            if (playableDirector != null && playableDirector.playableAsset == null) playableDirector.playableAsset = timelineAsset;
        }

        public void Start()
        {
            // Waits one frame to allow all other components to finish their Start() methods.
            if (trigger == DialogueTriggerEvent.OnStart) StartCoroutine(StartAfterOneFrame());
        }

        private bool listenForOnDestroy = false;

        public void OnEnable()
        {
            listenForOnDestroy = true;
            // Waits one frame to allow all other components to finish their OnEnable() methods.
            if (trigger == DialogueTriggerEvent.OnEnable) StartCoroutine(StartAfterOneFrame());
        }

        public void OnDisable()
        {
            if (!listenForOnDestroy) return;
            if (trigger == DialogueTriggerEvent.OnDisable) TryStart(null);
        }

        public void OnLevelWillBeUnloaded()
        {
            listenForOnDestroy = false;
        }

        public void OnApplicationQuit()
        {
            listenForOnDestroy = false;
        }

        public void OnDestroy()
        {
            if (!listenForOnDestroy) return;
            if (trigger == DialogueTriggerEvent.OnDestroy) TryStart(null);
        }

        private IEnumerator StartAfterOneFrame()
        {
            yield return null;
            TryStart(null);
        }

        /// <summary>
        /// Show the alert if the condition is true.
        /// </summary>
        public void TryStart(Transform actor)
        {
            if (tryingToStart) return;
            tryingToStart = true;
            try
            {
                if ((condition == null) || condition.IsTrue(actor))
                {
                    SetBindings();
                    playableDirector.Play();
                    DestroyIfOnce();
                }
            }
            finally
            {
                tryingToStart = false;
            }
        }

        private void SetBindings()
        {
            var timelineAsset = playableDirector.playableAsset as TimelineAsset;
            if (timelineAsset == null) return;
            for (var i = 0; i < bindings.Count; i++)
            {
                if (bindings[i] != null)
                {
                    var track = timelineAsset.GetOutputTrack(i);
                    if (track != null)
                    {
                        playableDirector.SetGenericBinding(track, bindings[i]);
                    }
                }
            }
        }

    }
}
#endif
#endif
