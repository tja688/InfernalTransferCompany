using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using DG.Tweening; // 引入DOTween

// 定义轨道颜色，这里用一种接近DOTween Logo的绿色
[TrackColor(0.0f, 0.6f, 0.2f)]
// 指定这个轨道绑定的组件类型是 DOTweenPath
[TrackBindingType(typeof(DOTweenPath))]
// 指定这个轨道能接受哪种Clip
[TrackClipType(typeof(DOTweenPathClip))]
public class DOTweenPathTrack : TrackAsset
{
    // 创建轨道的混合器（Mixer）
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<DOTweenPathMixerBehaviour>.Create(graph, inputCount);
    }
}

// ---------------------------------------------------------
// 混合器逻辑 (Mixer Behaviour) - 这是真正控制动画进度的核心类
// ---------------------------------------------------------
public class DOTweenPathMixerBehaviour : PlayableBehaviour
{
    // 每一帧都会执行
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        DOTweenPath pathComponent = playerData as DOTweenPath;

        if (pathComponent == null) return;

        // 1. 获取 Tween 对象
        Tween tween = pathComponent.GetTween();

        // 运行时保护：如果还没初始化，尝试初始化
        if (tween == null && Application.isPlaying)
        {
            pathComponent.DOPlay(); 
            tween = pathComponent.GetTween();
        }
        
        if (tween == null || !tween.IsActive()) return;
        

        // 2. 强制暂停
        // 我们完全通过 Timeline 的进度来“采样”动画，不需要它自己跑
        tween.Pause();

        // 3. 计算 Timeline 传入的加权时间
        int inputCount = playable.GetInputCount();
        float finalTargetTime = 0f;
        bool hasActiveClip = false;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            if (inputWeight > 0f)
            {
                ScriptPlayable<DOTweenPathBehaviour> inputPlayable = (ScriptPlayable<DOTweenPathBehaviour>)playable.GetInput(i);
                
                double clipTime = inputPlayable.GetTime();
                double clipDuration = inputPlayable.GetDuration();

                // 归一化时间 (0~1)
                float normalizedTime = (float)(clipTime / clipDuration);
                finalTargetTime += normalizedTime * inputWeight;
                hasActiveClip = true;
            }
        }

        // 4. 应用时间到 Tween
        if (hasActiveClip)
        {
            float tweenDuration = tween.Duration(false); 
            float absoluteTime = finalTargetTime * tweenDuration;

            // ---------------------------------------------------------------
            // 【关键修复 2】：andPlay 参数改为 false
            // true = 跳转后自动播放（导致抖动和乱跑）
            // false = 跳转后保持静止（这才是你要的“预览/scrubbing”效果）
            // ---------------------------------------------------------------
            tween.Goto(absoluteTime, false); 
        }
    }
}