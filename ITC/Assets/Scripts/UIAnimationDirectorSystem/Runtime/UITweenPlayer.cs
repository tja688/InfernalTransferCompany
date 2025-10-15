using System;
using UnityEngine;
using UnityEngine.Events;

namespace DirectorUI
{
    /// <summary>
    /// Lightweight player that bridges <see cref="UITransitionTicket"/> data to the existing goal-driven tween system.
    /// </summary>
    [DisallowMultipleComponent]
    public class UITweenPlayer : MonoBehaviour
    {
        /// <summary>
        /// 播放一個動畫預設，並在動畫完成時調用 onComplete 回調。
        /// </summary>
        public void Play(GameObject target, UITweenPreset preset, Action onComplete = null)
        {
            if (target == null || preset == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (!target.TryGetComponent(out global::UITweenPlayer player))
            {
                Debug.LogWarning($"[DirectorUI] GameObject '{target.name}' 缺少 UITweenPlayer 組件。", target);
                onComplete?.Invoke();
                return;
            }

            if (onComplete == null)
            {
                player.Play(preset);
                return;
            }

            UnityAction handler = null;
            handler = () =>
            {
                player.onComplete.RemoveListener(handler);
                onComplete.Invoke();
            };

            player.onComplete.AddListener(handler);
            player.Play(preset);
        }
    }
}
