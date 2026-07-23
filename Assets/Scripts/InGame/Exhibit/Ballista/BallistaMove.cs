using Fusion;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class BallistaMove : NetworkBehaviour, IProjectileMovement
	{
		[SerializeField] private Transform _barrel;
		[SerializeField] private Transform _rotateBase;
		[SerializeField] private CameraController _cameraController;
		[SerializeField] private LayerMask _layerMask;
		[SerializeField] private float _sens;
		[SerializeField] private float _padSens;
		[SerializeField] private float _playerOffset = 3;
		[Header("CameraAngleLimit")]
		[SerializeField] private bool _useYawLimit;
		[SerializeField] private Vector2 _pitchLimit = new Vector2(-90f, 90f);
		[SerializeField] private Vector2 _yawLimit = new Vector2(-90f, 90f);

		private NetworkObject _playerObject;
		private Quaternion _defaultRotation;
		private float _basePitch;
		private float _baseYaw;
		[Networked] private float Pitch { get; set; }
		[Networked] private float Yaw { get; set; }

		public override void Spawned()
		{
			_defaultRotation = transform.rotation;
			_basePitch = _barrel.localRotation.eulerAngles.x;
			_baseYaw = _rotateBase.localRotation.eulerAngles.y;
			_cameraController.Init(true);
		}

		public override void Render()
		{
			_rotateBase.transform.localRotation = Quaternion.Euler(0, Yaw, 0);
			_barrel.localRotation = Quaternion.Euler(Pitch, Yaw, 0);
		}

		public void Initialize(NetworkObject playerObject, PlayerRef playerRef)
		{
			_playerObject = playerObject;
		}

		public void MoveUpdate(PlayerInput input)
		{
			var moveInput = input.LookDirection;
			_cameraController.RotateCamera(moveInput, Runner.DeltaTime);
			var pitch = _cameraController.CameraPitch;
			var yaw = Mathf.DeltaAngle(0, _cameraController.CameraYaw);
			pitch = Mathf.Clamp(pitch, _pitchLimit.x, _pitchLimit.y);
			yaw = Mathf.Clamp(yaw, _yawLimit.x, _yawLimit.y);
			_cameraController.SetCameraRotate(pitch, yaw);
			

			var cameraForward = _cameraController.GetCameraForward();
			if (Physics.Raycast(_cameraController.GetCameraPosition(), cameraForward, out var hit, 100, _layerMask))
			{
				var direction = hit.point - _rotateBase.transform.position;
				var euler = Quaternion.LookRotation(direction.normalized).eulerAngles;
				Pitch = euler.x;
				Yaw = euler.y;
			}
			else
			{
				var cameraEuler = Quaternion.LookRotation(cameraForward).eulerAngles;
				Pitch = cameraEuler.x;
				Yaw = cameraEuler.y;
			}
			
			UpdatePlayerPosition();
		}

		void UpdatePlayerPosition()
		{
			var quaternion = Quaternion.Euler(0, Yaw, 0);
			_playerObject.transform.rotation = quaternion;
			_playerObject.transform.position = _rotateBase.position + quaternion * (Vector3.back * _playerOffset);
		}
		public void Refresh()
		{ 
			_cameraController.CameraReset();
		}
	}
}