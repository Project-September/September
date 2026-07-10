using September.NewResult.RankingPolicy;
using UnityEngine;

namespace September.InGame.Rules
{
    public interface IGameRule
    {
        public IGameStartStrategy GameStartStrategy { get; }
        public IPlayerKilledUseCase PlayerKilledUseCase { get; }
        public IRankingPolicy RankingPolicy { get; }
    }

    [CreateAssetMenu(fileName = "GameRule", menuName = "September/Game Rule")]
    public class GameRule : ScriptableObject, IGameRule
    {
        [SerializeReference, SubclassSelector, Header("ゲーム開始時の処理")] private IGameStartStrategy _gameStartStrategy;
        [SerializeReference, SubclassSelector, Header("プレイヤーキル時の処理")] private IPlayerKilledUseCase _playerKilledUseCase;
        [SerializeReference, SubclassSelector, Header("順位付けルール")] private IRankingPolicy _rankingPolicy;

        public IGameStartStrategy GameStartStrategy => _gameStartStrategy;
        public IPlayerKilledUseCase PlayerKilledUseCase => _playerKilledUseCase;
        public IRankingPolicy RankingPolicy => _rankingPolicy;
    }
}
