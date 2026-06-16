using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using InGame.Player;
using September.Common;
using September.Common.Input;
using September.InGame.Mountable;
using UnityEngine;

namespace September.InGame.Kraken
{
    public class Kraken : NetworkBehaviour, IMountable, IDamageable
    {
        /// <summary> クラーケン中のカメラ優先度 </summary>
        private const int CameraPriority = 15;

        /// <summary> クラーケン時カメラのコントローラー </summary>
        [Header("カメラ")]
        [SerializeField] private CameraController _cameraController;

        /// <summary> 攻撃処理コンポーネント </summary>
        [Header("攻撃設定")]
        [SerializeField] private HitChecker _hitChecker;
        [SerializeField] private float _hitStartTime;
        [SerializeField] private float _hitEndTime;
        [SerializeField] private int _damage;

        [Header("アニメーション設定")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _animationName;

        [Header("インタラクト設定")]
        [SerializeField] private Collider _interactArea;

        [Header("ダメージ設定")]
        [SerializeField] private int _dealScore = 10;

        private InputWrapper _attack;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private Vector3 _originalPlayerPosition;
        private Quaternion _originalPlayerRotation;

        [Networked] private NetworkBool IsAttackTriggered { get; set; }
        private bool _isAttacking;

        private void Start()
        {
            _hitChecker.OnHit += x =>
            {
                if (x.TryGetComponent<IDamageable>(out var damageable))
                {
                    var hitData = new HitData
                    {
                        HitActionType = HitActionType.Damage,
                        Amount = _damage,
                        ExecutorRef = Object.InputAuthority,
                        TargetRef = damageable.OwnerPlayerRef
                    };

                    damageable.TakeHit(ref hitData);
                }
            };

            _initialPosition = transform.position;
            _initialRotation = transform.rotation;

            _cameraController.Init(true);
        }

        /// <summary>
        /// ローカルでの入力処理
        /// </summary>
        private void LateUpdate()
        {
            if (!HasInputAuthority) return;

            if (GameInput.I.Player.Aim.triggered)
            {
                _cameraController.CameraReset();
            }

            _cameraController.RotateCamera(new Vector2(GameInput.I.Player.Look.ReadValue<Vector2>().x, 0), Runner.DeltaTime);
        }

        public bool IsAlive { get; } = true;
        public PlayerRef OwnerPlayerRef { get; private set; }

        public void TakeHit(ref HitData hitData)
        {
            if (HasStateAuthority && OwnerPlayerRef.IsRealPlayer && hitData.HitActionType == HitActionType.Damage)
            {
                PlayerDatabase.Instance.Server_AddKrakenDamageScore(hitData.ExecutorRef, _dealScore);
            }
        }

        /// <summary>
        /// 指定のプレイヤーをクラーケンにする
        /// </summary>
        /// <param name="owner"> </param>
        public void GetOn(PlayerRef owner)
        {
            // カメラを有効化する
            RPC_ResetCamera(owner);
            RPC_SetCameraPriority(owner, CameraPriority);

            // 元のプレイヤーオブジェクトを取得
            var playerObject = Runner.GetPlayerObject(owner);
            if (playerObject == null)
            {
                Debug.LogWarning($"{owner}のプレイヤーオブジェクトが見つかりませんでした");
                return;
            }

            _originalPlayerPosition = playerObject.transform.position;
            _originalPlayerRotation = playerObject.transform.rotation;

            playerObject.transform.position = transform.position;
            playerObject.transform.rotation = transform.rotation;

            // 元のプレイヤーオブジェクトを非表示にする
            if (playerObject.TryGetComponent<PlayerManager>(out var playerManager))
            {
                playerManager.RPC_SetInvisible(true);
            }

            // このプレイヤーから入力を受け取るように設定する
            Object.AssignInputAuthority(owner);

            // インタラクト用の判定を消す
            RPC_SetInteractAreaActive(false);

            OwnerPlayerRef = owner;
        }

        /// <summary>
        /// 指定プレイヤーのクラーケン状態を解除する
        /// </summary>
        public void GetOff(PlayerRef owner)
        {
            // カメラを無効化する
            RPC_SetCameraPriority(owner, 0);

            // 元のプレイヤーオブジェクトを取得
            var playerObject = Runner.GetPlayerObject(owner);
            if (playerObject == null)
            {
                Debug.LogWarning($"{owner}のプレイヤーオブジェクトが見つかりませんでした");
                return;
            }

            playerObject.transform.position = _originalPlayerPosition;
            playerObject.transform.rotation = _originalPlayerRotation;

            // 元のプレイヤーオブジェクトを表示する
            var playerManager = playerObject.GetComponent<PlayerManager>();
            if (playerManager != null) playerManager.RPC_SetInvisible(false);

            // 初期トランスフォームに戻す
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;

            // 入力を受け取らないようにする
            Object.RemoveInputAuthority();

            OwnerPlayerRef = default;
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput<PlayerInput>(out var input))
            {
                // transform.Rotate(0, 10 * input.MoveDirection.x * Runner.DeltaTime, 0);

                _attack.SetInput(input.Buttons.IsSet(PlayerButtons.Attack));

                if (_attack.IsJustPressed)
                {
                    IsAttackTriggered = true;
                }
            }
        }

        public override void Render()
        {
            if (IsAttackTriggered && !_isAttacking)
            {
                _isAttacking = true;
                var forward = Camera.main.transform.forward;
                forward.y = 0;
                forward.Normalize();
                Debug.DrawRay(transform.position, forward * 100f, Color.green, 10f);
                Attack(Vector3.down).Forget();
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetInteractAreaActive(bool active)
        {
            _interactArea.enabled = active;
        }

        private async UniTask Attack(Vector3 targetPos)
        {
            _animator.Play(_animationName, 0, 0f);
            await UniTask.WaitForSeconds(_hitStartTime);
            _hitChecker.StartHitCheck();
            await UniTask.WaitForSeconds(_hitEndTime - _hitStartTime);
            _hitChecker.EndHitCheck();
            _isAttacking = false;
            IsAttackTriggered = false;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ResetCamera(PlayerRef player)
        {
            if (player != Runner.LocalPlayer) return;

            _cameraController.CameraReset();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetCameraPriority(PlayerRef player, int priority)
        {
            if (player != Runner.LocalPlayer) return;

            _cameraController.SetCameraPriority(priority);
        }
    }
}
