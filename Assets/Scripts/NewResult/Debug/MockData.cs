using System.Linq;
using September.Common;
using September.NewResult.RankingPolicy;
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
        }
        
        public MockData(RankingEntryData[] data)
        {
            _data = data.OrderBy(x => x._rank).ToArray();
        }

        public GameResultInfo Create(ExhibitScoreConfig config, IRankingPolicy rankingPolicy)
        {
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

            var players = new PlayerResultEntry[ranking.Length];
            for (var i = 0; i < ranking.Length; i++)
            {
                var data = ranking[i];
                players[i] = new PlayerResultEntry(
                    _data[i]._playerName,
                    _data[i]._characterType,
                    ResultExhibitScoreEntryUtility.CalcScoreEntries(config, data.exhibitInteractCounts),
                    data.score,
                    data.isOgre,
                    data.isSelf,
                    Random.Range(0, 1000), Random.Range(0, 1000),
                    data.exhibitInteractCounts.Values.Sum(),
                    Random.Range(0, 10)
                    );
            }

            var gameResultInfo = new GameResultInfo("Field", players);
            return gameResultInfo;
        }
    }
}
