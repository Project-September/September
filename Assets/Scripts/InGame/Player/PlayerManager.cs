using System.Net.NetworkInformation;
using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using InGame.Common;
using InGame.Health;
using Ingame.Tanihira;
using September.Common;
using UnityEngine;
using PlayerInput = September.Common.PlayerInput;

namespace InGame.Player
{
    /// <summary>
    /// Playerのどこまでの機能を入れるかは未定
    /// </summary>
    public class PlayerManager : NetworkBehaviour, IAfterTick
    {
        [SerializeField] GameObject _colliderObj;
        [SerializeField] GameObject _meshObj;
        [SerializeField] private float _stunTime; // PlayerParameter に入れるべきか
        [SerializeField] private Vector3 _respawnPosition;
        
        PlayerMovement _playerMovement;
        CameraController _cameraController;
        PlayerHealth _playerHealth;
        AnimationClipPlayerManager _animationClipPlayerManager;
        PlayerControlState _playerControlState = PlayerControlState.Normal;
        TickTimer _stunTickTimer;
        /// <summary>
        /// 無敵時間のタイマー
        /// </summary>
        private TickTimer _invincibleTickTimer;
        PlayerEffectController _playerEffectController;
        Rigidbody _rigidbody;
        private bool _shouldWarp = false;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _isVaultingLastFrame = false;
        public PlayerControlState CurrentPlayerControlState => _playerControlState;

        /// <summary>
        /// 気絶時間
        /// </summary>
        private float _sTimer;

        public void SetWarpTarget(Vector3 targetPosition,Quaternion targetRotation)
        {
            if(!HasStateAuthority)
                return;
            
            _targetPosition = targetPosition;
            _targetRotation = targetRotation;
            _shouldWarp = true;
        }
        
        public bool IsLocalPlayer => HasInputAuthority;
        
        [Networked] private NetworkButtons PreviousButtons { get; set; }
        [Networked, HideInInspector] public NetworkBool IsStun { get; private set; }
        
        private PlayerStatus _playerStatus;

        public override void Spawned()
        {
            InitComponents();
            
            _respawnPosition = transform.position;
            _playerStatus = GetComponent<PlayerStatus>();
            
            //気絶時間の設定
            var animF = _animationClipPlayerManager.FaintAnim.length / _animationClipPlayerManager.PlaybackSpeed;
            var animG = _animationClipPlayerManager.GetUpAnim.length / _animationClipPlayerManager.PlaybackSpeed;
            var finalTime = animF + animG;
            _sTimer = finalTime;
        }

        /// <summary> Player関連コンポーネントの初期化 </summary>
        void InitComponents()
        {
            _playerMovement = GetComponent<PlayerMovement>();
            _playerEffectController = GetComponentInChildren<PlayerEffectController>();
            _rigidbody = GetComponent<Rigidbody>();
            if (TryGetComponent(out CameraController cameraController))
            {
                _cameraController = cameraController;
                cameraController.Init(IsLocalPlayer);
            }

            if (TryGetComponent(out PlayerHealth health))
            {
                _playerHealth = health;
                health.OnDeath += OnDeath;
            }

            if (TryGetComponent(out AnimationClipPlayerManager anim))
            {
                _animationClipPlayerManager = anim;
            }
        }

