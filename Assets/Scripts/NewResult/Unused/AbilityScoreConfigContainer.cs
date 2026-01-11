using System.Linq;
using September.Common;
using UnityEngine;

namespace September.NewResult
{
    [CreateAssetMenu(fileName = "AbilityScoreConfigContainer", menuName = "ScriptableObjects/AbilityScoreConfigContainer")]
    public class AbilityScoreConfigContainer : ScriptableObject
    {
        [SerializeField] private AbilityScoreConfigTable _abilityScoreConfigs = new();
        
        public ExhibitScoreConfig GetAbilityScoreConfig(CharacterType characterType)
        {
            return _abilityScoreConfigs.GetValueOrDefault(characterType);
        }
    }

    [System.Serializable]
    public class AbilityScoreConfigTable
    {
        [SerializeField] private ScoreConfigTableEntry[] _entries;

        public ExhibitScoreConfig this[CharacterType characterType] => GetValueOrDefault(characterType);

        public ExhibitScoreConfig GetValueOrDefault(CharacterType characterType)
        {
            if (_entries.Any(x => x.CharacterType == characterType))
            {
                var entry = _entries.First(x => x.CharacterType == characterType);
                return entry.ExhibitScoreConfig;
            }
            return ScriptableObject.CreateInstance<ExhibitScoreConfig>();
        }
    }

    [System.Serializable]
    public struct ScoreConfigTableEntry
    {
        [SerializeField] private CharacterType _characterType;
        [SerializeField] private ExhibitScoreConfig _exhibitScoreConfig;
        
        public CharacterType CharacterType => _characterType;
        public ExhibitScoreConfig ExhibitScoreConfig => _exhibitScoreConfig;
    }
}