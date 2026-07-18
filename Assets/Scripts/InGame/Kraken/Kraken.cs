using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using InGame.Interact;
using InGame.Player;
using September.Common;
using September.Common.Input;
using September.InGame.Kraken.Animations;
using September.InGame.Kraken.Attack;
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
        [SerializeField] private float _hitRayCastHeight;
        [SerializeField] private float _hitStartTime;
        [SerializeField] private float _hitEndTime;
        [SerializeField] private int _damage;
        [SerializeField] private LayerMask _hitGroundLayer;
        [SerializeField] private HitChecker _armHitChecker;
        [SerializeField] private float _armStartTime;
        [SerializeField] private float _armEndTime;

        [Header("攻撃予測設定")]
        [SerializeField] private AttackPrediction _attackPrediction;
        [SerializeField] private Vector3 _predictionSize;
        [SerializeField] private float _predictionEndTime;

        [Header("アニメーション設定")]
        [SerializeField] private KrakenAttackAnimationHandler _tentacleAnimator;

        [Header("インタラクト設定")]
        [SerializeField] private InteractableBase _interactable;

        [Header("ダメージ設定")]
        [SerializeField] private int _dealScore = 10;

        private InputWrapper _attack;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private Vector3 _originalPlayerPosition;
        private Quaternion _originalPlayerRotation;

        private Vector3 _targetPosition;

        private readonly HashSet<Collider> _alreadyHits = new();

        [Networked] private bool IsAttacking { get; set; }

        private void Start()
        {
            _armHitChecker.OnHit += OnHitCheckerOnOnHit;
            _hitChecker.OnHit += OnHitCheckerOnOnHit;

            _initialPosition = transform.position;
            _initialRotation = transform.rotation;

            _cameraController.Init(true);

            return;

            void OnHitCheckerOnOnHit(Collider hitCollider)
            {
                if (_alreadyHits.Contains(hitCollider)) return;

                float rayWorldHeight = _hitRayCastHeight + transform.position.y;
                Vector3 hitPos = hitCollider.ClosestPoint(transform.position);
                Vector3 rayOrigin = new(hitPos.x, rayWorldHeight, hitPos.z);
                float distance = rayWorldHeight - hitPos.y;

                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, distance, _hitGroundLayer))
                {
                    Debug.DrawRay(rayOrigin, Vector3.down * distance, Color.red, 100f);
                    return;
                }

                Debug.DrawRay(rayOrigin, Vector3.down * distance, Color.green, 100f);

                _alreadyHits.Add(hitCollider);

                IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    var hitData = new HitData { HitActionType = HitActionType.Damage, Amount = _damage, ExecutorRef = Object.InputAuthority, TargetRef = damageable.OwnerPlayerRef };

                    damageable.TakeHit(ref hitData);
                }
            }
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

            _interactable.ForceSetInteractable = false;

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
            if (!HasStateAuthority) return;
            if (IsAttacking) return;

            if (GetInput<PlayerInput>(out var input))
            {
                // transform.Rotate(0, 10 * input.MoveDirection.x * Runner.DeltaTime, 0);

                _attack.SetInput(input.Buttons.IsSet(PlayerButtons.Attack));

                if (_attack.IsJustPressed)
                {
                    Camera mainCamera = Camera.main;
                    if (mainCamera == null) return;
                    Vector3 origin = mainCamera.transform.position;
                    Vector3 forward = mainCamera.transform.forward;
                    if (Physics.Raycast(origin, forward, out RaycastHit hit, Mathf.Infinity))
                    {
                        RPC_Attack(hit.point);
                    }
                    else
                    {
                        RPC_Attack(forward * 20f);
                    }
                    IsAttacking = true;
                }
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_Attack(Vector3 targetPosition)
        {
            Attack(targetPosition).Forget();
        }

        public async UniTask Attack(Vector3 targetPosition)
        {
            Debug.Log("Start Attack");
            Debug.DrawLine(transform.position, targetPosition, Color.green, 10f);
            DebugDrawUtility.DrawWireSphere(targetPosition, 3f, Color.green, 10f);

            _alreadyHits.Clear();

            var task = _tentacleAnimator.Attack(targetPosition);
            {
                Vector3 dir = targetPosition - _tentacleAnimator.LatestArmRootPosition;
                dir.y = 0;
                Quaternion lookRotation = Quaternion.LookRotation(dir);
                _attackPrediction.Show(new AttackPredictionShape(targetPosition, _predictionSize, lookRotation));
                UniTask.WaitForSeconds(_predictionEndTime).ContinueWith(() => _attackPrediction.Hide());
            }

            await UniTask.WhenAll(
                HitCheck(_hitChecker, _hitStartTime, _hitEndTime),
                HitCheck(_armHitChecker, _armStartTime, _armEndTime));

            IsAttacking = false;

            Debug.Log("End Attack");
        }

        public async UniTask HitCheck(HitChecker hitChecker, float startTime, float endTime)
        {
            await UniTask.WaitForSeconds(startTime);
            hitChecker.StartHitCheck();
            await UniTask.WaitForSeconds(endTime - startTime);
            hitChecker.EndHitCheck();
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
