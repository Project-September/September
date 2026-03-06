using System.Collections.Generic;
using System.Linq;

namespace September.NewResult
{
    /// <summary>
    /// 順位決めのルールを適用するサービス
    /// </summary>
    public struct RankingService
    {
        public static IEnumerable<PlayerResultEntry> Apply(IEnumerable<PlayerResultEntry> playerResultInfo)
        {
            var ordered = playerResultInfo.OrderByDescending(x => x.TotalScore).ToArray();
            ordered = ordered.Where(x => !x.IsOgre).Concat(ordered.Where(x => x.IsOgre)).ToArray();

            for (var i = 0; i < ordered.Length; i++)
            {
                if (i != 0 && !ordered[i].IsOgre && ordered[i].TotalScore == ordered[i - 1].TotalScore)
                {
                    ordered[i].Rank = ordered[i - 1].Rank;
                }
                else
                {
                    ordered[i].Rank = i + 1;
                }
            }
            
            return ordered;
        }
    }
}