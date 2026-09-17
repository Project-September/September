using Common.Extensions;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace InGame.Common
{
    /// <summary>
    /// 同一レイヤー上の2クリップを保持し、呼び出し側から補間比率を制御できるPlayable。
    /// </summary>
    internal sealed class ControllableLayerBlend
    {
        private const double EndEpsilon = 0.01d;

        private readonly AnimationMixerPlayable _mixer;
        private readonly AnimationClipPlayable _firstPlayable;
        private readonly AnimationClipPlayable _secondPlayable;

        public bool IsValid => _mixer.IsValid();

        public bool IsComplete => IsClipComplete(_firstPlayable) && IsClipComplete(_secondPlayable);

        public ControllableLayerBlend(
            PlayableGraph graph,
            AnimationLayerMixerPlayable layerMixer,
            int layerSlot,
            AnimationClip firstClip,
            AnimationClip secondClip,
            bool applyFirstFootIk,
            bool applySecondFootIk,
            float speed)
        {
            _mixer = AnimationMixerPlayable.Create(graph, 2);
            _firstPlayable = CreateClipPlayable(graph, firstClip, applyFirstFootIk, speed);
            _secondPlayable = CreateClipPlayable(graph, secondClip, applySecondFootIk, speed);

            _mixer.ConnectInput(0, _firstPlayable, 0);
            _mixer.ConnectInput(1, _secondPlayable, 0);
            layerMixer.ConnectInput(layerSlot, _mixer, 0);
        }

        public void SetBlendWeight(float weight)
        {
            if (!IsValid) return;

            var clampedWeight = Mathf.Clamp01(weight);
            _mixer.SetInputWeight(0, 1f - clampedWeight);
            _mixer.SetInputWeight(1, clampedWeight);
        }

        public void Destroy(AnimationLayerMixerPlayable layerMixer, int layerSlot)
        {
            if (!IsValid) return;

            var root = (Playable)_mixer;
            var currentInput = layerMixer.GetInput(layerSlot);
            if (currentInput.IsValid() && currentInput.GetHandle() == root.GetHandle())
                layerMixer.DisconnectInput(layerSlot);

            root.DestroyTree();
        }

        private static AnimationClipPlayable CreateClipPlayable(
            PlayableGraph graph,
            AnimationClip clip,
            bool applyFootIk,
            float speed)
        {
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(applyFootIk);
            playable.SetTime(0d);
            playable.SetDuration(clip.length);
            playable.SetSpeed(speed);
            return playable;
        }

        private static bool IsClipComplete(AnimationClipPlayable playable)
        {
            return !playable.IsValid() || playable.GetTime() >= playable.GetDuration() - EndEpsilon;
        }
    }
}
