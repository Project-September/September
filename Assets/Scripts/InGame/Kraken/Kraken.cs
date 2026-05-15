using Fusion;
using InGame.Health;
using InGame.Player;
using September.Common;
using September.Common.Input;
using September.InGame.Mountable;
using UnityEngine;

namespace September.InGame.Kraken
{
    public class Kraken : NetworkBehaviour, IMountable
    {
        /// <summary> クラーケン中のカメラ優先度 </summary>
        private const int CameraPriority = 15;
        
        /// <summary> クラーケン時カメラのコントローラー </summary>
        [Header("カメラ")]
        [SerializeField] private CameraController _cameraController;
        
        /// <summary> 攻撃処理コンポーネント </summary>
        [Header("攻撃設定")]
        [SerializeField] private HitboxCaster _hitboxCaster;
        [SerializeField] private int _damage;

        [Header("アニメーション設定")] 
        [SerializeField] private Transform _leg;
        [SerializeField] private Animator _animator;

        private InputWrapper _attack;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private void Start()
        {
            _hitboxCaster.OnHit += hitCollider =>
            {
                if (hitCollider.TryGetComponent<IDamageable>(out var damageable))
                {
                    var hitData = new HitData
                    {
                        HitActionType = HitActionType.Damage,
                        Amount = _damage,
                        ExecutorRef = Object.InputAuthority,
                        TargetRef = damageable.OwnerPlayerRef,
                    };

                    damageable.TakeHit(ref hitData);
                }
            };

            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            
            _leg.transform.parent = null;
        }

        /// <summary>
        /// ローカルでの入力処理
        /// </summary>
        private void LateUpdate()
        {
            if(!HasInputAuthority) return;
            
            if (GameInput.I.Player.Aim.triggered)
            {
                _cameraController.CameraReset();
            }
            
            _cameraController.RotateCamera(new Vector2(GameInput.I.Player.Look.ReadValue<Vector2>().x, 0) ,Runner.DeltaTime);
        }
        
        public override void FixedUpdateNetwork()
        {
            if (GetInput<PlayerInput>(out var input))
            {
                _attack.SetInput(input.Buttons.IsSet(PlayerButtons.Attack));
                
                transform.Rotate(0, 10 * input.MoveDirection.x * Runner.DeltaTime, 0);

                if (_attack.IsJustPressed)
                {
                    if (Physics.Raycast(Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)), out var hit))
                    {
                        var targetPos = hit.point;
                        Attack(targetPos);
                    }
                }
            }
        }
        
        /// <summary>
        /// 指定のプレイヤーをクラーケンにする
        /// </summary>
        /// <param name="owner"></param>
        public void GetOn(PlayerRef owner)
        {
            // カメラを有効化する
            if (Runner.LocalPlayer == owner)
            {
                _cameraController.Init(true);
                _cameraController.CameraReset();
                _cameraController.SetCameraPriority(CameraPriority);
            }
            
            // 元のプレイヤーオブジェクトを取得
            var playerObject = Runner.GetPlayerObject(owner);
            if (playerObject == null)
            {
                Debug.LogWarning($"{owner}のプレイヤーオブジェクトが見つかりませんでした");
                return;
            }
            
            // 元のプレイヤーオブジェクトを非表示にする
            if (playerObject.TryGetComponent<PlayerManager>(out var playerManager))
            {
                playerManager.RPC_SetInvisible(true);
            }

            // このプレイヤーから入力を受け取るように設定する
            Object.AssignInputAuthority(owner);
        }

        /// <summary>
        /// 指定プレイヤーのクラーケン状態を解除する
        /// </summary>
        public void GetOff(PlayerRef owner)
        {
            // カメラを無効化する
            if (Runner.LocalPlayer == owner)
            {
                _cameraController.CameraReset();
                _cameraController.SetCameraPriority(0);
            }
            
            // 元のプレイヤーオブジェクトを取得
            var playerObject = Runner.GetPlayerObject(owner);
            if (playerObject == null)
            {
                Debug.LogWarning($"{owner}のプレイヤーオブジェクトが見つかりませんでした");
                return;
            }
            
            // 元のプレイヤーオブジェクトを表示する
            var playerManager = playerObject.GetComponent<PlayerManager>();
            if (playerManager != null) playerManager.RPC_SetInvisible(false);
            
            // 初期トランスフォームに戻す
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            
            // 入力を受け取らないようにする
            Object.RemoveInputAuthority();
        }

        private void Attack(Vector3 targetPos)
        {
            _hitboxCaster.StartCast();
        }
    }
}