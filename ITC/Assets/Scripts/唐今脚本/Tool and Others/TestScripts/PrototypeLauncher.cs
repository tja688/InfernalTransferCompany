using UnityEngine;
using UnityEngine.Playables; // 必须引用这个命名空间
using UnityEngine.Timeline;

public class PrototypeLauncher : MonoBehaviour
{
    [Header("核心组件")]
    // public AnimancerComponent animancer; // 暂时不用 Animancer
    public PlayableDirector director;       // 改用 Unity 原生的 Timeline 播放器

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
        // 如果 Timeline 已经在播放，重置到开头
        if (director.state == PlayState.Playing)
        {
            director.time = 0;
        }
        
        // 播放
        director.Play();
        
        Debug.Log("使用 PlayableDirector 播放 Timeline！");
    }
}