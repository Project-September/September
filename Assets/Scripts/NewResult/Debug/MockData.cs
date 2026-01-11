using System.Linq;
using September.Common;
using UnityEngine;

namespace September.NewResult
{
    public class MockData
    {
        private readonly RankingEntryData[] _data;

        [System.Serializable]
        public struct RankingEntryData
        {
            public int _rank;
            public string _playerName;
            public CharacterType _characterType;

            public RankingEntry Convert()
            {
                return new RankingEntry(_rank, _playerName);
            }
        }
        
        public MockData(RankingEntryData[] data)
        {
            _data = data.OrderBy(x => x._rank).ToArray();
        }

        public GameResultInfo Create(ExhibitScoreConfig config)
        {
            var builder = new GameResultInfoBuilder();
            builder.SetStageName("Field");

            var playerCount = _data.Length;
            var ogrePlayerIndex = playerCount - 1;
            var localPlayerIndex = Random.Range(0, playerCount);

            var db = Enumerable.Range(0, playerCount).Select((_, i) =>
                {
                    var exhibitInteractCounts =
                        config.Entries
                            .Select(x => new { key = x.Type, value = Random.Range(0, 10) })
                            .ToDictionary(x => x.key, x => x.value);

                    var score = exhibitInteractCounts.Select((x, i) => x.Value * config.Entries[i].Points).Sum();
                    return (
                        score,
                        exhibitInteractCounts,
                        isOgre: ogrePlayerIndex == i,
                        isSelf: localPlayerIndex == i
                        );
                }
            );
            var ordered = db.OrderByDescending(x => x.score).ToArray();
            var ranking = ordered.Where(x => !x.isOgre).Concat(ordered.Where(x => x.isOgre)).ToArray();
            
            for (var i = 0; i < ranking.Length; i++)
            {
                var data = ranking[i];
                var player = new PlayerResultInfoBuilder();
                player.SetPlayerName(_data[i]._playerName);
                player.SetCharacterType(_data[i]._characterType);
                player.SetTotalScore(data.score);
                player.SetScoreConfig(config.Entries);
                player.SetIsOgre(data.isOgre);
                player.SetIsSelf(data.isSelf);
                player.SetExhibitInteractCounts(data.exhibitInteractCounts);
                builder.AddPlayer(player.BuildInstance());
            }

            var gameResultInfo = builder.BuildInstance();
            return gameResultInfo;
        }
    }
}