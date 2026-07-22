using Fusion;
using September.Common;

namespace September.NewResult.Data.ScorePolicy
{
    public class DefaultScorePolicy : IGameResultScorePolicy
    {
        public int GetScore(PlayerRef player)
        {
            return PlayerDatabase.Instance.PlayerDataDic.Get(player).Score;
        }
    }
}
