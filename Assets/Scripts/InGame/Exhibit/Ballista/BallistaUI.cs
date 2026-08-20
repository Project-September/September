using September.InGame.Exhibit;
using TMPro;
using UniRx;
using UnityEngine;

namespace  September.InGame.Exhibit.UI
{
    public class BallistaUI : MonoBehaviour
    {
        [SerializeField] ProjectileInteractableBase _projectile;
        [SerializeField] TMP_Text _projectileText;
        [SerializeField] string _reloadText = "リロード中・・・";
        private void Start()
        {
            _projectile.OnAmmoChanged += UpdateAmmo;
        }

        private void OnDestroy()
        {
            _projectile.OnAmmoChanged -= UpdateAmmo;
        }

        private void UpdateAmmo(int ammo)
        {
            var text = ammo.ToString();
            if (ammo == 0)
            {
                text = _reloadText;
            }

            _projectileText.text = text;
        }
    }
}
