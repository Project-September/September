using System;
using Fusion;
using InGame.Health;
using InGame.Player.Hatano;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ability
{
    [Serializable]
    public class DoubleBarreledGun : AbilityBase
    {
        [Header("参照")]
        [Header("AimCameraController")]
        [SerializeField] private AimCameraController _aimCameraController;
        [Space(30)] 
        [Header("射程距離")] 
        [SerializeField] private float _doubleBarreledGunDistance;
        [Header("射撃インターバル")] 
        [SerializeField] private float _shootingInterval;
        private float _shootingIntervalTimer;
        [Header("マズル（左）")]
        [SerializeField] private Transform _doubleBarreledGunLeftMuzzle;
        [Header("マズル（右）")] 
        [SerializeField] private Transform _doubleBarreledGunRightMuzzle;
        [Header("通常時のダメージ")]
        [SerializeField] private int _damage;
        [Header("鬼の時のダメージ")] 
        [SerializeField] private int _ogreDamage;
        [Networked] private ShootingStateType _lastShootingStateType { get; set; }
        
        private HatanoAbilityStatusManagement _abilityStatusManagement;
        
        protected override void OnStart()
        {
            if(_abilityStatusManagement == null) _abilityStatusManagement = 
                Parameter.Owner.GetComponent<HatanoAbilityStatusManagement>();
        }

        protected override void OnUpdate(float deltaTime)
        {
            if(_abilityStatusManagement.AbilityStatus != HatanoAbilityStatus.DoubleBarreledGun) return;
            
            if (_shootingStateType == ShootingStateType.Stance)
            {
                if (_playerInput.Buttons.IsSet(PlayerButtons.Shooting))
                {
                    GunTargetDetection();
                    _shootingStateType = ShootingStateType.Shooting;
                }
            }
            
            //射撃後、インターバルを開始する
            if (_shootingStateType == ShootingStateType.Shooting)
            {
                _shootingIntervalTimer += Runner.DeltaTime;
                if (_shootingIntervalTimer >= _shootingInterval)
                {
                    _shootingStateType = ShootingStateType.Stance;
                    _shootingIntervalTimer = 0;
                }
            }
            
            //構えの入力が終了後
            if (!_playerInput.Buttons.IsSet(PlayerButtons.Ability2))
            {
                ApplyCameraState(ShootingStateType.None);
                _phase = AbilityPhase.Available;
                _shootingStateType = ShootingStateType.None;
                _lastShootingStateType = ShootingStateType.None;
            }
            
            //状態変化検知
            if (_shootingStateType != _lastShootingStateType)
            {
                _lastShootingStateType = _shootingStateType;
                ApplyCameraState(ShootingStateType.Stance);
            }
        }
        
        private void GunTargetDetection()
        {
            var origin = _aimCameraController.AimOrigin;
            var dir = _aimCameraController.AimDirection;
            Debug.DrawRay(origin, dir * _doubleBarreledGunDistance, Color.red);
            
            var hit = Physics.Raycast(origin, dir, out RaycastHit hitInfo, _doubleBarreledGunDistance);
            //hitがtrueなら当たった場所を渡す　falseなら最大距離を渡す
            GunShootingDetection(hit? hitInfo.point :
                origin + dir * _doubleBarreledGunDistance);
        }

        private void GunShootingDetection(Vector3 targetPos)
        {
            //左
            var originLeft = _doubleBarreledGunLeftMuzzle.position;
            var dirLeft = targetPos - originLeft;
            Debug.DrawRay(originLeft, dirLeft * _doubleBarreledGunDistance, Color.blue);
            //右
            var originRight = _doubleBarreledGunRightMuzzle.position;
            var dirRight = targetPos - originLeft;
            Debug.DrawRay(originRight, dirRight * _doubleBarreledGunDistance, Color.blue);
            
            //左右のマズルから、ヒットした場所にRayを飛ばす
            Physics.Raycast(originLeft, dirLeft, out var gunHitInfoLeft, _doubleBarreledGunDistance);
            Physics.Raycast(originRight, dirRight, out var gunHitInfoRight, _doubleBarreledGunDistance);
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
            if(hitLeft.collider.GetComponentInParent<NetworkObject>() == Parameter.Owner) return;
            if(hitRight.collider.GetComponentInParent<NetworkObject>() == Parameter.Owner) return;
            
            var damageableLeft = hitLeft.collider.GetComponentInParent<IDamageable>();
            var damageableRight =  hitRight.collider.GetComponentInParent<IDamageable>();
            GunDamage(damageableLeft);
            GunDamage(damageableRight);
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
            var hitData = new HitData(HitActionType.Damage,
                enableData ? sessionData.IsOgre ? _ogreDamage : _damage : _damage, inputAuthority,
                damageable.OwnerPlayerRef, null, damageable);
            damageable.TakeHit(ref hitData);
        }
        
        private void ApplyCameraState(ShootingStateType type)
        {
            if (type == ShootingStateType.Stance)
            {
                _aimCameraController.RPC_AimCamera();;
                _aimCameraController.RPC_CrosshairToggleChange(true);
            }
            else
            {
                _aimCameraController.RPC_NormalCamera();
                _aimCameraController.RPC_CrosshairToggleChange(false);
            }
        }
    }
}

