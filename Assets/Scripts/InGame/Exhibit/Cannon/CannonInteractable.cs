using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class CannonInteractable : NetworkBehaviour
	{
		[Header("砲弾に関する設定")] [SerializeField] private int _maxAmmo;
		[SerializeField] private float _reloadTime;
		[SerializeField] private float _radius;
		[SerializeField] private int _baseDamage = 10;

		[Header("その他")] [SerializeField] private Transform _waitCharacterTransform;
		[SerializeField] private CameraController _cameraController;
		[SerializeField] private InteractableBase _interactable;
		[SerializeField] private CannonAimRenderer _cannonAimRenderer;

		private ProjectileLauncher _launcher;
		private IProjectileMovement _move;
		private PlayerManager _currentUsePlayer;
		private int _currentAmmo;

		[Networked] private PlayerRef CurrentUsePlayerRef { get; set; }
		[Networked] private TickTimer LastFireTime { get; set; }
		[Networked] private TickTimer WaitForSeconds { get; set; }

		public override void Spawned()
		{
			base.Spawned();
			_launcher = GetComponent<ProjectileLauncher>();
			_move = GetComponent<IProjectileMovement>();
			_cannonAimRenderer.Initialize(_radius);
			_cannonAimRenderer.RenderActive(false);
		}

		/// <summary>
		///     Hostのみで実行されるインタラクト開始時の初期化処理
		/// </summary>
		public void OnInteractStart(PlayerRef playerRef)
		{
			// プレイヤーの取得
			CurrentUsePlayerRef = playerRef;
			_currentUsePlayer = Runner.GetPlayerObject(CurrentUsePlayerRef).GetComponent<PlayerManager>();

			// インタラクトの機能を一時的に無効化する
			_interactable.ForceSetInteractable = false;

			RPC_SetCameraPriority(CurrentUsePlayerRef, 15);
			RPC_EffectActive(true);
			Object.AssignInputAuthority(CurrentUsePlayerRef);
			_currentAmmo = _maxAmmo;

			// 使用中のプレイヤークライアントのみの処理
			if (!_currentUsePlayer) return;
			PlayerActive(false);
			_currentUsePlayer.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);

			// Playerがダメージを受けた際にInteractを終了する
			_currentUsePlayer.GetComponent<PlayerHealth>().OnHitTaken += PlayerHitTaken;
		}

		/// <summary>
		///     インタラクト中の処理(Hostのみ)
		/// </summary>
		public void OnInteractFixedNetworkUpdate(PlayerInput input)
		{
			base.FixedUpdateNetwork();

			// キャノンの回転内部処理
			_move.MoveUpdate(input);
			_currentUsePlayer?.
				SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);
			_currentUsePlayer.transform.position = _waitCharacterTransform.position;
			
			// 射撃処理
			if (input.Buttons.IsSet(PlayerButtons.Attack) && LastFireTime.ExpiredOrNotRunning(Runner) &&
			    _currentAmmo > 0)
			{
				_launcher.Fire(CurrentUsePlayerRef);
				LastFireTime = TickTimer.CreateFromSeconds(Runner, _reloadTime);
				_currentAmmo -= 1;
				if (_currentAmmo <= 0) WaitForSeconds = TickTimer.CreateFromSeconds(Runner, 1f);
			}

			if (WaitForSeconds.Expired(Runner))
			{
				WaitForSeconds = TickTimer.None;
				OnInteractEnd();
			}
		}

		/// <summary>
		///     インタラクト終了時の処理(Hostのみ)
		/// </summary>
		public void OnInteractEnd()
		{
			SetCooldown();
			_move.Refresh();
			Object.RemoveInputAuthority();
			RPC_EffectActive(false);
			RPC_SetCameraPriority(CurrentUsePlayerRef, 5);

			// 使用中のプレイヤークライアント限定処理
			if (!_currentUsePlayer) return;
			PlayerActive(true);
			_currentUsePlayer.GetComponent<PlayerHealth>().OnHitTaken -= PlayerHitTaken;
			_currentUsePlayer.GetComponent<CameraController>().CameraReset();
			
			_currentUsePlayer = null;
			CurrentUsePlayerRef = default;
			_interactable.EndInteract();
		}

		private void PlayerActive(bool isActive)
		{
			if (Runner.IsServer)
			{
				RPC_SetCameraPriority(CurrentUsePlayerRef, isActive ? 0 : 15);
				_currentUsePlayer.RPC_SetControlState(isActive
					? PlayerManager.PlayerControlState.Normal
					: PlayerManager.PlayerControlState.ForcedControl);
				_currentUsePlayer.RPC_SetUseGrav(isActive);
				// TODO : Playerの Rigidbodyを直接触るのは良くないので、PlayerManagerにisKinematicを設定する関数を作る
				_currentUsePlayer.GetComponent<Rigidbody>().isKinematic = !isActive;
			}
		}

		private void SetCooldown()
		{
			// クールダウン処理
			var chara = PlayerDatabase.Instance.PlayerDataDic[CurrentUsePlayerRef].CharacterType;
			var time = _interactable.CooldownTimeDictionary.Dictionary.TryGetValue(CharacterType.All, out var all)
				? all
				: _interactable.CooldownTimeDictionary.Dictionary.GetValueOrDefault(chara, 0f);
			_interactable.SetCooldown(time);
			_interactable.ForceSetInteractable = true;
		}
		
		[Rpc]
		private void RPC_EffectActive(bool isActive)
		{Debug.Log(
				$"Local={Runner.LocalPlayer}, Current={CurrentUsePlayerRef}, Active={isActive}");
			_cannonAimRenderer.RenderActive(isActive);
			
			if(Runner.LocalPlayer == CurrentUsePlayerRef)
			{
				_launcher.IsRenderLine = isActive;
			}
		}

		private void PlayerHitTaken(HitData hitData)
		{
			OnInteractEnd();
		}

		#region Helper

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_SetCameraPriority(PlayerRef playerRef, int priority)
		{
			if (Runner.LocalPlayer != playerRef) return;
			_cameraController.SetCameraPriority(priority);
		}

		#endregion

		#region Gizmos

		private void OnDrawGizmos()
		{
			if (!Object || !Object.IsValid) return;
			// 当たり判定の可視化
			var colliderPos = _launcher.HitPosition;
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(colliderPos, _radius);
		}

		#endregion
	}

	public interface IProjectileMovement
	{
		public void MoveUpdate(PlayerInput input);
		public void Refresh();
	}
}