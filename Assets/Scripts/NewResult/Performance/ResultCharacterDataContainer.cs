using System;
using System.Linq;
using Fusion;
using September.Common;
using UnityEngine;

namespace September.NewResult
{
    [CreateAssetMenu(fileName = "ResultCharacterDataContainer", menuName = "ScriptableObjects/ResultCharacterDataContainer", order = 0)]
    public class ResultCharacterDataContainer : ScriptableObject
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
        
        public CharacterType Type => _type;
        public ResultPerformanceState ResultCharacterPrefab => _resultCharacterPrefab;
        public Sprite Icon => _icon;
        
        [SerializeField] private string _testString;
        public string TestString => _testString;
    }
}