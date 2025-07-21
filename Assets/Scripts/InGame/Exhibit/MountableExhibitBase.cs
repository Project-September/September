using System;
using System.Collections.Generic;
using Fusion;
using InGame.Health;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace InGame.Exhibit
{
    public class MountableExhibitBase : NetworkBehaviour,IDamageable
    {
        protected CameraController CameraController { get; private set; }
        
        protected Animator Animator { get; private set; }

        protected Rigidbody Rigidbody { get; private set; }
        
        #region AttackParam

        protected MeleeHitboxExecutor Executor;
        [SerializeField] private List<Transform> _points;
        [SerializeField] private float _hitboxRadius = 0.2f;
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private int _startFrame;
        [SerializeField] private int _endFrame = 34;
        [SerializeField] private int _damageAmount = 100;
        #endregion
        
        #region DamageableParam
        public bool IsAlive =>_currentHealth > 0;
        public PlayerRef OwnerPlayerRef => Object.InputAuthority;

        private bool _isInvincible ;
    
        private int _currentHealth;
    
        [SerializeField] private int _maxHealth;
        
        #endregion
        
        public override void Spawned()
        {
             Rigidbody = GetComponent<Rigidbody>();
             if (TryGetComponent(out CameraController cameraController))
             {
                 CameraController = cameraController;
                 cameraController.Init(true);
             }
             Animator = GetComponent<Animator>();
             Rigidbody.isKinematic = true;
             _currentHealth = _maxHealth;
        }
        
        private void CreateHitBox(PlayerRef playerRef)
        {
            Executor = new MeleeHitboxExecutor(_points, _hitboxRadius, _hitMask, _startFrame, _endFrame)
            {
                OnHit = collider =>
                {
                    if (collider.TryGetComponent(out IDamageable damageable))
                    {
                        var hitData = new HitData(HitActionType.Damage, _damageAmount, playerRef,
                            damageable.OwnerPlayerRef);
                        Debug.Log($"Hit --> {hitData.TargetRef}に{hitData.Amount}ダメージ");
                        damageable.TakeHit(ref hitData);
                    }
                }
            };
        }
        
        /// <summary>
        /// 展示物に乗ってる間のUpdate関数
        /// </summary>
        public virtual void OnInteractFixedUpdate(PlayerInput playerInput,float deltaTime)
        {
          
        }
        
        private void LateUpdate()
        {
            if(!HasInputAuthority) return;
            
            if (GameInput.I.Player.Aim.triggered)
            {
                CameraController.CameraReset();
            }
            CameraController.RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(),Runner.DeltaTime);
        }
        
        /// <summary>
        /// インタラクト開始時の切り替え処理
        /// ホストでのみ実行される点に注意
        /// </summary>
        public virtual void GetOn(PlayerRef playerRef)
        {
            Object.AssignInputAuthority(playerRef);
            CameraController.Init(true);
            RPC_SetCameraPriority(playerRef,15);
            RPC_SetIsKinematic(playerRef,false);
            CreateHitBox(playerRef);
        }
        
        /// <summary>
        /// インタラクト終了時の切り替え処理
        /// ホストでのみ実行される点に注意
        /// </summary>
        public virtual void GetOff(PlayerRef playerRef)
        {
            Object.RemoveInputAuthority();
            RPC_SetCameraPriority(playerRef,5);
            RPC_SetIsKinematic(playerRef,true);
            Executor = null;
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetCameraPriority(PlayerRef player,int priority)
        {
            if (Runner.LocalPlayer == player)
            {
                CameraController.SetCameraPriority(priority);
            }
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetIsKinematic(PlayerRef player, bool kinematic)
        {
           Rigidbody.isKinematic = kinematic;
        }
        
        public void TakeHit(ref HitData hitData)
        {
            ApplyHit(ref hitData);

            hitData.IsLastHit = !IsAlive;

            if (HasStateAuthority)
            {
                hitData.Executor?.HitExecution(hitData);
            }
        }

        void ApplyHit(ref HitData hitData)
        {
            if (!IsAlive)
            {
                hitData.Amount = 0;
                return;
            }
            if (hitData.HitActionType == HitActionType.Damage)
            {
                hitData.Amount = TakeDamage(hitData.Amount);
            }
            else if (hitData.HitActionType == HitActionType.Heal)
            {
                hitData.Amount = TakeHeal(hitData.Amount);
            }
        }
    
        int TakeDamage(int damage)
        {
            if (_isInvincible) return 0;
            var prevHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth - damage, 0, _maxHealth);
            return prevHealth - _currentHealth;
        }

        int TakeHeal(int heal)
        {
            if (_isInvincible) return 0;
            var prevHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth + heal, 0, _maxHealth);
            return prevHealth - _currentHealth;
        }

    }
}


