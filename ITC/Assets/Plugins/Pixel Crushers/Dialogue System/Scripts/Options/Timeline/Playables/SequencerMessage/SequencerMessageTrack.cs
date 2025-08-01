// Recompile at 2025/7/1 10:17:08
#if USE_TIMELINE
#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixelCrushers.DialogueSystem
{

    [TrackColor(0.855f, 0.8623f, 0.87f)]
    [TrackClipType(typeof(SequencerMessageClip))]
    [TrackBindingType(typeof(GameObject))]
    public class SequencerMessageTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<SequencerMessageMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
#endif
#endif
