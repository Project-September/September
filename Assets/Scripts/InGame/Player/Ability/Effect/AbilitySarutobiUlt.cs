using System;
using InGame.Player.Sarutobi;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    [Serializable]
    public class AbilitySarutobiUlt : AbilityUltBase
    {
        [Header("必殺技の発動時間")]
        [SerializeField] private float _duration = 10;
        
        [Header("追加するクナイの本数")]
        [SerializeField] private int _addProjectileCount = 2;

        private ThrowKunai _throwKunai;

        protected override void OnCutInEnd()
        {
            var player = Parameter.Owner;
            
            if (!_throwKunai)
                if (!player.TryGetComponent(out _throwKunai))
                {
                    return;
                }
            
            _throwKunai.SetBulletCount(1 + _addProjectileCount);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (TimeSinceCutInEnd > _duration)
            {
                _throwKunai.SetBulletCount(1);
                RequestEndAbility();
            }
        }
    }
}