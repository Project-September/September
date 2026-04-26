using September.InGame.Common.Stats;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerStatus : StatsManager
    {
        [SerializeField] private PlayerParameter _params;
        
        protected override StatsContainer GetInitialStats()
        {
            return _params.GetStats();
        }

        public int MaxHealth => (int)CurrentStats.GetStat(StatType.Health).MaxValue;
        public int CurrentHealth => (int)CurrentStats.GetStatValue(StatType.Health);
        public float MaxStamina => CurrentStats.GetStat(StatType.Stamina).MaxValue;
        public float CurrentStamina => CurrentStats.GetStatValue(StatType.Stamina);
        public float StaminaRegen => CurrentStats.GetStatValue(StatType.StaminaRegen);
        public float StaminaConsumption => CurrentStats.GetStatValue(StatType.StaminaConsumption);
        public float Speed => CurrentStats.GetStatValue(StatType.Speed);
        public float AttackDamage => CurrentStats.GetStatValue(StatType.AttackDamage);
    }
}