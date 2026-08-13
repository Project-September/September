using System.Collections.Generic;
using System.Linq;

namespace September.NewResult.RankingPolicy
{
    public class JewelryRankingPolicy : IRankingPolicy
    {
        public IEnumerable<PlayerResultEntry> Apply(IEnumerable<PlayerResultEntry> playerResultInfo)
        {
            var ordered = playerResultInfo.OrderByDescending(x => x.TotalScore).ToArray();

            for (var i = 0; i < ordered.Length; i++)
            {
                if (i != 0 && ordered[i].TotalScore == ordered[i - 1].TotalScore)
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
