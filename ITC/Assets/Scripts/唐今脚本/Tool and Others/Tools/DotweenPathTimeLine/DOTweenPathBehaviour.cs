using System;
using UnityEngine;
using UnityEngine.Playables;
using DG.Tweening; // 引入DOTween命名空间
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;

// 这是一个PlayableBehaviour，负责具体的逻辑处理
[Serializable]
public class DOTweenPathBehaviour : PlayableBehaviour
{
    // 用来存储运行时对应的DOTween对象
    private Tween _tween;
    private bool _isPausedByTimeline = false;
    
    // 初始化标记
    private bool _initialized = false;

    // 当Timeline开始播放这个Graph时调用
    public override void OnGraphStart(Playable playable)
    {
        base.OnGraphStart(playable);
        _initialized = false;
        _isPausedByTimeline = false;
    }

    // 当Timeline停止播放时调用
    public override void OnGraphStop(Playable playable)
    {
        base.OnGraphStop(playable);
        // 为了安全，可以在这里恢复Tween的原始状态，或者保持现状
        // 如果需要在Timeline结束时销毁Tween，可以在这里写
    }

    // 核心逻辑：Timeline每一帧都会调用这个方法（Mixer模式下）
    // 注意：我们将逻辑主要放在MixerBehaviour中处理会更适合多Clip混合
    // 但为了简单实现单Clip控制，我们可以配合Mixer使用。
    // *为了代码架构最简化且有效，逻辑将主要在下面的 MixerBehaviour 中实现*
    // 这个类目前作为一个纯粹的数据传递者存在即可。
}