using UnityEngine;
using Animancer;

public class PrototypeLauncher : MonoBehaviour
{
    [Header("核心组件")]
    public AnimancerComponent animancer;   // 播放器

    [Header("动效资产")]
    // 用 PlayableAssetTransition 来包住 Timeline
    [SerializeField] private PlayableAssetTransition moveAnimation;

    [Header("测试控制")]
    public KeyCode triggerKey = KeyCode.Space;

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            PlayAnimation();
        }
    }

    void PlayAnimation()
    {
        // 通过 Transition 播放 Timeline
        var state = animancer.Play(moveAnimation);

        // 重置时间
        state.Time = 0;

        Debug.Log("播放 Timeline 动效！");
    }
}