using Fusion;
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
        [SerializeField] private CameraController _cameraController;
        
        [Networked] private float Rotate { get; set; }
        
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
            
            // 元のプレイヤーオブジェクトを非表示にする
            var playerManager = playerObject.GetComponent<PlayerManager>();
            if (playerManager != null) playerManager.RPC_SetInvisible(true);
        }

        /// <summary>
        /// ネットワーク上での入力処理
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            if (GetInput<PlayerInput>(out var input))
            {
                Rotate += input.MoveDirection.x;
            }
            
            transform.Rotate(0, Rotate, 0);
        }
    }
}