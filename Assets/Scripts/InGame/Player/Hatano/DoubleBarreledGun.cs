using System;
using Fusion;
using InGame.Health;
using InGame.Player.Ability.Effect.Shooting;
using InGame.Player.Hatano;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ability
{
    [Serializable]
    public class DoubleBarreledGun : ShootingAbilityBase
    {
        private TickTimer _shotCooldown;
        [Header("通常時のダメージ")]
        [SerializeField] private int _damage;
        [Header("鬼の時のダメージ")] 
        [SerializeField] private int _ogreDamage;

        private HatanoAbilityStatusManagement _abilityStatusManagement;
        private HatanoWeaponController _weaponController;

        protected override bool ReplayAnimationOnEveryShot => true;
        
        protected override void OnStart()
        {
            base.OnStart();
            if (_weaponController == null)
                _weaponController = Parameter.Owner.GetComponentInChildren<HatanoWeaponController>(true);
            if(_abilityStatusManagement == null) _abilityStatusManagement = 
                Parameter.Owner.GetComponent<HatanoAbilityStatusManagement>();
            _shootingType = _shotCooldown.ExpiredOrNotRunning(Runner)
                ? ShootingStateType.Stance : ShootingStateType.Shooting;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (StopIfControlLocked()) return;

            if (_abilityStatusManagement.AbilityStatus != HatanoAbilityStatus.DoubleBarreledGun)
            {
                ResetShootingState();
                return;
            }
            
            GunInterval();
            ShootingInputJudgment();
            if (_phase != AbilityPhase.Active) return;
            StateDetection();
        }

        /// <summary>
        /// 射撃後のインターバル処理
        /// </summary>
        private void GunInterval()
        {
            // 構え直しても、直前の発射からのクールタイムは維持する。
            if (_shootingType == ShootingStateType.Shooting)
            {
                //タイマーが時間を超えたら再度、構えステートに変更
                // Inspector のクールタイムを射撃間隔にも使用する。
                if (_shotCooldown.ExpiredOrNotRunning(Runner))
                {
                    _shootingType = ShootingStateType.Stance;
                }
            }
        }

        /// <summary>
        /// 射撃入力を受けたら
        /// 左右のマズルからRayを飛ばして、Hitした場所にRayを飛ばす
        /// </summary>
        private void GunShootingDetection()
        {
            var aimOri = _aimCameraController.AimOrigin;
            var aimDir = _aimCameraController.AimDirection;
            var targetPos = ShootingPositionDetection(aimOri, aimDir);
            
            //左
            var originLeft = _muzzlePos[0].position;
            var dirLeft = targetPos - originLeft;
            Debug.DrawRay(originLeft, dirLeft * _shootingDistance, Color.blue);
            //右
            var originRight = _muzzlePos[1].position;
            var dirRight = targetPos - originRight;
            Debug.DrawRay(originRight, dirRight * _shootingDistance, Color.blue);
            
            //左右のマズルから、ヒットした場所にRayを飛ばす
            var hitLeft = Physics.Raycast(originLeft, dirLeft, out var gunHitInfoLeft, _shootingDistance, _hitLayerMask);
            var hitRight = Physics.Raycast(originRight, dirRight, out var gunHitInfoRight, _shootingDistance, _hitLayerMask);
            if (_weaponController != null)
                _weaponController.RPC_PlayGunShot(
                    originLeft, hitLeft ? gunHitInfoLeft.point : originLeft + dirLeft.normalized * _shootingDistance,
                    gunHitInfoLeft.normal, hitLeft,
                    originRight, hitRight ? gunHitInfoRight.point : originRight + dirRight.normalized * _shootingDistance,
                    gunHitInfoRight.normal, hitRight);
            GetGunHitPointIDamageable(gunHitInfoLeft, gunHitInfoRight);
        }

        /// <summary>
        /// ヒットしたコライダーからIDamageableを取得
        /// </summary>
        /// <param name="hitLeft">左マズルのヒットした場所</param>
        /// <param name="hitRight">右マズルのヒットした場所</param>
        private void GetGunHitPointIDamageable(RaycastHit hitLeft, RaycastHit hitRight)
        {
            //自身に当たった場合、処理を行わない
            var damageableL = hitLeft.collider != null
                && hitLeft.collider.GetComponentInParent<NetworkObject>() != Parameter.Owner
                ? hitLeft.collider.GetComponentInParent<IDamageable>() : null;
            var damageableR = hitRight.collider != null
                && hitRight.collider.GetComponentInParent<NetworkObject>() != Parameter.Owner
                ? hitRight.collider.GetComponentInParent<IDamageable>() : null;
            GunDamage(damageableL);
            GunDamage(damageableR);
        }

        /// <summary>
        /// ダメージを与える処理
        /// </summary>
        /// <param name="damageable">ヒットしたコライダーのIDamageable</param>
        private void GunDamage(IDamageable damageable)
        {
            if(damageable == null) return;
            
            var inputAuthority = Parameter.Owner.InputAuthority;
            //ダメージ処理
            bool enableData = PlayerDatabase.Instance.PlayerDataDic.TryGet(inputAuthority, out var sessionData);
            var hitData = new HitData(HitActionType.RangedDamage,
                enableData && sessionData.IsOgre ? _ogreDamage : _damage, inputAuthority,
                damageable.OwnerPlayerRef, null, damageable);
            damageable.TakeHit(ref hitData);
        }

        protected override void OnShooting()
        {
            GunShootingDetection();
            _shotCooldown = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0f, _cooldown));
            _shootingType = ShootingStateType.Shooting;
        }
    }
}
