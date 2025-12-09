using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// 这是一个Timeline Clip（片段）的数据定义脚本
[Serializable]
public class DOTweenPathClip : PlayableAsset, ITimelineClipAsset
{
    // 这里的模板引用了我们将要写的Behaviour脚本
    public DOTweenPathBehaviour template = new DOTweenPathBehaviour();

    // 实现ITimelineClipAsset接口，定义Clip的能力
    public ClipCaps clipCaps
    {
        // 支持循环、混合（尽管路径混合比较复杂，这里先开启基础能力）
        get { return ClipCaps.Looping | ClipCaps.Blending; }
    }

    // 创建Playable的方法，这是Timeline运行时的工厂方法
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DOTweenPathBehaviour>.Create(graph, template);
        return playable;
    }
}