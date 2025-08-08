using System;
using Fusion;
using InGame.Common;
using InGame.Health;
using September.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = September.Common.PlayerInput;

namespace InGame.Player
{
    public class PlayerManager : NetworkBehaviour, IAfterTick
    {
        [SerializeField] GameObject _colliderObj;
        [SerializeField] GameObject _meshObj;
        [SerializeField] private float _stunTime;

        PlayerMovement _playerMovement;
        CameraController _cameraController;
        PlayerHealth _playerHealth;
        PlayerControlState _playerControlState = PlayerControlState.Normal;
        TickTimer _stunTickTimer;

        private bool _shouldWarp = false;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;

        // ▼▼ 追加：Kartサンプル同様にTick整合のためNetworked入力を保持
        [Networked] private PlayerInput NetInput { get; set; }

        // ▼▼ 追加：ワープ後にRenderで見た目/カメラを処理するフラグ
        private bool _pendingWarpVisualSnap;

        public void SetWarpTarget(Vector3 targetPosition, Quaternion targetRotation)
        {
            if (!HasStateAuthority) return;
            _targetPosition = targetPosition;
            _targetRotation = targetRotation;
            _shouldWarp = true;
        }

        public bool IsLocalPlayer => HasInputAuthority;

        [Networked] private NetworkButtons PreviousButtons { get; set; }
        [Networked, HideInInspector] public NetworkBool IsStun { get; private set; }

        public override void Spawned()
        {
            InitComponents();
        }

        void InitComponents()
        {
            _playerMovement = GetComponent<PlayerMovement>();

            if (TryGetComponent(out CameraController cameraController))
            {
                if (!HasInputAuthority)
                {
                    // InputAuthorityがない場合はカメラを無効化
                    cameraController.enabled = false;
                }
                _cameraController = cameraController;
                cameraController.Init(IsLocalPlayer);
            }

            if (TryGetComponent(out PlayerHealth health))
            {
                _playerHealth = health;
                health.OnDeath += OnDeath;
            }

            if (TryGetComponent(out PlayerAnimBase playerAnimBase))
            {
                playerAnimBase.Init(this);
            }
        }

        private void LateUpdate()
        {
            if (HasInputAuthority)
            {
                if (GameInput.I.Player.Aim.triggered)
                    _cameraController.CameraReset();

                _cameraController.RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(), Time.deltaTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            // ▼ スタン終了チェック（権威のみ）
            if (HasStateAuthority)
            {
                if (_stunTickTimer.Expired(Runner) && IsStun)
                    Restart();
            }

            // ▼ 入力をNetworkedにミラー（Kartと同じ発想）
            if (GetInput<PlayerInput>(out var input))
                NetInput = input;

            // ▼ 物理更新は毎Tick、Networked入力を使って決定（全ピア同一）
            if (!IsStun && _playerControlState == PlayerControlState.Normal)
            {
                // 既存のUpdateMovementをそのまま使う場合：
                _playerMovement.UpdateMovement(
                    NetInput.MoveDirection,
                    NetInput.Buttons.IsSet(PlayerButtons.Dash),
                    NetInput.CameraYaw,
                    NetInput.Buttons.WasPressed(PreviousButtons, PlayerButtons.Jump),
                    Runner.DeltaTime
                );
            }

            // ▼ ワープは権威のみで確定 → 全員に可視スナップ通知
            if (_shouldWarp && HasStateAuthority)
            {
                transform.SetPositionAndRotation(_targetPosition, _targetRotation);
                _shouldWarp = false;

                // 見た目はRender側でスナップさせる（ローカルはカメラもリセット）
                RPC_NotifyWarp();
            }
        }

        // ▼ Render側で可視スナップ＆ローカルのカメラリセット（Kartの「見た目は後段で」の考え方）
        public override void Render()
        {
            if (_pendingWarpVisualSnap)
            {
                // 見た目補間スクリプトにスナップAPIがあるならここで呼ぶ想定：
                // var interp = GetComponent<RigidbodyVisualInterpolator>();
                // interp?.SnapNow();

                if (HasInputAuthority)
                    _cameraController?.CameraReset();

                _pendingWarpVisualSnap = false;
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_NotifyWarp()
        {
            _pendingWarpVisualSnap = true;
        }

        public void AfterTick()
        {
            // ▼ 安全にPreviousButtonsを更新（KartではNetworkedに状態を寄せる運用）
            if (GetInput<PlayerInput>(out var input))
                PreviousButtons = input.Buttons;
        }

        void Restart()
        {
            IsStun = false;
            _playerHealth.IsInvincible = false;
        }

        void OnDeath(HitData lastHitData)
        {
            IsStun = true;
            _stunTickTimer = TickTimer.CreateFromSeconds(Runner, _stunTime);
            _playerHealth.IsInvincible = true;
        }

        public void SetControlState(PlayerControlState controlState)
        {
            _playerControlState = controlState;

            if (_playerControlState == PlayerControlState.ForcedControl)
                _playerMovement.Stop();
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_SetColliderActive(NetworkBool active) => _colliderObj.SetActive(active);

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_SetMeshActive(NetworkBool active) => _meshObj.SetActive(active);

        public float GetRemainingStunTime => _stunTickTimer.RemainingTime(Runner) ?? 0;

        public enum PlayerControlState { Normal, InputLocked, ForcedControl }
    }
}
