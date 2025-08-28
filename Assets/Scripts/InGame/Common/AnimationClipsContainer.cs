using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace InGame.Common
{
    // AnimationClipPlayer で使用する AnimationClip 共有用
    [CreateAssetMenu(fileName = "AnimationClipsContainer", menuName = "Scriptable Objects/AnimationClipsContainer")]
    public class AnimationClipsContainer : ScriptableObject
    {
        private const string AssetPath = "AnimationClipsContainer";
        
        [SerializeField] private AnimationMontage[] _animationMontages;
        
        public static AnimationClipsContainer Instance { get; private set; }
        public AnimationMontage[] AnimationMontages => _animationMontages;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static async void Init()
        {
            try
            {
                Instance = await Addressables.LoadAssetAsync<AnimationClipsContainer>(AssetPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    [Serializable]
    public struct AnimationMontage
    {
        public AnimationClip AnimClip;
        [Header("Blend")]
        public Blend BlendIn;
        public Blend BlendOut;
        
        [Header("Layer Meta")]
        public AvatarMask AvatarMask;
        public bool IsAdditive;
        public bool UseRootMotion;

        [Serializable]
        public struct Blend
        {
            public float BlendTime;
            public AnimationCurve BlendCurve;
        }
    }
}
