using System;
using September.InGame.Common.Stats;
using UnityEngine;

namespace InGame.Player
{
    /// <summary>
    /// プレイヤーステータスの定義データを格納するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerParameter", menuName = "Scriptable Objects/Player/PlayerParameter")]
    public class PlayerParameter : ScriptableObject
    {
        [SerializeField] int _health;
        [SerializeField] float _speed;
        [SerializeField] float _stamina;
        [SerializeField] float _staminaConsumption;
        [SerializeField] float _staminaRegen;
        [SerializeField] int _attackDamage;
        
        public int Health => _health;
        public float Speed => _speed;
        public float Stamina => _stamina;
        public float StaminaRegen => _staminaRegen;
        public float StaminaConsumption => _staminaConsumption;
        public int AttackDamage => _attackDamage;

        public StatsContainer GetStats()
        {
            Span<Stat> stats = stackalloc Stat[]
            {
                new(StatType.Health, Health, maxValue: Health),
                new(StatType.Speed, Speed),
                new(StatType.Stamina, Stamina, maxValue: Stamina),
                new(StatType.StaminaRegen, StaminaRegen),
                new(StatType.StaminaConsumption, StaminaConsumption),
                new(StatType.AttackDamage, AttackDamage),
            };
            
            return new StatsContainer(stats);
        }
    }
}
