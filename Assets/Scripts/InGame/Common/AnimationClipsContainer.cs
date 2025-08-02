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
        
        [SerializeField] private AnimationClip[] _animationClips;
        
        public static AnimationClipsContainer Instance { get; private set; }
        public AnimationClip[] AnimationClips => _animationClips;

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
}
