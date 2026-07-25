using September.InGame.Rules.ScorePolicy;
using September.NewResult.RankingPolicy;
using UnityEngine;

namespace September.InGame.Rules
{
    public interface IGameRule
    {
        public IGameStartStrategy GameStartStrategy { get; }
        public IPlayerKilledStrategy PlayerKilledStrategy { get; }
        public IRankingPolicy RankingPolicy { get; }
        public IGameResultScorePolicy GameResultScorePolicy { get; }
    }

    [CreateAssetMenu(fileName = "GameRule", menuName = "September/Game Rule")]
    public class GameRule : ScriptableObject, IGameRule
    {
        [SerializeReference, SubclassSelector, Header("ゲーム開始時の処理")] private IGameStartStrategy _gameStartStrategy;
        [SerializeReference, SubclassSelector, Header("プレイヤーキル時の処理")] private IPlayerKilledStrategy _playerKilledStrategy;
        [SerializeReference, SubclassSelector, Header("順位付けルール")] private IRankingPolicy _rankingPolicy;
        [SerializeReference, SubclassSelector, Header("スコア付けルール")] private IGameResultScorePolicy _gameResultScorePolicy;

        public IGameStartStrategy GameStartStrategy => _gameStartStrategy;
        public IPlayerKilledStrategy PlayerKilledStrategy => _playerKilledStrategy;
        public IRankingPolicy RankingPolicy => _rankingPolicy;
        public IGameResultScorePolicy GameResultScorePolicy => _gameResultScorePolicy;
    }
}
