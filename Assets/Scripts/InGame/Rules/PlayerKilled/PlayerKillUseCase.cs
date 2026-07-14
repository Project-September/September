using Fusion;
using InGame.Health;
using September.Common;

namespace September.InGame.Rules
{
    public class PlayerKillUseCase
    {
        private readonly IPlayerKilledStrategy _strategy;

        public PlayerKillUseCase(IPlayerKilledStrategy strategy)
        {
            _strategy = strategy;
        }

        public void Execute(HitData hitData)
        {
            var killer = hitData.ExecutorRef;
            var victim = hitData.TargetRef;

            _strategy.ProcessKillEvent(killer, victim);

            if (killer == victim) return;

            UpdateStunData(killer, victim);
        }

        private void UpdateStunData(PlayerRef killerRef, PlayerRef killedPlayer)
        {
            var killerData = PlayerDatabase.Instance.PlayerDataDic.Get(killerRef);

            if (killerData.StunData.TryGet(killedPlayer, out int count))
            {
                killerData.StunData.Set(killedPlayer, count + 1);
            }
            else
            {
                killerData.StunData.Set(killedPlayer, 1);
            }

            PlayerDatabase.Instance.PlayerDataDic.Set(killerRef, killerData);

            // スコアの更新処理
            PlayerDatabase.Instance.Server_RecalculateScore(killerRef);
        }
    }
}
