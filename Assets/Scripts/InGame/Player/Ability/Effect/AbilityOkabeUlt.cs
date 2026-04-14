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
        
        private PlayerStatus _playerStatus;
        private EffectSpawner _effectSpawner;
        private string _effectID;

        protected override void OnCutInEnd()
        {
            Debug.Log("[AbilityOkabeUlt] Start");
            var player = Parameter.Owner;

            if (_playerStatus || player.TryGetComponent(out _playerStatus))
            {
                _playerStatus.AddEffect(_buffEffect);
            }
            
            _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            _effectID = Guid.NewGuid().ToString();
            _effectSpawner?.RequestPlayLoopEffect(_effectID, _effectType, player.transform.position, Quaternion.identity, player.transform);
        }

        protected override void OnUpdateUlt(float deltaTime)
        {
            if (TimeSinceCutInEnd > _duration)
            {
                RequestEndAbility();
            }
        }

        protected override void OnEndUlt()
        {
            _playerStatus?.RemoveEffect(_buffEffect);
            _effectSpawner?.StopEffect(_effectID);
        }
    }
}