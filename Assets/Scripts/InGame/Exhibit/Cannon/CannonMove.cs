using Fusion;
using September.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class CannonMove : NetworkBehaviour, IProjectileMovement
	{
		[Header("移動関連")] [SerializeField] private Transform _cannonBase;
		[SerializeField] private Transform _cannonBarrel;
		[SerializeField] private float _baseRotateSpeed;
		[SerializeField] private float _barrelRotateSpeed;
		// minをxとして、maxをyとして扱う
		[SerializeField] private Vector2 _baseRotateAngleLimit = new(-90, 90);
		[SerializeField] private Vector2 _barrelRotateAngleLimit = new(-90, 90);
		[SerializeField] private float _playerOffset = 3f;
		private Quaternion _baseDefaultRotation;
		private Quaternion _barrelDefaultRotation;
		[Networked] private NetworkObject Player { get; set; }
		[Networked] private Quaternion BaseRotation { get; set; }
		[Networked] private Quaternion BarrelRotation { get; set; }

		public override void Render()
		{ 
			base.Render();
			_cannonBase.localRotation = BaseRotation;
			_cannonBarrel.localRotation = BarrelRotation;
		}

		public void InitializeStateAuthority(NetworkObject playerObject, PlayerRef playerRef)
		{
			Player = playerObject;
		}

		public void Initialize()
		{
			
		}

		void IProjectileMovement.Update(PlayerInput input)
		{
			if(!HasStateAuthority) return;
			CannonRotate(input.MoveDirection.x, input.LookDirection.y);
			UpdatePlayerPos();
		}

		public void Reset()
		{
			RPC_Refresh();
		}
		
		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_Refresh()
		{
			BarrelRotation = _barrelDefaultRotation;
			BaseRotation = _baseDefaultRotation;
		}

		private void UpdatePlayerPos()
		{
			var quaternion = BaseRotation;
			Player.transform.rotation = quaternion;
			Player.transform.position = _cannonBase.position + quaternion * (Vector3.back * _playerOffset);
		}

		private void CannonRotate(float baseRotateInput, float barrelRotateInput)
		{
			baseRotateInput = Mathf.Clamp(baseRotateInput, -1, 1);
			barrelRotateInput = Mathf.Clamp(barrelRotateInput, -1, 1);
			
			// 土台の回転
			var currentBaseAxis = _cannonBase.localEulerAngles.y +
			                      _baseRotateSpeed * baseRotateInput;
			currentBaseAxis = WrapAngle(currentBaseAxis);
			BaseRotation = Quaternion.Euler(0,
				Mathf.Clamp(currentBaseAxis, _baseRotateAngleLimit.x, _baseRotateAngleLimit.y), 0);

			// 砲身の回転
			var currentBarrelAxis = _cannonBarrel.localEulerAngles.x +
			                        _barrelRotateSpeed * barrelRotateInput;
			currentBarrelAxis = WrapAngle(currentBarrelAxis);
			BarrelRotation = _cannonBase.localRotation * Quaternion.Euler(
				Mathf.Clamp(currentBarrelAxis, _barrelRotateAngleLimit.x, _barrelRotateAngleLimit.y), 0, 0);
		}
		
		/// <summary>
		///     角度を-180~180に変換する
		/// </summary>
		private float WrapAngle(float angle)
		{
			angle %= 360;
			if (angle > 180) angle -= 360;
			return angle;
		}
	}
}