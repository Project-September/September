using Fusion;
using September.Common;

namespace September.InGame.Rules.ScorePolicy
{
    public class DefaultScorePolicy : IGameResultScorePolicy
    {
        public int GetScore(PlayerRef player)
        {
            return PlayerDatabase.Instance.PlayerDataDic.Get(player).Score;
        }
    }
}
