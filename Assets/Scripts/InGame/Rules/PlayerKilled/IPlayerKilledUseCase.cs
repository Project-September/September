using Fusion;

namespace September.InGame.Rules
{
    /// <summary>
    /// プレイヤーをキルした際の処理を定義するユースケース
    /// </summary>
    public interface IPlayerKilledUseCase
    {
        public void ProcessKillEvent(PlayerRef killer, PlayerRef victim);
    }
}
