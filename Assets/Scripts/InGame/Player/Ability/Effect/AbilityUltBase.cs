using System;
using Fusion;
using InGame.Player.Ult;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    [Serializable]
    public abstract class AbilityUltBase : AbilityBase
    {
        [SerializeField] private float _startEffectTime;

        private CutInAnimatorBase _cutInAnimator;
        private PlayerHealth _playerHealth;
        private PlayerManager _playerManager;

        /// <summary>
        /// カットイン終了まで待機するタイマー
        /// </summary>
        private TickTimer _cutInEndTimer;

        /// <summary>
        /// 効果発動まで待機するタイマー
        /// </summary>
        private TickTimer _startEffectTimer;

        private bool _isCutInEnd;
        private bool _isEffectTriggered;

        /// <summary>
        /// 直近のカットイン終了からの経過時間
        /// </summary>
        public float TimeSinceCutInEnd
        {
            get
            {
                if (!_isCutInEnd) return 0;

                var elapsedTick = Runner.Tick - (_cutInEndTimer.TargetTick ?? 0);
                if (elapsedTick < 0) return 0;
                
                return elapsedTick * Runner.DeltaTime;
            }
        }

        protected sealed override void OnStart()
        {
            Debug.Log("<color=yellow>[AbilityUlt]</color> Start");
            
            var player = Parameter.Owner;
            
            if (!_playerHealth) _playerHealth = player.GetComponent<PlayerHealth>();
            if (!_cutInAnimator) _cutInAnimator = player.GetComponent<CutInAnimatorBase>();
            if (!_playerManager) _playerManager = player.GetComponent<PlayerManager>();
            
            // アビリティが発動したら条件をリセットする
            if (player.TryGetComponent<IUltCondition>(out var condition))
            {
                condition.OnUltActivated();
            }

            // 無敵化と入力ロック
            if (_playerHealth) _playerHealth.IsInvincible = true;
            if (_playerManager) _playerManager.RPC_SetControlState(PlayerManager.PlayerControlState.InputLocked);

            OnCutInStart();

            if (_cutInAnimator)
            {
                _cutInAnimator.RequestPlayCutInAnimation();
                _cutInEndTimer = TickTimer.CreateFromSeconds(Runner, (float)_cutInAnimator.Duration);
                _startEffectTimer = TickTimer.CreateFromSeconds(Runner, _startEffectTime);
            }
            else
            {
                Debug.LogWarning($"[AbilityUlt] CutInAnimatorが存在しないため、カットインをスキップします", Parameter.Owner);
                OnCutInEnd();
            }
        }

        protected sealed override void OnUpdate(float deltaTime)
        {
            Debug.Log($"{_startEffectTimer.RemainingTicks(Runner)} {_startEffectTimer.Expired(Runner)} {_startEffectTimer.IsRunning}");

            if (!_isEffectTriggered && _startEffectTimer.Expired(Runner))
            {
                Debug.Log("<color=yellow>[AbilityUlt]</color> Start Effect");

                OnStartEffect();
                _isEffectTriggered = true;
            }

            // カットイン終了待ち
            if (!_cutInEndTimer.Expired(Runner)) return;

            // カットインが終了したフレームだけ処理する
            if (!_isCutInEnd)
            {
                Debug.Log("<color=yellow>[AbilityUlt]</color> CutIn End", Parameter.Owner);
                // 無敵解除と入力ロック解除
                if (_playerHealth) _playerHealth.IsInvincible = false;
                if (_playerManager) _playerManager.RPC_SetControlState(PlayerManager.PlayerControlState.Normal);

                _isCutInEnd = true;
                OnCutInEnd();
                
                return;
            }
            
            // カットイン終了済みなら必殺技効果の更新処理
            OnUpdateUlt(deltaTime);
        }

        protected sealed override void OnEndAbility()
        {
            Debug.Log($"<color=yellow>[AbilityUlt]</color> End {StartTick} {Runner.Tick} {Runner.Tick - StartTick} {(Runner.Tick - StartTick) * Runner.DeltaTime}", Parameter.Owner);
            _isCutInEnd = false;
            _isEffectTriggered = false;
            OnEndUlt();
        }

        protected virtual void OnCutInStart() { }

        /// <summary> カットインが終了した瞬間の処理 </summary>
        protected abstract void OnCutInEnd();
        
        /// <summary> カットイン終了後の更新処理 </summary>
        protected virtual void OnUpdateUlt(float deltaTime) { }
        
        /// <summary> 必殺技終了時の処理 </summary>
        protected virtual void OnEndUlt() { }

        protected virtual void OnStartEffect() { }
    }
}
