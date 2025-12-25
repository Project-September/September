using System.Collections.Generic;
using Result;
using Random = UnityEngine.Random;

namespace September.NewResult
{
    public class PlayerResultInfoBuilder
    {
        private readonly List<ExhibitScoreEntry> _entries;
        private string _playerName;

        public void SetScoreConfig(ExhibitScoreEntry[] scoreConfig)
        {
            _entries.AddRange(scoreConfig);
        }

        public void SetPlayerName(string playerName)
        {
            _playerName = playerName;
        }
        
        public PlayerResultEntry BuildInstance()
        {
            var entries = new ResultExhibitScoreEntry[_entries.Count];
            
            // インタラクトできる種類とスコアを取得
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var type = entry.Type;
                var count = InGameResultContainer.ExhibitInteractCounts?.GetValueOrDefault(type, 0) ??
                            Random.Range(0, 10);
                var score = count * entry.Points;

                entries[i] = new ResultExhibitScoreEntry(type, count, score);
            }

            return new PlayerResultEntry(_playerName, entries);
        }
    }
}