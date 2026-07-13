using Fusion;
using September.Common;

namespace September.InGame.Rules
{
    /// <summary>
    /// 鬼ごっこルールにおけるプレイヤーキル時のユースケース
    /// </summary>
    public class TagPlayerKilledUseCase : IPlayerKilledUseCase
    {
        public void ProcessKillEvent(PlayerRef killer, PlayerRef victim)
        {
            SessionPlayerData killerData = PlayerDatabase.Instance.PlayerDataDic.Get(killer);
            var killedData = PlayerDatabase.Instance.PlayerDataDic.Get(victim);
            if (killerData.IsOgre && killer != victim)
            {
                killerData.IsOgre = false;
                PlayerDatabase.Instance.PlayerDataDic.Set(killer, killerData);
                killedData.IsOgre = true;
                PlayerDatabase.Instance.PlayerDataDic.Set(victim, killedData);
                PlayerDatabase.Instance.Server_AddOgreCount(victim);
            }

            if (killer == victim)
                return;

            UpdateStunData(killer, killerData, victim);
        }

        private void UpdateStunData(PlayerRef killerRef, SessionPlayerData killerData, PlayerRef killedPlayer)
        {
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
