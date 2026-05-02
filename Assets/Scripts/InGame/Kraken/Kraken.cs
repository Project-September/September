using Fusion;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace September.InGame.Kraken
{
    public class Kraken : NetworkBehaviour
    {
        private const int CameraPriority = 15;
        
        [SerializeField] private CameraController _cameraController;
        
        [Networked] private float Rotate { get; set; }
        
        private void LateUpdate()
        {
            if(!HasInputAuthority) return;
            
            if (GameInput.I.Player.Aim.triggered)
            {
                _cameraController.CameraReset();
            }
            _cameraController.RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(),Runner.DeltaTime);
        }
        
        public void GetOn(PlayerRef owner)
        {
            if (HasInputAuthority)
            {
                _cameraController.Init(true);
                _cameraController.SetCameraPriority(CameraPriority);
            }
            
            var playerObject = Runner.GetPlayerObject(owner);
            
            var playerManager = playerObject.GetComponent<PlayerManager>();
            if (playerManager != null) playerManager.RPC_SetInvisible(true);
        }

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