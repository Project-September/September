using Common.UserSettings;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class BallistaCamera : NetworkBehaviour
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
		[Networked] private float Pitch { get; set; }
		[Networked] private float Yaw { get; set; }

		private Vector3 _defaultOffset;
		
		private void LateUpdate()
		{
			_pivot.localRotation = Quaternion.Euler(Pitch, Yaw, 0);
			CheckCameraDistance();
		}

		public void Initialize()
		{
			_defaultRotation = _pivot.localRotation;
			_baseYaw = _defaultRotation.eulerAngles.y;
			_basePitch = _defaultRotation.eulerAngles.x;
			_defaultOffset = _cam.transform.localPosition;
		}

		public void ResetCamera()
		{
			Pitch = 0;
			Yaw = 0;
		}

		public void RotateCamera(float yaw, float pitch)
		{
			Pitch = Mathf.Clamp(pitch, _pitchMinMax.x, _pitchMinMax.y);
			Yaw = Mathf.Clamp(yaw, _yawMinMax.x, _yawMinMax.y);
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

			Pitch -= pitchInput * deltaTime * sens;
			Pitch = Mathf.Clamp(Pitch, _pitchMinMax.x, _pitchMinMax.y);
			Yaw += yawInput * deltaTime * sens;
			Yaw = Mathf.Clamp(Yaw, _yawMinMax.x,_yawMinMax.y);
		}

		public Transform GetCameraTf()
		{
			return _cam.transform;
		}
		
		void CheckCameraDistance()
		{
			var isHit = Physics.Linecast(_pivot.position, _pivot.position + _pivot.TransformDirection(_defaultOffset),
				out var hit, _collideAgainst);
            
			if (isHit)
			{
				Vector3 sphereCenter = hit.point + hit.normal * _cameraRadius;
				_cam.transform.position = sphereCenter;
			}
			
			else
			{
				_cam.transform.localPosition = _defaultOffset;
			}
		}
	}
}