        private void LateUpdate()
        {
            // if (_animationClipPlayer)
            // {
            //     var maxSpeed = _playerMovement.DashMoveSpeed;
            //     var walkSpeed = _playerMovement.WalkSpeed;
            //     var moveSpeed = _playerMovement._moveVelocity.magnitude;
            //     var weight = 0f;
            //     if (moveSpeed <= walkSpeed)
            //     {
            //         // 0..Walk -> 0..1
            //         weight = Mathf.InverseLerp(0f, walkSpeed, moveSpeed);
            //     }
            //     else
            //     {
            //         // Walk..Max -> 1..2
            //         weight = Mathf.InverseLerp(walkSpeed, maxSpeed, moveSpeed) + 1f;
            //     }
            //     
            //     _animationClipPlayer.SetLocoWeight(weight);
            //     
            //     switch ()
            //     {
            //         case false when _playerMovement.DoingVault:
            //             _animationClipPlayer.SetTopPriorityClip(true);
            //             break;
            //         case true when !_playerMovement.DoingVault:
            //             _animationClipPlayer.SetTopPriorityClip(false);
            //             break;
            //     }
            // }
            
            // Localでの処理にInputを送る
            if (HasInputAuthority)
            {
                if (GameInput.I.Player.Aim.triggered)
                {
                    _cameraController.CameraReset();
                }
                
                _cameraController.RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(), Time.deltaTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority)
            {
                if (_stunTickTimer.Expired(Runner) && IsStun)
                {
                    Restart();
                }
                
                //無敵時間終了の判定
                if (_invincibleTickTimer.Expired(Runner) && _playerHealth.IsInvincible && !IsStun)
                {
                    InvincibilityEnd();
                }
            }
            
            // プレイヤーの入力の管理
            if (GetInput<PlayerInput>(out var input))
            {
                if (!IsStun && _playerControlState == PlayerControlState.Normal)
                {
                    // player movement に入力を与えて更新する
                    _playerMovement.UpdateMovement(input.MoveDirection, input.Buttons.IsSet(PlayerButtons.Dash),
                        input.CameraYaw, input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Jump), Runner.DeltaTime);
                }
                
                _playerMovement.MoveTick(Runner.DeltaTime);

                if (input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Warp))
                {
                    Respawn();
                }
            }

            if (_shouldWarp)
            {
                transform.position = _targetPosition;
                transform.rotation = _targetRotation;
                _cameraController.CameraReset();
                _shouldWarp = false;
            }

            
        }

        public void AfterTick()
        {
            PreviousButtons = GetInput<PlayerInput>().GetValueOrDefault().Buttons;
        }

        /// <summary> 気絶が終わったとき </summary>
        void Restart()
        {
            IsStun = false;
            _playerEffectController.StopStunEffect();
            
            //気絶終了後、無敵時間を設ける
            _invincibleTickTimer = TickTimer.CreateFromSeconds(Runner, _playerStatus.InvincibilityTime);
        }
        
        /// <summary>
        /// 無敵時間終了後
        /// </summary>
        private void InvincibilityEnd()
        {
            _playerHealth.IsInvincible = false;
            Debug.LogWarning("無敵時間終了");
        }

        void OnDeath(HitData lastHitData)
        {
            IsStun = true;
            
            _stunTickTimer = TickTimer.CreateFromSeconds(Runner, _sTimer);
            _playerHealth.IsInvincible = true;
            _playerEffectController.PlayStunEffect();
        }

        public void SetControlState(PlayerControlState controlState)
        {
            _playerControlState = controlState;

            if (_playerControlState == PlayerControlState.ForcedControl)
            {
                _playerMovement.Stop();
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_SetColliderActive(NetworkBool active)
        {
            _colliderObj.SetActive(active);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_SetMeshActive(NetworkBool active)
        {
            _meshObj.SetActive(active);
        }
        
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_SetUseGrav(NetworkBool active)
        {
           _rigidbody.useGravity = active;
        }

        /// <summary> 非常用リスポーン </summary>
        void Respawn()
        {
            if (!HasStateAuthority) return;
            
            _playerMovement.Teleport(_respawnPosition);
            
            //タニヒラ用の処理を追記
            if (this.gameObject.TryGetComponent<FormationManager>(out FormationManager formationManager))
            {
                formationManager.WarpFriendNearPlayer(_respawnPosition, Quaternion.identity);
            }
        }

        /// <summary> スタンの経過時間を取得する </summary>
        public float GetRemainingStunTime => _stunTickTimer.RemainingTime(Runner) ?? 0;
        
        public enum PlayerControlState
        {
            Normal,
            InputLocked,
            ForcedControl
        }
    }
}
