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
		[SerializeField] private CameraController _cameraController;
		[SerializeField] private LayerMask _layerMask;
		[SerializeField] private float _playerOffset = 3;
		[SerializeField] private float _rotateSpeed = 1.5f;

		[Header("CameraAngleLimit")] [SerializeField]
		private bool _useYawLimit;

		[SerializeField] private Vector2 _pitchLimit = new(-90f, 90f);
		[SerializeField] private Vector2 _yawLimit = new(-90f, 90f);
		[SerializeField] private Vector3 _baseUp;
		[SerializeField] private Vector3 _barrelRight;
		[SerializeField] private float _baseYaw;
		[SerializeField] private float _basePitch;

		private Quaternion _barrelDefaultLocalRotation;
		private Quaternion _baseDefaultLocalRotation;
		[Networked] private NetworkObject PlayerObject { get; set; }
		[Networked] private float Pitch { get; set; }
		[Networked] private float Yaw { get; set; }
		[Networked] private Vector3 CameraForward { get; set; }

		public override void Spawned()
		{
			_baseDefaultLocalRotation = _rotateBase.rotation;
			_baseYaw = _barrel.rotation.eulerAngles.y;
			_basePitch = _barrel.rotation.eulerAngles.x;

			_barrelDefaultLocalRotation = _barrel.localRotation;

			_cameraController.Init(true);
			Debug.Log($"baseYaw: {_baseYaw} yaw: {_cameraController.CameraYaw}");
			_basePitch = _cameraController.CameraPitch;

			Debug.Log($"pitch: {_cameraController.CameraPitch} yaw: {_cameraController.CameraYaw}");

			_baseYaw = Vector3.SignedAngle(
				Vector3.forward,
				_cameraController.transform.forward,
				_baseUp);
			
			Yaw = _baseYaw;
		}

		public override void Render()
		{
			ModelRotate();
			if (HasInputAuthority)
			{
				RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(), Time.deltaTime);
			}
		}

		private void ModelRotate()
		{
			Debug.Log(
				$"base Default {_baseDefaultLocalRotation.eulerAngles} yaw: {Quaternion.AngleAxis(Yaw, _baseUp).eulerAngles} " +
				$"rotate: {(_baseDefaultLocalRotation * Quaternion.AngleAxis(Yaw, _baseUp)).eulerAngles}");
			// worldのrotationでやりたい場合も全く同じ関数でOK
			var _rotate = ReplaceTwist(transform.rotation, _baseUp, Yaw);
			transform.rotation = _rotate;

			//_rotateBase.localRotation = _baseDefaultLocalRotation *  Quaternion.AngleAxis(Yaw - _baseYaw, _baseUp);
			_barrel.localRotation = _barrelDefaultLocalRotation * Quaternion.AngleAxis(Pitch, _barrelRight);
			Debug.DrawRay(_rotateBase.position, _rotateBase.forward * 100, Color.cyan);
		}

		// 一軸の回転のみを新しい角度に置き換える
		private static Quaternion ReplaceTwist(Quaternion q, Vector3 axis, float newAngleDeg)
		{
			// --- q を swing * twist に分解 ---
			var r = new Vector3(q.x, q.y, q.z);
			var p = Vector3.Project(r, axis); // axis成分だけ取り出す
			var twist = new Quaternion(p.x, p.y, p.z, q.w);

			// 分解が退化するケース(180度回転など)のケア
			if (twist.x == 0f && twist.y == 0f && twist.z == 0f && twist.w == 0f)
				twist = Quaternion.identity;
			else
				twist = twist.normalized;

			var swing = q * Quaternion.Inverse(twist);

			// --- twist だけを新しい角度に差し替えて再合成 ---
			var newTwist = Quaternion.AngleAxis(newAngleDeg, axis);
			return swing * newTwist;
		}

		public void Initialize(NetworkObject playerObject, PlayerRef playerRef)
		{
			PlayerObject = playerObject;
			_cameraController.CameraReset();
		}

		public void MoveUpdate(PlayerInput input)
		{
			var cameraForward = input.DesiredLookDirection;

			if (Physics.Raycast(input.CameraPosition, cameraForward, out var hit, 100, _layerMask))
			{
				CameraForward = (hit.point - _rotateBase.position).normalized;
			}
			else
			{
				CameraForward = cameraForward;
			}

			var lookRotation = Quaternion.LookRotation(CameraForward);
			Yaw = lookRotation.eulerAngles.y;
			Pitch = Mathf.DeltaAngle(0f, lookRotation.eulerAngles.x);

			UpdatePlayerPosition();
		}

		private void RotateCamera(Vector2 input, float deltaTime)
		{
			_cameraController.RotateCamera(input, deltaTime);
			// 角度を-180~180に変換してclampする
			var yaw = Mathf.DeltaAngle(_baseYaw, _cameraController.CameraYaw);
			var pitch = Mathf.DeltaAngle(0, _cameraController.CameraPitch);
			Debug.Log($"pitch: {pitch} yaw: {yaw} baseYaw: {_baseYaw} basePitch: {_basePitch}");
			pitch = Mathf.Clamp(pitch, _pitchLimit.x, _pitchLimit.y);
			yaw = Mathf.Clamp(yaw, _yawLimit.x, _yawLimit.y) + _baseYaw;
			
			_cameraController.SetCameraRotate(pitch, yaw);
		}

		private void UpdatePlayerPosition()
		{
			var quaternion = Quaternion.AngleAxis(Yaw + _baseYaw, Vector3.up);
			PlayerObject.transform.rotation = quaternion;
			PlayerObject.transform.position = _rotateBase.position + quaternion * (Vector3.back * _playerOffset);
		}

		public void Refresh()
		{
			_cameraController.CameraReset();
		}
	}
}