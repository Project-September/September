using System;
using System.Collections.Generic;
using Fusion;
using InGame.Health;
using InGame.Player;
using Ingame.Tanihira;
using NaughtyAttributes;
using September.Common;
using September.InGame.Common;
using September.InGame.Effect;
using September.InGame.UI;
using UnityEngine;
using UnityEngine.AI;

namespace InGame.Exhibit
{
    public class MountableExhibitBase : NetworkBehaviour,IDamageable
    {
        protected CameraController CameraController { get; private set; }
        
        protected Animator Animator { get; private set; }

        protected Rigidbody Rigidbody { get; private set; }
        
        protected Action HitAction { get; set; }

        protected bool IsSpawned { get; private set; }
        
        #region AttackParam

        protected MeleeHitboxExecutor Executor;
        [SerializeField] private List<Transform> _points;
        [SerializeField] private float _hitboxRadius = 0.2f;
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private int _startFrame;
        [SerializeField] private int _endFrame = 34;
        [SerializeField] private int _damageAmount = 100;
        protected float StartFrame => _startFrame;
        protected float EndFrame => _endFrame;
        #endregion
        
        #region DamageableParam
        public bool IsAlive =>_currentHealth > 0;
        public PlayerRef OwnerPlayerRef => Object.InputAuthority;

        private bool _isInvincible ;
    
        private int _currentHealth;
        
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
    
        [SerializeField] private int _maxHealth;
        
        #endregion
        
        private PlayerManager _ownerPlayerManager;
        
        private Vector3 _hidePosition = new Vector3(0, 0, 0);
        
        [SerializeField, Label("Playerが登場する位置")] private Transform _getOffPoint;
        
        private Collider _collider;

        [Label("Playerに戻るときの地面からの高さ")][SerializeField] private float _height = 10f;
        EffectSpawner _effectSpawner;
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
             IsSpawned = true;
             _initialPosition = transform.position;
             _initialRotation = transform.rotation;
             _collider = gameObject.GetComponentInHierarchy<Collider>();
             if (!_effectSpawner)
                 _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
             if(!Runner.IsServer) return;
             RPC_SetIsKinematic(true);
        }
        
        protected void CreateHitBox(PlayerRef playerRef)
        {
            Executor = new MeleeHitboxExecutor(_points, _hitboxRadius, _hitMask, _startFrame, _endFrame)
            {
                OnHit = (col,pos) =>
                {
                    var damageable = col.GetComponentInParent<IDamageable>();
                    if(col == _collider) return;
                    if (damageable == null) return;
                    var hitData = new HitData(HitActionType.Damage, _damageAmount, playerRef,
                        damageable.OwnerPlayerRef);
                    damageable.TakeHit(ref hitData);
                    _effectSpawner.RequestPlayOneShotEffect(EffectType.HitNormal,col.ClosestPoint(pos), Quaternion.identity);
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
            if(!IsSpawned || !HasInputAuthority) return;
            
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
            _currentHealth = _maxHealth;
            _ownerPlayerManager = StaticServiceLocator.Instance.Get<InGameManager>()
                .PlayerDataDic[playerRef].GetComponent<PlayerManager>();
            _ownerPlayerManager.SetControlState(PlayerManager.PlayerControlState.ForcedControl);
            _ownerPlayerManager.RPC_SetColliderActive(false);
            _ownerPlayerManager.RPC_SetMeshActive(false);
            UIController.I.ChangeDescriptionUI(true);
            Object.AssignInputAuthority(playerRef);
            CameraController.Init(true);
            RPC_SetCameraPriority(playerRef,15);
            RPC_SetIsKinematic(false);
            if (_ownerPlayerManager.TryGetComponent<FormationManager>(out var formationManager))
            {
                formationManager.WarpFriendOutField();
            }
        }
        
        /// <summary>
        /// インタラクト終了時の切り替え処理
        /// ホストでのみ実行される点に注意
        /// </summary>
        public virtual void GetOff(PlayerRef playerRef)
        {
            _ownerPlayerManager.transform.position = _getOffPoint.position;
            _ownerPlayerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
            _ownerPlayerManager.RPC_SetColliderActive(true);
            _ownerPlayerManager.RPC_SetMeshActive(true);
            Object.RemoveInputAuthority();
            UIController.I.ChangeDescriptionUI(false);
            RPC_SetCameraPriority(playerRef,5);
            RPC_SetIsKinematic(true);
            var obj = _ownerPlayerManager.GetComponent<NetworkObject>();
            if (_ownerPlayerManager.TryGetComponent<FormationManager>(out var formationManager))
            {
                formationManager.WarpFriendNearPlayer(obj.transform.position,obj.transform.rotation);
            }
            transform.SetPositionAndRotation(_initialPosition, _initialRotation);
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
        private void RPC_SetIsKinematic(bool kinematic)
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
                HitAction?.Invoke();
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


