#if UNITY_EDITOR
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace InGame.Common.Timeline.Editor
{
    /// <summary>
    /// AnimationClipの差し替えを検知し、Timelineのクリップの名前を合わせるためのエディタ
    /// </summary>
    [CustomTimelineEditor(typeof(AnimationClipPlayerAsset))]
    public class AnimationClipPlayerAssetEditor : ClipEditor
    {
        public override void OnClipChanged(TimelineClip clip)
        {
            var asset = clip.asset as AnimationClipPlayerAsset;
            UpdateClip(asset);
        }

        private static void UpdateClip(AnimationClipPlayerAsset asset)
        {
            var director = TimelineEditor.inspectedDirector;
            if (director == null) return;

            var timeline = director.playableAsset as TimelineAsset;
            if (timeline == null) return;

            foreach (var track in timeline.GetOutputTracks())
            {
                foreach (var clip in track.GetClips())
                {
                    if (clip.asset == asset)
                    {
                        if (asset.Playable.Clip == null)
                        {
                            clip.displayName = "Empty";
                        }
                        else
                        {
                            clip.displayName = asset.Playable.Clip.name;
                        }

                        return;
                    }
                }
            }
        }
    }
}
#endif