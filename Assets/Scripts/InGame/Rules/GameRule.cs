using UnityEngine;

namespace September.InGame.Rules
{
    public interface IGameRule
    {
        public IGameStartStrategy GameStartStrategy { get; }
        public IPlayerKilledUseCase PlayerKilledUseCase { get; }
    }

    [CreateAssetMenu(fileName = "GameRule", menuName = "September/Game Rule")]
    public class GameRule : ScriptableObject, IGameRule
    {
        [SerializeReference, SubclassSelector, Header("ゲーム開始時の処理")] private IGameStartStrategy _gameStartStrategy;
        [SerializeReference, SubclassSelector, Header("プレイヤーキル時の処理")] private IPlayerKilledUseCase _playerKilledUseCase;

        public IGameStartStrategy GameStartStrategy => _gameStartStrategy;
        public IPlayerKilledUseCase PlayerKilledUseCase => _playerKilledUseCase;
    }
}
