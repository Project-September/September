using System.Linq;
using Fusion;
using InGame.Exhibit.InteractEffect;
using InGame.Health;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    public class AbilityHammerAttack : AbilityNormalAttack
    {
        protected override void OnHitEnemy(Collider hitInfo)
        {
            if (hitInfo.GetComponentInParent<NetworkObject>() == Parameter.Owner) return;
            var damageable = hitInfo.GetComponentInParent<IDamageable>();
            var disableInteractEffect = hitInfo.GetComponent<DisableInteractEffect>();
            if (damageable == null && !disableInteractEffect) return;

            if (damageable != null)
            {
                var hitData = new HitData(
                    HitActionType.Damage,
                    _attackDamage,
                    Parameter.Owner.InputAuthority,
                    damageable.OwnerPlayerRef);
                damageable.TakeHit(ref hitData);
            }

            if (disableInteractEffect)
            {
                PlayerRef actor = Parameter.Owner.InputAuthority;
                disableInteractEffect.OnHitHammerAttack();
                PlayerDatabase.Instance.Server_AddDestroyExhibit(actor,disableInteractEffect.ExhibitType);
            }

            //エフェクトの再生
            _effectSpawner.RequestPlayOneShotEffect(_hitEffect,
                hitInfo.ClosestPoint(_hitChecker.HitPoint.First().position), Quaternion.identity);
        }
    }
}