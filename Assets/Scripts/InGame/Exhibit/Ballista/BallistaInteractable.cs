using Fusion;
using September.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
    public class BallistaInteractable: ProjectileInteractableBase
    {
        [SerializeField] private float _reloadSeconds = 10f;
        [Networked] private TickTimer ReloadTimer { get; set; }

        protected override void OnInteractFixedUpdate()
        {
            if (ReloadTimer.Expired(Runner))
            {
                ReloadTimer = default(TickTimer);
                _currentAmmo = _maxAmmo;
                Debug.Log("reload");
            }
        }

        protected override void Fire()
        {
            if(!ReloadTimer.ExpiredOrNotRunning(Runner)) return;
            _launcher.Fire(CurrentUsePlayerRef);
            _currentAmmo -= 1;
            LastFireTimer = TickTimer.CreateFromSeconds(Runner, _reloadTime);
            
            if (_currentAmmo <= 0)
            {
                Debug.Log("current ammo is 0");
                ReloadTimer = TickTimer.CreateFromSeconds(Runner, _reloadSeconds);
            }
        }
    }
}