using System;
using InGame.Health;
using InGame.Interact;
using InGame.Player.Hatano;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ability
{
    [Serializable]
    public class AbilityRocketLauncher : AbilityBase
    {
        [Header("参照")]
        [Header("PlayerInteractionController")]
        [SerializeField] private PlayerInteractionController _playerInteractionController;
        [Header("AimCameraController")]
        [SerializeField] private AimCameraController _aimCameraController;
        [Space(30)]
        [Header("ロケットランチャー射程距離")]
        [SerializeField] private float _rocketLauncherDistance;
        [Header("ロケットランチャー発射位置")]
        [SerializeField] private Transform _rocketLauncherStartPos;
        [Header("ロケットランチャーの攻撃範囲")]
        [SerializeField] private float _rocketLauncherRadius;
        [Header("通常時のダメージ")]
        [SerializeField] private int _damage;
        [Header("鬼の時のダメージ")] 
        [SerializeField] private int _ogreDamage;
        [Header("射撃のステート")] 
        [SerializeField] private ShootingStateType _shootingStateType;
        
        private HatanoAbilityStatusManagement _abilityStatusManagement;
        
        protected override void OnStart()
        {
            if(_abilityStatusManagement == null) _abilityStatusManagement = 
                Parameter.Owner.GetComponent<HatanoAbilityStatusManagement>();
            
            _shootingStateType = ShootingStateType.Stance;
            _aimCameraController.CrosshairToggleChange(true);
            _aimCameraController.AimCamera();
        }

        protected override void OnUpdate(float deltaTime)
        {
            if(_abilityStatusManagement.AbilityStatus != HatanoAbilityStatus.RocketLauncher) return;
            
            if (_shootingStateType == ShootingStateType.Stance)
            {
                _aimCameraController.PlayerDirectionCamera();
                
                //一度撃ったら、アビリティを終了する
                if (_playerInput.Buttons.IsSet(PlayerButtons.Shooting))
                {
                    LauncherTargetDetection();
                    _phase = AbilityPhase.Ending;
                    _aimCameraController.NormalCamera();
                    _aimCameraController.CrosshairToggleChange(false);
                }
            }
            
            //撃つ入力を終了したら、アビリティを終了する
            if (!_playerInput.Buttons.IsSet(PlayerButtons.Ability2))
            {
                _phase = AbilityPhase.Available;
                _aimCameraController.NormalCamera();
                _aimCameraController.CrosshairToggleChange(false);
            }
        }

        private void LauncherTargetDetection()
        {
            var origin = _aimCameraController.MainCamera.transform.position;
            var dir = _aimCameraController.MainCamera.transform.forward;
            Debug.DrawRay(origin, dir * _rocketLauncherDistance, Color.red);
            
            //カメラからのRay
            var hit = Physics.Raycast(origin, dir, out RaycastHit hitInfo, _rocketLauncherDistance);
            //hitがtrueなら当たった場所を渡す　falseなら最大距離を渡す
            LauncherShootingDetection(hit? hitInfo.point :
                origin + dir * _rocketLauncherDistance);
        }

        /// <summary>
        /// ロケットランチャーを発射する
        /// </summary>
        /// <param name="targetPos">ヒットした場所</param>
        private void LauncherShootingDetection(Vector3 targetPos)
        {
            var origin = _rocketLauncherStartPos.position;
            var dir = targetPos - origin;
            Debug.DrawRay(origin, dir * _rocketLauncherDistance, Color.blue);
            
            //hitした場所に向かってRayを飛ばす（プレイヤーからのRay）
            var laserPoint = Physics.Raycast(origin, dir, out var laseHitInfo, _rocketLauncherDistance);
            //ヒットしたところにロケットランチャーを発射
            RocketLauncherRadius(laseHitInfo.point);
        }

        /// <summary>
        /// ロケットランチャーの攻撃処理
        /// </summary>
        /// <param name="targetPos">ヒットした場所</param>
        private void RocketLauncherRadius(Vector3 targetPos)
        {
            //攻撃範囲内のオブジェクトを取得
            Collider[] radiusObjs = Physics.OverlapSphere(targetPos, _rocketLauncherRadius);
            var playerInput = Parameter.Owner.InputAuthority;
            foreach (var obj in radiusObjs)
            {
                var damageable = obj.GetComponentInParent<IDamageable>();
                if(damageable == null) continue;
                
                //自身に当たっていたらスキップ
                if(damageable.OwnerPlayerRef == playerInput) continue;
                
                //ダメージ処理
                bool enableData = PlayerDatabase.Instance.PlayerDataDic.TryGet(playerInput, out var sessionData);
                var damage = _damage;
                if(enableData && sessionData.IsOgre) damage = _ogreDamage; //鬼の場合、鬼時のダメージに変更
                var hitData = new HitData(HitActionType.Damage,
                    damage, playerInput, damageable.OwnerPlayerRef, null, damageable);
                damageable.TakeHit(ref hitData);
            }
        }
    }
}
