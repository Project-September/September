using System;
using Cysharp.Threading.Tasks;
using InGame.Player.Ult;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    [Serializable]
    public abstract class AbilityUltBase : AbilityBase
    {
        private CutInAnimatorBase _cutInAnimator;
        private PlayerHealth _playerHealth;
        
        protected sealed override void OnStart()
        {
            Debug.Log("[AbilityUlt] OnStart");
            
            var player = Parameter.Owner;
            
            if (!_playerHealth) _playerHealth = player.GetComponent<PlayerHealth>();
            if (!_cutInAnimator) _cutInAnimator = player.GetComponent<CutInAnimatorBase>();
            
            PlayCutIn().Forget();
        }

        private async UniTask PlayCutIn()
        {
            if (_playerHealth) _playerHealth.IsInvincible = true;

            if (_cutInAnimator)
            {
                var token = Parameter.Owner.destroyCancellationToken;
                await _cutInAnimator.PlayCutInAnimation(token);
            }
            
            if (_playerHealth) _playerHealth.IsInvincible = false;
            
            OnCutInEnd();
        }
        
        protected abstract void OnCutInEnd();
    }
}