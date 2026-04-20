using UnityEngine.Timeline;

namespace InGame.Common.Timeline
{
    [TrackBindingType(typeof(AnimationClipPlayer))]
    [TrackClipType(typeof(AnimationClipPlayerAsset))]
    public class AnimationClipPlayerTrack : TrackAsset
    {
    }
}