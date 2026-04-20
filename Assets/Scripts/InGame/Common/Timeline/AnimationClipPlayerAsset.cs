using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace InGame.Common.Timeline
{
    [Serializable]
    public class AnimationClipPlayerAsset : PlayableAsset, ITimelineClipAsset, IPropertyPreview
    {
        [SerializeField] private AnimationClipPlayerPlayable _playable;
        
        public ClipCaps clipCaps => ClipCaps.None;

        public override double duration => _playable.Clip == null ? base.duration : _playable.Clip.length;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AnimationClipPlayerPlayable>.Create(graph, _playable);
            return playable;
        }

        // 書いてみたけどよくわかってない
#if UNITY_EDITOR
        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            var player = director.GetGenericBinding(this) as AnimationClipPlayer;
            if (player == null) return;

            var clip = _playable.Clip;
            if (clip == null) return;

            driver.AddFromClip(clip);
        }
#endif
    }
}