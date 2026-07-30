using System.Collections.Generic;
using Cinemachine;
using Fusion;
using InGame.Health;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
	[DefaultExecutionOrder(100)] // 他のNetworkBehaviourより遅く実行する
	public class ProjectileInteractableBase : NetworkBehaviour
	{
		[SerializeField] protected Transform _waitCharacterTransform;
		[SerializeField] protected CinemachineVirtualCamera _cameraController;
		[SerializeField] protected InteractableBase _interactable;
		
		[Header("reload設定")] [SerializeField] protected int _maxAmmo;
		[SerializeField] protected float _reloadTime;

		protected ProjectileLauncher _launcher;
		protected IProjectileMovement _move;
		protected PlayerManager _usingPlayer;
		protected int _currentAmmo;

 		[Networked] protected PlayerRef CurrentUsePlayerRef { get; set; }
		[Networked] protected TickTimer LastFireTimer { get; set; }
		[Networked] protected TickTimer WaitExitTimer { get; set; }
		[Networked] private TickTimer InteractEndLockTimer { get; set; }

		public override void Spawned()
		{
			base.Spawned();
			_launcher = GetComponent<ProjectileLauncher>();
			_move = GetComponent<IProjectileMovement>();
		}
		
		public override void Render()
		{
			base.Render();
			_launcher.EffectRender();
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();
			if(CurrentUsePlayerRef.IsNone) return;
			
			GetInput(out PlayerInput input);
			_move.MoveUpdate(input);
		}

		/// <summary>
		///     Hostのみで実行されるインタラクト開始時の初期化処理
		/// </summary>
		public virtual void InteractStart(PlayerRef playerRef)
		{
			// プレイヤーの取得
			CurrentUsePlayerRef = playerRef;
			_usingPlayer = Runner.GetPlayerObject(CurrentUsePlayerRef).GetComponent<PlayerManager>();

			// インタラクトの機能を一時的に無効化する
			_interactable.ForceSetInteractable = false;

			RPC_SetCameraPriority(CurrentUsePlayerRef, 15);
			RPC_EffectActive(CurrentUsePlayerRef, true);
			Object.AssignInputAuthority(CurrentUsePlayerRef);
			_currentAmmo = _maxAmmo;

			// 使用中のプレイヤーに対する処理
			if (!_usingPlayer) return;
			_usingPlayer.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);
			PlayerActive(false);
			_move.Initialize(_usingPlayer.Object, playerRef);

			// Playerがダメージを受けた際にInteractを終了する
			_usingPlayer.GetComponent<PlayerHealth>().OnHitTaken += PlayerHitTaken;
			
			// インタラクトして1秒後からインタラクト解除可能にする
			InteractEndLockTimer = TickTimer.CreateFromSeconds(Runner, 1f);
			OnInteractStart();
		}

		/// <summary>
		///     インタラクト中の処理(Hostのみ)
		/// </summary>
		public virtual void InteractFixedNetworkUpdate(PlayerInput input)
		{
			base.FixedUpdateNetwork();
			
			// 射撃処理
			if (input.Buttons.IsSet(PlayerButtons.Attack) && LastFireTimer.ExpiredOrNotRunning(Runner) &&
			    _currentAmmo > 0)
			{
				Fire();
			}

			CheckInteractEnd(input);
			
			OnInteractFixedUpdate();
		}

		protected virtual void Fire()
		{
			_launcher.Fire(CurrentUsePlayerRef);
			LastFireTimer = TickTimer.CreateFromSeconds(Runner, _reloadTime);
			_currentAmmo -= 1;
			if (_currentAmmo <= 0)
			{
				WaitExitTimer = TickTimer.CreateFromSeconds(Runner, 1f);
			}
		}

		protected virtual void CheckInteractEnd(PlayerInput input)
		{
			if (input.Buttons.IsSet(PlayerButtons.Interact) && InteractEndLockTimer.ExpiredOrNotRunning(Runner))
			{
				InteractEnd();
			}

			if (WaitExitTimer.Expired(Runner))
			{
				WaitExitTimer = TickTimer.None;
				InteractEnd();
			}
		}

		/// <summary>
		///     インタラクト終了時の処理(Hostのみ)
		/// </summary>
		public void InteractEnd()
		{
			SetCooldown();
			_move.Refresh();
			Object.RemoveInputAuthority();
			RPC_SetCameraPriority(CurrentUsePlayerRef, 5);

			// 使用中のプレイヤークライアント限定処理
			if (!_usingPlayer) return;
			PlayerActive(true);
			RPC_EffectActive(CurrentUsePlayerRef, false);
			_usingPlayer.GetComponent<PlayerHealth>().OnHitTaken -= PlayerHitTaken;
			
			OnInteractEnd();
			
			_usingPlayer = null;
			CurrentUsePlayerRef = default;
			_interactable.EndInteract();
		}

		protected virtual void OnInteractStart()
		{
			
		}

		protected virtual void OnInteractFixedUpdate()
		{
			
		}

		protected virtual void OnInteractEnd()
		{
			
		}

		private void PlayerActive(bool isActive)
		{
			if (Runner.IsServer)
			{
				RPC_SetCameraPriority(CurrentUsePlayerRef, isActive ? 0 : 15);
				_usingPlayer.RPC_SetControlState(isActive
					? PlayerManager.PlayerControlState.Normal
					: PlayerManager.PlayerControlState.ForcedControl);
				_usingPlayer.RPC_SetUseGrav(isActive); 
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
		private void RPC_EffectActive(PlayerRef currentPlayer, bool isActive)
		{
			EffectActive(currentPlayer, isActive);
		}

		protected virtual void EffectActive(PlayerRef currentPlayer, bool isActive)
		{
			if(Runner.LocalPlayer == currentPlayer)
			{
				_launcher.IsRenderLine = isActive;
			}
		}

		private void PlayerHitTaken(HitData hitData)
		{
			InteractEnd();
		}

		#region Helper

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_SetCameraPriority(PlayerRef playerRef, int priority)
		{
			if (Runner.LocalPlayer != playerRef) return;
			_cameraController.Priority = priority;
			_cameraController.MoveToTopOfPrioritySubqueue();
		}

		#endregion
	}

	public interface IProjectileMovement
	{
		public void Initialize(NetworkObject playerObject, PlayerRef playerRef);
		public void MoveUpdate(PlayerInput input);
		public void Refresh();
	}
}