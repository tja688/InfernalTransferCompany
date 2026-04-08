// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// The persistent active data component works with the PersistentDataManager to set a target 
    /// game object active or inactive when loading a game (or when applying persistent data
    /// between level changes).
    /// </summary>
    /// <remarks>
    /// Inactive game objects don't receive messages. Don't add this component to an inactive game 
    /// object. Instead, add it to a "manager" object and set the target to the object that you 
    /// want to activate or deactivate.
    /// </remarks>
    [AddComponentMenu("")] // Use wrapper.
    public class PersistentActiveData : MonoBehaviour
    {

        /// <summary>
        /// The target game object.
        /// </summary>
        [Tooltip("根据下方 Condition 设置为激活或未激活的 GameObject。")]
        public GameObject target;

        /// <summary>
        /// If this condition is <c>true</c>, the target game object is activated; otherwise it's deactivated.
        /// </summary>
        [Tooltip("若为 true，则激活 Target；否则停用。")]
        public Condition condition;

        /// <summary>
        /// When the script starts, check the condition and set the target GameObject active/inactive.
        /// </summary>
        [Tooltip("脚本启动时检查 condition，并设置目标 GameObject 为激活/未激活。否则只会在加载游戏或从其他场景进入时检查。")]
        public bool checkOnStart;

        protected virtual void Start()
        {
            if (checkOnStart) Check();
        }

        protected virtual void OnEnable()
        {
            PersistentDataManager.RegisterPersistentData(gameObject);
        }

        protected virtual void OnDisable()
        {
            PersistentDataManager.UnregisterPersistentData(gameObject);
        }

        /// <summary>
        /// Listens for an OnApplyPersistentData message from the PersistentDataManager, and sets a target
        /// game object accordingly.
        /// </summary>
        public void OnApplyPersistentData()
        {
            Check();
        }

        public virtual void Check()
        {
            if (enabled)
            {
                if (target == null)
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: No target is assigned to Persistent Active Data component on " + name + ".", this);
                }
                else
                {
                    target.SetActive(condition.IsTrue(null));
                }
            }
        }

    }

}
