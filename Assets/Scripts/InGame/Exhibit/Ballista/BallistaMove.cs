using Fusion;
using September.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class BallistaMove : NetworkBehaviour, IProjectileMovement
	{
		[SerializeField] private Transform _barrel;
		[SerializeField] private Transform _rotateBase;
		[SerializeField] private BallistaCamera _cameraController;
		[SerializeField] private LayerMask _layerMask;

		private float _basePitch;
		private float _baseYaw;
		[Networked] private float Pitch { get; set; }
		[Networked] private float Yaw { get; set; }

		public override void Spawned()
		{
			_basePitch = _barrel.rotation.eulerAngles.x;
			_baseYaw = _rotateBase.rotation.eulerAngles.y;
			_cameraController.Initialize();
		}

		public override void Render()
		{
			_rotateBase.transform.rotation = Quaternion.Euler(0, Yaw + _baseYaw, 0);
			_barrel.rotation = Quaternion.Euler(_basePitch + Pitch, Yaw + _baseYaw, 0);
		}

		public void MoveUpdate(PlayerInput input)
		{
			var moveInput = input.LookDirection;
			_cameraController.RotateCamera(moveInput, Runner.DeltaTime);

			var cameraTf = _cameraController.GetCameraTf();
			if (Physics.Raycast(cameraTf.position, cameraTf.forward, out var hit, 100, _layerMask))
			{
				var direction = hit.point - _rotateBase.transform.position;
				var euler = Quaternion.LookRotation(direction.normalized).eulerAngles;
				Pitch = euler.x;
				Yaw = euler.y;
			}
			else
			{
				Pitch = _cameraController.GetCameraTf().eulerAngles.x;
				Yaw = _cameraController.GetCameraTf().eulerAngles.y;
			}
		}

		public void Refresh()
		{ 
			_cameraController.ResetCamera();
		}
	}
}