using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace InGame.Common.Timeline
{
    [Serializable]
    public class AnimationClipPlayerAsset : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private AnimationClipPlayerPlayable _playable;

        public AnimationClipPlayerPlayable Playable => _playable;

        public ClipCaps clipCaps => ClipCaps.None;

        public override double duration => _playable.Clip == null ? base.duration : _playable.Clip.length;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AnimationClipPlayerPlayable>.Create(graph, _playable);
            return playable;
        }
    }
}