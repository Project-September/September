using Fusion;
using InGame.Exhibit.InteractEffect;
using InGame.Health;
using InGame.Interact;
using September.Common;
using September.InGame.Common.Stats;
using System.Linq;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    public class AbilityHammerAttack : AbilityNormalAttack
    {
        protected override void OnHitEnemy(Collider hitInfo, Vector3 hitPosition)
        {
            if (hitInfo.GetComponentInParent<NetworkObject>() == Parameter.Owner) return;
            var damageable = hitInfo.GetComponentInParent<IDamageable>();
            var disableInteractEffect = hitInfo.gameObject.GetComponentInHierarchy<DisableInteractEffect>();
            if (damageable == null && !disableInteractEffect) return;

            // 鬼状態かどうかでダメージを変更
            int damage = GetAttackDamage();

            if (damageable != null)
            {
                var hitData = new HitData(
                    HitActionType.Damage,
                    damage,
                    Parameter.Owner.InputAuthority,
                    damageable.OwnerPlayerRef);
                damageable.TakeHit(ref hitData);
                _buildGenerator?.UpdateBuild(BuildRouteType.AttackPower);
            }

            if (disableInteractEffect)
            {
                if (disableInteractEffect.gameObject.GetComponent<InteractableBase>().IsInCooldown()) return;
                PlayerRef actor = Parameter.Owner.InputAuthority;
                disableInteractEffect.OnHitHammerAttack(actor);
                PlayerDatabase.Instance.Server_AddDestroyExhibit(actor,disableInteractEffect.ExhibitType);
            }

            //エフェクトの再生
            _effectSpawner.RequestPlayOneShotEffect(_hitEffect, hitInfo.ClosestPoint(hitInfo.bounds.ClosestPoint(hitPosition)), Quaternion.identity);
        }
    }
}