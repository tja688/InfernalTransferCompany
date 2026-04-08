// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This is the base class for all deprecated dialogue event trigger components.
    /// </summary>
    public abstract class DialogueEventStarter : MonoBehaviour
    {

        /// <summary>
        /// Set <c>true</c> if this event should only happen once.
        /// </summary>
        [Tooltip("仅在此场景实例中触发一次，然后销毁此组件。注意：这不会在场景切换或存档之间保持持久。它只适用于当前场景实例。若要让某件事在玩家整个通关过程中只发生一次（包括场景切换和存档），请使用持久化数据组件。")]
        public bool once = false;

        protected virtual bool useOnce { get { return true; } }

        protected void DestroyIfOnce()
        {
            if (once) Destroy(this);
        }

    }

}
