using System.Collections.Generic;

namespace September.NewResult.RankingPolicy
{
    /// <summary>
    /// 順位付けルール
    /// </summary>
    public interface IRankingPolicy
    {
        /// <summary>
        /// 順位付けルールに応じてRankの値を書き換えます。
        /// </summary>
        /// <param name="playerResultInfo"></param>
        /// <returns></returns>
        public IEnumerable<PlayerResultEntry> Apply(IEnumerable<PlayerResultEntry> playerResultInfo);
    }
}
