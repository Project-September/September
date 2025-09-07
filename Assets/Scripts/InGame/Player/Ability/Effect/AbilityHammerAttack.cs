using System.Linq;
using Fusion;
using InGame.Common;
using InGame.Exhibit.InteractEffect;
using InGame.Health;
using InGame.Interact;
using September.Common;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    public class AbilityHammerAttack : AbilityNormalAttack
    {
        protected override void OnHitEnemy(Collider hitInfo)
        {
            Debug.Log(
                $"Name {hitInfo.gameObject.name} networkObject {hitInfo.GetComponentInParent<NetworkObject>()} owner {Parameter.Owner} isAlive {hitInfo.GetComponentInParent<IDamageable>()?.IsAlive} disableInteractEffect {hitInfo.GetComponent<DisableInteractEffect>()}");
            if (hitInfo.GetComponentInParent<NetworkObject>() == Parameter.Owner) return;
            var damageable = hitInfo.GetComponentInParent<IDamageable>();
            var disableInteractEffect = hitInfo.GetComponent<DisableInteractEffect>();
            if (damageable == null || !disableInteractEffect) return;

            if (damageable.IsAlive)
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
                disableInteractEffect.OnHitHammerAttack();
            }

            //エフェクトの再生
            _effectSpawner.RequestPlayOneShotEffect(_hitEffect,
                hitInfo.ClosestPoint(_hitChecker.HitPoint.First().position), Quaternion.identity);
        }
    }
}