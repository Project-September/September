using System;
using September.InGame.Exhibit;
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
        private void Start()
        {
            Subscribe(FindProjectileObjects());
            _uiParent.SetActive(false);
            _crosshairObject.SetActive(false);
        }

        private ProjectileInteractableBase[] FindProjectileObjects()
        {
            return FindObjectsByType<ProjectileInteractableBase>(FindObjectsSortMode.None); 
        }

        private void　Subscribe(ProjectileInteractableBase[] projectileObjects)
        {
            foreach (var projectile in projectileObjects)
            {
                projectile.OnInteractStart += InteractStart;
                projectile.OnInteractEnd += InteractEnd;
                projectile.OnAmmoChanged += _bulletView.UpdateAmmo;
            }
        }

        private void InteractStart(ProjectileInteractableBase projectile)
        {
            if(_currentInteractable != null) return;
            _currentInteractable = projectile;
            _uiParent.SetActive(true);
            _crosshairObject.SetActive(_currentInteractable.ReticleEffect is BallistaReticle);
        }

        private void InteractEnd(ProjectileInteractableBase projectile)
        {
            _currentInteractable = null;
            _uiParent.SetActive(false);
            _crosshairObject.SetActive(false);
        }
    }
}
