using UnityEngine;

namespace InGame.Player
{
    [CreateAssetMenu(fileName = "PlayerParameter", menuName = "Scriptable Objects/Player/PlayerParameter")]
    public class PlayerParameter : ScriptableObject
    {
        [SerializeField] int _health;
        [SerializeField] float _speed;
        [SerializeField] float _stamina;
        [SerializeField] private float _staminaConsumption;
        [SerializeField] float _staminaRegen;
        [SerializeField] int _attackDamage;
        [Header("無敵時間")]
        [SerializeField] private float _invincibilityTime;
        
        public int Health => _health;
        public float Speed => _speed;
        public float Stamina => _stamina;
        public float StaminaRegen => _staminaRegen;
        public int AttackDamage => _attackDamage;
        public float InvincibilityTime => _invincibilityTime;
    }
}
