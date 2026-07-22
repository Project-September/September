using Fusion;

namespace September.InGame.Rules.ScorePolicy
{
    public interface IGameResultScorePolicy
    {
        public int GetScore(PlayerRef player);
    }
}
