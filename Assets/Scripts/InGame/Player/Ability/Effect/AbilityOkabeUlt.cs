using System;
using InGame.Common;
using September.Common;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    [Serializable]
    public class AbilityOkabeUlt : AbilityUltBase
    {
        [SerializeField] private float _duration = 5f;
        [SerializeField] private StatusEffect _buffEffect;
        [SerializeField] private EffectType _effectType;
        
        private EffectSpawner _effectSpawner;
        private string _effectID;

        protected override void OnCutInEnd()
        {
            Debug.Log("[AbilityOkabeUlt] Start");
            var player = Parameter.Owner;

            if (player.TryGetComponent<PlayerStatus>(out var playerStatus))
            {
                var spec = new EffectableStatus.StatusEffectSpec(_buffEffect);
                spec.Duration = _duration;
                playerStatus.AddEffect(spec);
            }
            
            _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            _effectID = Guid.NewGuid().ToString();
            _effectSpawner.RequestPlayLoopEffect(_effectID, _effectType, player.transform.position, Quaternion.identity, player.transform);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (TimeSinceCutInEnd > _duration)
            {
                RequestEndAbility();
            }
        }

        protected override void OnEndAbility()
        {
            Debug.Log($"[AbilityOkabeUlt] End {StartTick} {Runner.Tick} {Runner.Tick - StartTick} {(Runner.Tick - StartTick) * Runner.DeltaTime}");
            _effectSpawner?.StopEffect(_effectID);
        }
    }
}