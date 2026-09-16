using Fusion;
using InGame.Player;
using September.Common;
using Unity.Mathematics;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class BallistaMove : NetworkBehaviour, IProjectileMovement
	{ 
		[SerializeField] private Transform _barrel;
		[SerializeField] private Transform _rotateBase;
		[SerializeField] private Transform _shootPos;
		[SerializeField] private Transform _playerPos;
		[SerializeField] private Transform _muzzle;
		[SerializeField] private CameraController _cameraController;
		[SerializeField] private LayerMask _layerMask;
		[SerializeField] private float _playerOffset = 3;
		[SerializeField] private float _rotateSpeed = 1.5f;
		
		[Header("ターゲット距離設定")]
		[SerializeField] private float _raycastDistance = 1000f;
		[SerializeField] private float _defaultDistance = 100f;
		[SerializeField] private float _minDistance = 20f;

		[Header("CameraAngleLimit")] [SerializeField]
		private bool _useYawLimit;
		[SerializeField] private Vector2 _pitchLimit = new(-90f, 90f);
		[SerializeField] private Vector2 _yawLimit = new(-90f, 90f);
		[SerializeField] private Vector3 _baseUp;
		[SerializeField] private Vector3 _barrelRight;
		private float _baseYaw;
		private float _basePitch;
		
		[Networked] private NetworkObject PlayerObject { get; set; }
		[Networked] private float Pitch { get; set; }
		[Networked] private float Yaw { get; set; }

		public override void Spawned()
		{
			_cameraController.Init(true);
			_baseYaw = _barrel.rotation.eulerAngles.y;
			Yaw = _baseYaw;
			_basePitch = _barrel.rotation.eulerAngles.x;
			Pitch = _basePitch;
		}

		public void Render()
		{
			ModelRotate();
			UpdatePlayerPosition();
			if (HasInputAuthority)
			{
				RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(), Time.deltaTime);
			}
		}

		public void InitializeStateAuthority(NetworkObject playerObject, PlayerRef playerRef)
		{
			PlayerObject = playerObject;
		}

		public void Initialize()
		{
			if(HasInputAuthority)
			{
				_cameraController.SetCameraRotate(_basePitch, _baseYaw);
			}
		}

		private void ModelRotate()
		{
			// 軸をYawに置き換える
			var _baseAngle = _rotateBase.eulerAngles;
			_baseAngle.y = Yaw;
			_rotateBase.rotation = Quaternion.Euler(_baseAngle);
			
			var currentBarrelAngles = _barrel.eulerAngles;
			currentBarrelAngles.x = Pitch;
			_barrel.rotation = Quaternion.Euler(currentBarrelAngles);
		}

		void IProjectileMovement.Update(PlayerInput input)
		{
			var cameraForward = input.DesiredLookDirection;
			Debug.DrawRay(_cameraController.GetCameraPosition(), cameraForward * _raycastDistance, Color.green);
			Debug.DrawRay(_muzzle.position, _muzzle.forward * _raycastDistance, Color.cyan);
			
			var ray = new Ray(input.CameraPosition, cameraForward);
			Vector3 targetPos;
			
			// _defaultDistance以下のhit距離の場合はdefault距離での値に統一する
			if (Physics.Raycast(ray, out var hit, _raycastDistance, _layerMask))
			{
				if (hit.distance > _minDistance)
				{
					targetPos = hit.point;
				}
				else
				{
					targetPos = ray.origin + ray.direction * _minDistance;
				}
			}
			else
			{
				targetPos = ray.origin + ray.direction * _defaultDistance;
			}
			
			Vector3 direction = targetPos - _muzzle.position;
			float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
			float horizontalDistance =
				new Vector2(direction.x, direction.z).magnitude;

			float pitch =
				-Mathf.Atan2(direction.y, horizontalDistance) * Mathf.Rad2Deg;
			Yaw = yaw;
			Pitch = pitch;
			
			UpdatePlayerPosition();
		}

		private void RotateCamera(Vector2 input, float deltaTime)
		{
			_cameraController.RotateCamera(input, deltaTime);

			// 角度を-180~180に変換してclampする
			var pitch = Mathf.DeltaAngle(0, _cameraController.CameraPitch);
			pitch = Mathf.Clamp(pitch, _pitchLimit.x, _pitchLimit.y);
			
			float yaw = _cameraController.CameraYaw;
			if (_useYawLimit)
			{
				yaw = Mathf.DeltaAngle(_baseYaw, _cameraController.CameraYaw);
				yaw = Mathf.Clamp(yaw, _yawLimit.x, _yawLimit.y) + _baseYaw;
			}
			
			_cameraController.SetCameraRotate(pitch, yaw);
		}

		private void UpdatePlayerPosition()
		{
			if(!PlayerObject) return;
			PlayerObject.transform.position = _playerPos.position;
			PlayerObject.transform.rotation = _playerPos.rotation;
		}

		public void Reset()
		{
			Yaw = _baseYaw;
			Pitch = _basePitch;
			PlayerObject = null;
		}
	}
}