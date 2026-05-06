using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Video;

namespace September.Common
{
    [CreateAssetMenu(fileName = "Character Data Container", menuName = "ScriptableObjects/CharacterDataContainer")]
    public class CharacterDataContainer : ScriptableObject
    {
        const int DataCount = 4;
        static CharacterDataContainer _instance;
        public static CharacterDataContainer Instance => _instance;
        [Serializable]
        public struct CharacterData
        {
            public CharacterType Type;
            [TextArea(1, 5)] public string DisplayName;
            [TextArea(2, 5)] public string AbilityName;
            [TextArea(3, 5)] public string AbilityExplain;
            public NetworkPrefabRef Prefab;
            public VideoClip ExplainVideo;
            public Sprite CharacterIcon;
            public AnimationClip EmoteAnimation;
            public string SelectedVoice;
            public string StartVoice;
            public string WinningVoice;
            public string AttackVoice;
            public string DamageVoice;
            public string InteractVoice;
            public string UniqueActionVoice;
            public string BGM;
        }
        [SerializeField, ArrayLength(DataCount)] CharacterData[] _characterData;
        // キャラクターと操作説明を紐づけるための辞書
        [SerializeField] SerializableDictionary<CharacterType, ControlDescriptionType> _controlDictionary;

        public static async UniTaskVoid LoadAssetAsync(string path)
        {
            _instance = await Addressables.LoadAssetAsync<CharacterDataContainer>(path);
        }

        public CharacterData GetCharacterData(CharacterType characterType)
        {
            return _characterData.FirstOrDefault(data => data.Type == characterType);
        }

        /// <summary>
        /// CharacterTypeに対応するControlDescriptionTypeを返す
        /// </summary>
        public ControlDescriptionType GetControlDescriptionType(CharacterType characterType)
        {
            if (_controlDictionary.Dictionary.TryGetValue(characterType, out var controlDescriptionType))
            {
                return controlDescriptionType;
            }
            else
            {
                Debug.LogError($"{characterType}に対応する操作説明が紐づけされていません");
                return ControlDescriptionType.Player; // デフォルト値を返す
            }
        }

        public CharacterData GetCharacterData(int index)
        {
            return _characterData[index];
        }

        public string[] GetNames()
        {
            return _characterData.Select(data => data.DisplayName).ToArray();
        }
    }
}