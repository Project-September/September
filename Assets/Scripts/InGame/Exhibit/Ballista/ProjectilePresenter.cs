using System;
using Fusion;
using September.Common;
using September.InGame.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class ProjectilePresenter : MonoBehaviour
	{
		[SerializeField] private BulletView _bulletView;
		[SerializeField] private GameObject _uiParent;
		[SerializeField] private GameObject _crosshairObject;
		private ProjectileInteractableBase[] _projectileObjects;
		private ProjectileInteractableBase _currentInteractable;
		private PlayerRef _localPlayer;

		private void Start()
		{
			_projectileObjects = FindProjectileObjects();
			Subscribe(_projectileObjects);
			_uiParent.SetActive(false);
			_crosshairObject.SetActive(false);
			var manager = StaticServiceLocator.Instance.Get<InGameManager>();
			_localPlayer = manager.Runner.LocalPlayer;
		}

		private void OnDisable()
		{
			foreach (var projectileObject in _projectileObjects)
			{
				projectileObject.OnInteractStart -= InteractStart;
				projectileObject.OnInteractEnd -= InteractEnd;
				projectileObject.OnAmmoChanged -= BulletUpdate;
			}
		}

		private ProjectileInteractableBase[] FindProjectileObjects()
		{
			return FindObjectsByType<ProjectileInteractableBase>(FindObjectsSortMode.None);
		}

		private void Subscribe(ProjectileInteractableBase[] projectileObjects)
		{
			foreach (var projectile in projectileObjects)
			{
				projectile.OnInteractStart += InteractStart;
				projectile.OnInteractEnd += InteractEnd;
				projectile.OnAmmoChanged += BulletUpdate;
			}
		}

		private void InteractStart(ProjectileInteractableBase projectile, PlayerRef playerRef)
		{
			if (_currentInteractable != null) return;
			if(playerRef != _localPlayer) return;
			_currentInteractable = projectile;
			_uiParent.SetActive(true);
			_crosshairObject.SetActive(_currentInteractable.ReticleEffect is BallistaReticle);
		}

		private void BulletUpdate(int bullet, float time, PlayerRef playerRef)
		{
			if(playerRef != _localPlayer) return;
			_bulletView.UpdateAmmo(bullet, time);
		}

		private void InteractEnd(ProjectileInteractableBase projectile, PlayerRef playerRef)
		{
			if(playerRef != _localPlayer) return;
			_currentInteractable = null;
			_uiParent.SetActive(false);
			_crosshairObject.SetActive(false);
		}
	}
}