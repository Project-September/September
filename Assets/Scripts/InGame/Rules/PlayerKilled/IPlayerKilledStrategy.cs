using Fusion;

namespace September.InGame.Rules
{
    /// <summary>
    /// プレイヤーをキルした際の処理
    /// </summary>
    public interface IPlayerKilledStrategy
    {
        public void ProcessKillEvent(PlayerRef killer, PlayerRef victim);
    }
}
