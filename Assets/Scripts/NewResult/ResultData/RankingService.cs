using System.Collections.Generic;
using System.Linq;

namespace September.NewResult
{
    /// <summary>
    /// 順位決めのルールを適用するサービス
    /// </summary>
    public class RankingService
    {
        public RankingEntry[] Create(IEnumerable<PlayerResultEntry> playerResultInfo)
        {
            var ordered = playerResultInfo.OrderByDescending(x => x.TotalScore).ToArray();
            ordered = ordered.Where(x => !x.IsOgre).Concat(ordered.Where(x => x.IsOgre)).ToArray();

            var ranking = new RankingEntry[ordered.Length];
            for (var i = 0; i < ordered.Length; i++)
            {
                var rank = i + 1;
                var playerName = ordered[i].PlayerName;
                var characterType = ordered[i].CharacterType;
                var rankingEntry = new RankingEntry(rank, playerName, characterType);
                ranking[i] = rankingEntry;
            }
            
            return ranking;
        }
    }
}