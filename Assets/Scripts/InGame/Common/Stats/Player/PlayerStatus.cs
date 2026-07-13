using September.InGame.Common.Stats;
using UnityEngine;

namespace InGame.Player
{
    /// <summary>
    /// プレイヤーステータスを管理するコンポーネント
    /// </summary>
    public class PlayerStatus : StatsManager
    {
        [SerializeField] private PlayerParameter _params;

        protected override StatsContainer GetInitialStats()
        {
            return _params.GetStats();
        }

        // プレイヤー共通ステータスへのアクセスを便利にする用（なくても良い。他のコンポーネントからも以下のように CurrentStats.GetStat でとってきても良い）
        public int MaxHealth => (int)CurrentStats.GetStat(StatType.Health).MaxValue;
        public int CurrentHealth => (int)CurrentStats.GetStat(StatType.Health).Value;
        public float MaxStamina => CurrentStats.GetStat(StatType.Stamina).MaxValue;
        public float CurrentStamina => CurrentStats.GetStat(StatType.Stamina).Value;
        public float StaminaRegen => CurrentStats.GetStat(StatType.StaminaRegen).Value;
        public float StaminaConsumption => CurrentStats.GetStat(StatType.StaminaConsumption).Value;
        public float Speed => CurrentStats.GetStat(StatType.Speed).Value;
        public float AttackDamage => CurrentStats.GetStat(StatType.AttackDamage).Value;
        public float InteractDurationMultiply => CurrentStats.GetStat(StatType.InteractDurationMultiply).Value;
        public float StunDurationMultiply => CurrentStats.GetStat(StatType.StunDurationMultiply).Value;
        public int Jewelry => (int)CurrentStats.GetStat(StatType.Jewelry).Value;
    }
}