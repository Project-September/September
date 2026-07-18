using System;
using Cinemachine;
using Common.UserSettings;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class BallistaCamera : MonoBehaviour
	{
		[SerializeField] private Transform _pivot;
		[SerializeField] private CinemachineVirtualCameraBase _cam;
		[SerializeField] private float _sens;
		[SerializeField] private float _padSens;
		[SerializeField] private Vector2 _pitchMinMax;
		[SerializeField] private Vector2 _yawMinMax;
		[SerializeField] private LayerMask _collideAgainst;
		[SerializeField] private float _cameraRadius;
		
		Quaternion _defaultRotation; 
		private float _basePitch;
		private float _baseYaw;
		private float _pitch;
		private float _yaw;

		private void LateUpdate()
		{
			CheckCameraDistance();
		}

		public void Initialize()
		{
			_defaultRotation = _pivot.localRotation;
			_baseYaw = _defaultRotation.eulerAngles.y;
			_basePitch = _defaultRotation.eulerAngles.x;
		}

		public void ResetCamera()
		{
			_pitch = 0;
			_yaw = 0;
		}

		public void RotateCamera(float yaw, float pitch)
		{
			_pitch = Mathf.Clamp(pitch, _pitchMinMax.x, _pitchMinMax.y);
			_yaw = Mathf.Clamp(yaw, _yawMinMax.x, _yawMinMax.y);
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

			_pivot.localRotation = Quaternion.Euler(_pitch, _yaw, 0);
		}
		
		void CheckCameraDistance()
		{
			var isHit = Physics.Linecast(_pivot.position, _pivot.position + _pivot.TransformDirection(_cam.transform.position),
				out var hit, _collideAgainst);
            
			if (isHit)
			{
				Vector3 sphereCenter = hit.point + hit.normal * _cameraRadius;
				_cam.transform.position = sphereCenter;
			}
		}
	}
}