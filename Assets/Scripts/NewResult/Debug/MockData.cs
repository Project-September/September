using System.Linq;
using September.Common;
using UnityEngine;

namespace September.NewResult
{
    public class MockData
    {
        private readonly RankingEntry[] _data;

        [System.Serializable]
        public struct RankingEntryData
        {
            public int _rank;
            public string _playerName;
            public CharacterType _characterType;

            public RankingEntry Convert()
            {
                return new RankingEntry(_rank, _playerName, _characterType);
            }
        }
        
        public MockData(RankingEntryData[] data)
        {
            _data = data.Select(x => x.Convert()).ToArray();
        }
        
        public GameResultInfo Create(ExhibitScoreConfig config)
        {
            var builder = new GameResultInfoBuilder();
            builder.SetStageName("Field");
            builder.SetRanking(_data);

            var playerCount = _data.Length;
            var ogrePlayerIndex = Random.Range(0, playerCount);
            var localPlayerIndex = Random.Range(0, playerCount);
            for (var i = 0; i < playerCount; i++)
            {
                var exhibitInteractCounts = 
                    config.Entries
                        .Select(x => new { key = x.Type, value = Random.Range(0, 10)})
                        .ToDictionary(x => x.key, x => x.value);

                var score = exhibitInteractCounts.Select((x, i) => x.Value * config.Entries[i].Points).Sum();
                
                var player = new PlayerResultInfoBuilder();
                player.SetPlayerName(_data[i].PlayerName);
                player.SetCharacterType((CharacterType)(i % 4 + 1));
                player.SetTotalScore(score);
                player.SetScoreConfig(config.Entries);
                player.SetIsOgre(i == ogrePlayerIndex);
                player.SetIsSelf(i == localPlayerIndex);
                player.SetExhibitInteractCounts(exhibitInteractCounts);
                builder.AddPlayer(player.BuildInstance());
            }

            var gameResultInfo = builder.BuildInstance();
            return gameResultInfo;
        }
    }
}