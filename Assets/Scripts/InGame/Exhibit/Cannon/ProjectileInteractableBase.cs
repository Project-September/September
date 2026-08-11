using System;
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
		[SerializeField] protected Animator _animator;
		
		[Header("reload設定")] 
		[SerializeReference, SubclassSelector] IFireController _fireBullerController;

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

			// 使用中のプレイヤーに対する処理
			if (!_usingPlayer) return;
			_usingPlayer.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);
			PlayerActive(false);
			_move.Initialize(_usingPlayer.Object, playerRef);
			_fireBullerController.Init();
			StartAnimation(true);

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
			if (input.Buttons.IsSet(PlayerButtons.Attack) && LastFireTimer.ExpiredOrNotRunning(Runner))
			{
				Fire();
			}

			CheckInteractEnd(input);
			
			OnInteractFixedUpdate();
		}

		protected virtual void Fire()
		{
			_launcher.Fire(CurrentUsePlayerRef);
			_fireBullerController.Fire();
			PlayFireAnimation();
			
			_currentAmmo -= 1;
			if (!_fireBullerController.IsUsable())
			{
				var timer = TickTimer.CreateFromSeconds(Runner, 1f);
				WaitExitTimer = timer;
				LastFireTimer = timer;
			}
			else
			{
				LastFireTimer = _fireBullerController.GetNextFireTimer(Runner);
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
			StartAnimation(false);
			Object.RemoveInputAuthority();
			RPC_SetCameraPriority(CurrentUsePlayerRef, 5);

			// 使用中のプレイヤークライアントに対しての
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

		private void StartAnimation(bool isActive)
		{
			_animator.SetBool("IsStart", isActive);
		}

		private void PlayFireAnimation()
		{
			_animator?.SetTrigger("Fire");
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

	public interface IFireController
	{
		void Init();
		void Fire();
		bool IsUsable();
		TickTimer GetNextFireTimer(NetworkRunner runner);
	}

	[Serializable]
	public class UseReload : IFireController
	{
		[SerializeField] int _maxAmmo;
		[SerializeField] private float _fireRate;
		[SerializeField] float _reloadTime;
		private int _currentAmmo;
		private bool _isReloading;


		public void Init()
		{
			_currentAmmo = _maxAmmo;
			_isReloading = false;
		}

		public void Fire()
		{
			_currentAmmo--;
			if (_currentAmmo == 0)
			{
				_isReloading = true;
			}
		}

		public bool IsUsable()
		{
			return true;
		}

		public TickTimer GetNextFireTimer(NetworkRunner runner)
		{
			if (_isReloading)
			{
				Init();
				return TickTimer.CreateFromSeconds(runner, _reloadTime);
			}
			
			return TickTimer.CreateFromSeconds(runner, _fireRate);
		}
	}
	
	[Serializable]
	public class UseNoReload : IFireController
	{
		[SerializeField] int _maxAmmo;
		[SerializeField] private float _fireRate;
		private int _currentAmmo;

		public void Init()
		{
			_currentAmmo = _maxAmmo;	
		}

		public void Fire()
		{
			_currentAmmo--;
		}

		public bool IsUsable()
		{
			return 0 < _currentAmmo;
		}

		public TickTimer GetNextFireTimer(NetworkRunner runner)
		{
			return TickTimer.CreateFromSeconds(runner, _fireRate);
		}
	}
}