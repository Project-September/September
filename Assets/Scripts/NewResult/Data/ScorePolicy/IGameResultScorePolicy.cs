using Fusion;

namespace September.NewResult.Data.ScorePolicy
{
    public interface IGameResultScorePolicy
    {
        public int GetScore(PlayerRef player);
    }
}
