using System;
using InGame.Health;
using InGame.Interact;
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
        
        protected override void OnStart()
        {
            _shootingStateType = ShootingStateType.Stance;
            _aimCameraController.CrosshairToggleChange(true);
            _aimCameraController.CameraToggleChange();
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_shootingStateType == ShootingStateType.Stance)
            {
                //一度撃ったら、アビリティを終了する
                if (_playerInput.Buttons.IsSet(PlayerButtons.Shooting))
                {
                    LauncherTargetDetection();
                    _phase = AbilityPhase.Ending;
                    _aimCameraController.CameraToggleChange();
                    _aimCameraController.CrosshairToggleChange(false);
                }
            }

            //撃つ入力を終了したら、アビリティを終了する
            if (!_playerInput.Buttons.IsSet(PlayerButtons.Ability2))
            {
                _phase = AbilityPhase.Ending;
                _aimCameraController.CameraToggleChange();
                _aimCameraController.CrosshairToggleChange(false);
            }
        }

        public override void OnUpdateLocal(float deltaTime, GameObject owner)
        {
            
        }
        
        private void LauncherTargetDetection()
        {
            var origin = _aimCameraController.MainCamera.transform.position;
            var dir = _aimCameraController.MainCamera.transform.forward;
            Debug.DrawRay(origin, dir * _rocketLauncherDistance, Color.red);
            
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
            
            //hitした場所に向かってRayを飛ばす
            var laserPoint = Physics.Raycast(origin, dir, out var laseHitInfo, _rocketLauncherDistance);
            if (laserPoint) //ヒットしたところにロケットランチャーを発射する
            {
                //ロケランの攻撃範囲内のプレイヤーを取得する
                RocketLauncherRadius(laseHitInfo.point);
            }
            else //ヒットしなかった場合、Rayを飛ばした方向に向かってロケットランチャーを発射
            {
                RocketLauncherRadius(laseHitInfo.point);
            }
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
                var hitData = new HitData(HitActionType.Damage,
                    enableData ? sessionData.IsOgre ? _ogreDamage : _damage : _damage, playerInput,
                    damageable.OwnerPlayerRef, null, damageable);
                damageable.TakeHit(ref hitData);
            }
        }
    }
}
