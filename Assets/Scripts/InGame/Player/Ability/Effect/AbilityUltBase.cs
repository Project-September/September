using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    [Serializable]
    public abstract class AbilityUltBase : AbilityBase
    {
        [Header("カットイン設定")]
        [SerializeField] private float _cutInDuration = 1f; // 仮

        private PlayerHealth _playerHealth;
        
        protected sealed override void OnStart()
        {
            Debug.Log("[AbilityUlt] OnStart");
            
            if (!_playerHealth)
            {
                _playerHealth = Parameter.Owner.GetComponent<PlayerHealth>();
            }
            
            PlayCutIn().Forget();
        }

        protected async UniTask PlayCutIn()
        {
            if (_playerHealth) _playerHealth.IsInvincible = true;
            await UniTask.WaitForSeconds(_cutInDuration);
            if (_playerHealth) _playerHealth.IsInvincible = false;
            OnCutInEnd();
        }
        
        protected abstract void OnCutInEnd();
    }
}