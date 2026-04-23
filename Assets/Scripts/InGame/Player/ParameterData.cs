using InGame.Common;
using UnityEngine;

namespace InGame.Player
{
    [CreateAssetMenu(fileName = "ParameterData", menuName = "Scriptable Objects/Player/ParameterData")]
    public class ParameterData : ScriptableObject
    {
        [SerializeField] private StatType _type;
        [SerializeField] private float _defaultValue;
        [SerializeField] private float _maxValue;
        [SerializeField] private float _minValue;
        
        public float DefaultValue => _defaultValue;
        public float MaxValue => _maxValue;
        public float MinValue => _minValue;
        public StatType  Type => _type;
    }
}
