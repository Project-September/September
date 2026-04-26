using System.Collections.Generic;
using Fusion;
using September.InGame.Common.Stats;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerStatus : StatsManager
    {
        [SerializeField] private PlayerParameter _params;
        
        protected override StatsContainer GetInitialStats()
        {
            MaxHealth = _params.Health;
            MaxStamina = _params.Stamina;
            
            return _params.GetStats();
        }

        protected override void OnStatsUpdated(HashSet<StatType> updatedStats)
        {
            foreach (StatType statType in updatedStats)
            {
                switch (statType)
                {
                    case StatType.Health:
                        CurrentHealth = (int)CurrentStats.GetStatValue(StatType.Health);
                        break;
                    case StatType.Stamina:
                        CurrentStamina = CurrentStats.GetStatValue(StatType.Stamina);
                        break;
                    case StatType.StaminaRegen:
                        StaminaRegen = CurrentStats.GetStatValue(StatType.StaminaRegen);
                        break;
                    case StatType.StaminaConsumption:
                        StaminaConsumption = CurrentStats.GetStatValue(StatType.StaminaConsumption);
                        break;
                    case StatType.Speed:
                        Speed = CurrentStats.GetStatValue(StatType.Speed);
                        break;
                    case StatType.AttackDamage:
                        AttackDamage = CurrentStats.GetStatValue(StatType.AttackDamage);
                        break;
                }
            }
        }

        [Networked] public int MaxHealth { get; private set; }
        [Networked] public int CurrentHealth { get; private set; }
        [Networked] public float MaxStamina { get; private set; }
        [Networked] public float CurrentStamina { get; private set; }
        [Networked] public float StaminaRegen { get; private set; }
        [Networked] public float StaminaConsumption { get; private set; }
        [Networked] public float Speed { get; private set; }
        [Networked] public float AttackDamage { get; private set; }
    }
}