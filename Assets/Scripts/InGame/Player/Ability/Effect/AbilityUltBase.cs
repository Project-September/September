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
        private PlayerManager _playerManager;

        private float _cutInEndTime;

        public float TimeSinceCutInEnd
        {
            get
            {
                if (_cutInAnimator == null)
                {
                    return 0;
                }

                if (_cutInAnimator.IsCutInAnimationPlaying)
                {
                    return 0;
                }
                
                return ElapsedTime - _cutInEndTime;
            }
        }

        protected sealed override void OnStart()
        {
            Debug.Log("[AbilityUlt] OnStart");
            
            var player = Parameter.Owner;
            
            if (!_playerHealth) _playerHealth = player.GetComponent<PlayerHealth>();
            if (!_cutInAnimator) _cutInAnimator = player.GetComponent<CutInAnimatorBase>();
            if (!_playerManager) _playerManager = player.GetComponent<PlayerManager>();
            
            PlayCutIn().Forget();
        }

        private async UniTask PlayCutIn()
        {
            if (_playerHealth) _playerHealth.IsInvincible = true;
            if (_playerManager) _playerManager.RPC_SetControlState(PlayerManager.PlayerControlState.InputLocked);

            if (_cutInAnimator)
            {
                var token = Parameter.Owner.destroyCancellationToken;
                _cutInAnimator.RequestPlayCutInAnimation();
                await UniTask.WaitUntil(_cutInAnimator, c => !c.IsCutInAnimationPlaying, cancellationToken: token);
            }
            
            if (_playerHealth) _playerHealth.IsInvincible = false;
            if (_playerManager) _playerManager.RPC_SetControlState(PlayerManager.PlayerControlState.Normal);

            _cutInEndTime = ElapsedTime;
            OnCutInEnd();
        }
        
        protected abstract void OnCutInEnd();
    }
}