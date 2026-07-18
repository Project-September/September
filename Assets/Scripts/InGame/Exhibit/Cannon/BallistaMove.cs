using System;
using Common.UserSettings;
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
		[SerializeField] private BallistaCamera _cameraController;
		[SerializeField] private LayerMask _layerMask;
		[SerializeField] private Vector2 _pitchMinMax;
		[SerializeField] private Vector2 _yawMinMax;
		[SerializeField] private float _sens;
		[SerializeField] private float _padSens;

		private float _basePitch;
		private float _baseYaw;
		private float _pitch;
		private float _yaw;

		public override void Spawned()
		{
			_basePitch = _barrel.rotation.eulerAngles.x;
			_baseYaw = _rotateBase.rotation.eulerAngles.y;
			_cameraController.Initialize();
		}

		public void MoveUpdate(PlayerInput input)
		{
			var moveInput = input.LookDirection;
			RotateCamera(moveInput, Runner.DeltaTime);

			var cameraForward = _cameraController.transform.forward.normalized;
			if (Physics.Raycast(_cameraController.transform.position, cameraForward, out var hit, 100, _layerMask))
			{
				var direction = hit.point - _rotateBase.transform.position;
				var euler = Quaternion.LookRotation(direction).eulerAngles;
				_pitch = Mathf.Clamp(euler.x, _pitchMinMax.x, _pitchMinMax.y);
				_yaw = Mathf.Clamp(euler.y, _yawMinMax.x, _yawMinMax.y);
			}

			_rotateBase.transform.rotation = Quaternion.Euler(0, _yaw + _baseYaw, 0);
			_barrel.rotation = Quaternion.Euler(_basePitch + _pitch, _yaw + _baseYaw, 0);
		}		
		
		public void RotateCamera(Vector2 input, float deltaTime)
		{
			var settings = UserSettings.Get();
			var sens =
				GameInput.I.UseDeviceType == GameInput.DeviceType.KeyboardMouse
					? _sens * settings.MouseSensitivity
					: _padSens * settings.PadSensitivity;
			
			float pitchInput = input.y;
			float yawInput = input.x;

			_pitch -= pitchInput * deltaTime * sens;
			_pitch = Mathf.Clamp(_pitch, _pitchMinMax.x - _basePitch, _basePitch - _pitchMinMax.y);
			_yaw -= yawInput * deltaTime * sens;
			_yaw = Mathf.Clamp(_yaw, _yawMinMax.x - _baseYaw, _baseYaw - _yawMinMax.y);
		}

		public void Refresh()
		{ 
			_cameraController.ResetCamera();
		}
	}
}