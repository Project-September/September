using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InGame.Common
{
    // AnimationClipPlayer で使用する AnimationClip 共有用
    [CreateAssetMenu(fileName = "AnimationClipsContainer", menuName = "Scriptable Objects/AnimationClipsContainer")]
    public class AnimationClipsContainer : ScriptableObject
    {
        private const string AssetPath = "AnimationClipsContainer";
        
        [SerializeField] private AnimationMontageStruct[] _animationMontages;
        
        public static AnimationClipsContainer Instance { get; private set; }
        public AnimationMontageStruct[] AnimationMontages => _animationMontages;

        #if UNITY_EDITOR
        [InitializeOnLoadMethod]
        #else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        #endif
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
    public struct AnimationMontageStruct
    {
        public AnimationClip AnimClip;
        public float PlaySpeed;
        [Header("Blend")]
        public LayerInfo.Blend BlendIn;
        public LayerInfo.Blend BlendOut;
        
        [Header("Layer Meta")]
        public LayerInfo.LayerType TargetLayer;
        public bool IsAdditive;

        public bool _loop;

        public AnimationMontageStruct(float playSpeed = 1)
        {
            AnimClip = null;
            PlaySpeed = playSpeed;
            BlendIn = new LayerInfo.Blend();
            BlendOut = new LayerInfo.Blend();
            TargetLayer = LayerInfo.LayerType.Base;
            IsAdditive = false;
            
            _loop = false;
        }
    }
}
