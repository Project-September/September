using System;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace September.InGame.Kraken
{
    public class Kraken : NetworkBehaviour
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
        [SerializeField] private Transform _leg;
        [SerializeField] private Animator _animator;
        
        [Networked] private float Rotate { get; set; }

        private void Start()
        {
            _hitChecker.OnHit += x =>
            {
                if (x.TryGetComponent<IDamageable>(out var damageable))
                {
                    var hitData = new HitData()
                    {
                        HitActionType = HitActionType.Damage,
                        Amount = _damage,
                        ExecutorRef = Object.InputAuthority,
                        TargetRef = damageable.OwnerPlayerRef,
                    };
                    
                    damageable.TakeHit(ref hitData);
                }
            };
        }

        /// <summary>
        /// ローカルでの入力処理
        /// </summary>
        private void LateUpdate()
        {
            // ローカル以外は弾く
            if(!HasInputAuthority) return;
            
            if (GameInput.I.Player.Aim.triggered)
            {
                _cameraController.CameraReset();
            }
            
            _cameraController.RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(),Runner.DeltaTime);
        }
        
        /// <summary>
        /// ネットワーク上での入力処理
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            if (GetInput<PlayerInput>(out var input))
            {
                Rotate += input.MoveDirection.x;

                if (input.Buttons.IsSet(PlayerButtons.Attack))
                {
                    if (Physics.Raycast(Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)), out var hit))
                    {
                        var targetPos = hit.point;
                        Attack(targetPos).Forget();
                    }
                }
            }
            
            transform.Rotate(0, Rotate, 0);
        }
        
        /// <summary>
        /// 指定のプレイヤーをクラーケンにする
        /// </summary>
        /// <param name="owner"></param>
        public void GetOn(PlayerRef owner)
        {
            // カメラを有効化する
            if (HasInputAuthority)
            {
                _cameraController.Init(true);
                _cameraController.SetCameraPriority(CameraPriority);
            }
            
            // 元のプレイヤーオブジェクトを取得
            var playerObject = Runner.GetPlayerObject(owner);
            if (playerObject == null) return;
            
            // 元のプレイヤーオブジェクトを非表示にする
            var playerManager = playerObject.GetComponent<PlayerManager>();
            if (playerManager != null) playerManager.RPC_SetInvisible(true);
        }

        public async UniTask Attack(Vector3 targetPos)
        {
            _leg.position = targetPos;
            await UniTask.WaitForSeconds(_hitStartTime);
            _hitChecker.StartHitCheck();
            await UniTask.WaitForSeconds(_hitEndTime - _hitStartTime);
            _hitChecker.EndHitCheck();
        }
    }
}