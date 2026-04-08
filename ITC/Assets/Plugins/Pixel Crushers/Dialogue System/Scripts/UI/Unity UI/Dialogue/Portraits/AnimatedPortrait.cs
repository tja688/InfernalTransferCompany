// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Specifies the animated portrait to use for this actor.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class AnimatedPortrait : MonoBehaviour
    {

        [Tooltip("运行此角色动画肖像的 Animator Controller。它应驱动 Image 组件，而不是 SpriteRenderer。")]
        public RuntimeAnimatorController animatorController;
    }

}