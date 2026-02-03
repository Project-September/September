using System;
using System.Linq;
using Fusion;
using September.Common;
using UnityEngine;

namespace September.NewResult
{
    [CreateAssetMenu(fileName = "ResultCharacterAssetsContainer", menuName = "ScriptableObjects/ResultCharacterAssetsContainer", order = 0)]
    public class ResultCharacterAssetsContainer : ScriptableObject
    {
        private const int DataCount = 4;
        
        [SerializeField, ArrayLength(DataCount)] private ResultPerformanceCharacterAssets[] _assets;
        
        public ResultPerformanceCharacterAssets GetAssets(CharacterType characterType)
        {
            return _assets.FirstOrDefault(x => x.Type == characterType);
        }
    }

    [Serializable]
    public struct ResultPerformanceCharacterAssets
    {
        [SerializeField] private CharacterType _type;
        [SerializeField] private ResultPerformanceState _resultCharacterPrefab;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Sprite _resultDetailViewIcon;
        
        public CharacterType Type => _type;
        public ResultPerformanceState ResultCharacterPrefab => _resultCharacterPrefab;
        public Sprite Icon => _icon;
        public Sprite ResultDetailViewIcon => _resultDetailViewIcon;
    }
}