using System.Collections.Generic;
using Result;
using September.Common;
using Random = UnityEngine.Random;

namespace September.NewResult
{
    public class PlayerResultInfoBuilder
    {
        private readonly List<ExhibitScoreEntry> _entries = new();
        private IReadOnlyDictionary<ExhibitType, int> _exhibitInteractCounts;
        private string _playerName;
        private CharacterType _characterType;
        private int _totalScore;
        private bool _isOgre;
        private bool _isSelf;
        
        private int _damageDealt;
        private int _damageReceived;
        private int _ogreCount;
        private int _totalInteractCount;

        public void SetExhibitInteractCounts(IReadOnlyDictionary<ExhibitType, int> exhibitInteractCounts)
        {
            _exhibitInteractCounts = exhibitInteractCounts;
        }

        public void SetScoreConfig(IReadOnlyList<ExhibitScoreEntry> scoreConfig)
        {
            _entries.AddRange(scoreConfig);
        }

        public void SetPlayerName(string playerName)
        {
            _playerName = playerName;
        }

        public void SetCharacterType(CharacterType characterType)
        {
            _characterType = characterType;
        }

        public void SetTotalScore(int totalScore)
        {
            _totalScore = totalScore;
        }

        public void SetIsOgre(bool isOgre)
        {
            _isOgre = isOgre;
        }

        public void SetIsSelf(bool isSelf)
        {
            _isSelf = isSelf;
        }

        public void SetDamage(int damageDealt, int damageReceived)
        {
            _damageDealt = damageDealt;
            _damageReceived = damageReceived;
        }

        public void SetOgreCount(int ogreCount)
        {
            _ogreCount = ogreCount;
        }

        public void SetTotalInteractCount(int totalInteractCount)
        {
            _totalInteractCount = totalInteractCount;
        }
        
        public PlayerResultEntry BuildInstance()
        {
            var entries = new ResultExhibitScoreEntry[_entries.Count];

            // インタラクトできる種類とスコアを取得
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var type = entry.Type;
                var count = _exhibitInteractCounts.GetValueOrDefault(type, 0);
                var score = count * entry.Points;

                entries[i] = new ResultExhibitScoreEntry(type, count, score);
            }

            return new PlayerResultEntry(_playerName, _characterType, _totalScore, entries, _isOgre, _isSelf, _damageDealt, _damageReceived, _totalInteractCount, _ogreCount);
        }
    }
}