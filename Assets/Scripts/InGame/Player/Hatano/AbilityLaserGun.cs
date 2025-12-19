using System;
using Fusion;
using InGame.Interact;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ability
{
    [Serializable]
    public class AbilityLaserGun : AbilityBase
    {
        [Header("参照")]
        [Header("PlayerInteractionController")]
        [SerializeField] private PlayerInteractionController _playerInteractionController;
        [Header("AimCameraController")]
        [SerializeField] private AimCameraController _aimCameraController;
        [Space(30)]
        [Header("○○距離")]
        [SerializeField] private float _laserDistance;
        [Header("○○発射位置")]
        [SerializeField] private Transform _laserStartPoint;
        //[Header("○○発射インターバル")] 
        //[SerializeField] private float _intervalTime;
        //private float _intervalTimer; //インターバル用のタイマー
        [Header("○○使用時のインタラクション必要時間")] 
        [SerializeField] private float _interactionTime;
        private float _interactionTimer;
        [Header("射撃のステート")]
        [SerializeField] private ShootingStateType _shootingStateType;
        //private SessionPlayerData _data;
        /// <summary>
        /// true：撃った　false：撃ってない
        /// </summary>
        //private bool _isShoot;
        
        protected override void OnStart()
        {
            //構えている状態にする
            _shootingStateType = ShootingStateType.Stance;
            _aimCameraController.CrosshairToggleChange(true);
            //カメラをAimカメラに変更する
            _aimCameraController.CameraToggleChange();
        }

        protected override void OnUpdate(float deltaTime)
        {
            /*
            //発射直後にインターバルを開始する
            if (_isShoot)
            {
                //インターバル時間を過ぎたら、再度発射を可能にする
                _intervalTimer = Time.deltaTime;
                if (_intervalTimer >= _intervalTime)
                {
                    _isShoot = false;
                    _intervalTimer = 0;
                }
            }
            */
            
            //構えている状態
            if (_shootingStateType == ShootingStateType.Stance)
            {
                //var origin = _aimCameraController.MainCamera.transform.position;
                //var dir = _aimCameraController.MainCamera.transform.forward;
                //Debug.DrawRay(origin, dir * _laserDistance, Color.red);
                
                //撃つ入力をしている時、Rayを飛ばす
                if (_playerInput.Buttons.IsSet(PlayerButtons.Shooting))
                {
                    LaserTargetDetection();
                }
            }
            
            //構える入力を離したら、アビリティを終了する
            if (!_playerInput.Buttons.IsSet(PlayerButtons.Ability1))
            {
                _phase = AbilityPhase.Ending;
                _aimCameraController.CameraToggleChange();
                _aimCameraController.CrosshairToggleChange(false);
            }
        }

        public override void OnUpdateLocal(float deltaTime, GameObject owner)
        {
            
        }

        /// <summary>
        /// Rayを飛ばす
        /// カメラ基準
        /// </summary>
        private void LaserTargetDetection()
        {
            var origin = _aimCameraController.MainCamera.transform.position;
            var dir = _aimCameraController.MainCamera.transform.forward;
            Debug.DrawRay(origin, dir * _laserDistance, Color.red);
            
            var hit = Physics.Raycast(origin, dir, out RaycastHit hitInfo, _laserDistance);
            //hitがtrueなら当たった場所を渡す　falseなら最大距離を渡す
            LaserShootingDetection(hit? hitInfo.point :
                origin + dir * _laserDistance);
        }

        /// <summary>
        /// インタラクトオブジェクトに当たったか判定する
        /// 当たっていたら、インタラクションを行う（キーを入力している状態のこと）
        /// </summary>
        /// <param name="targetPos">Rayの当たった場所</param>
        private void LaserShootingDetection(Vector3 targetPos)
        {
            var origin = _laserStartPoint.position;
            var dir = targetPos - origin;
            Debug.DrawRay(origin, dir * _laserDistance, Color.blue);
            
            //hitした場所に向かってRayを飛ばす
            var laserPoint = Physics.Raycast(origin, dir, out var laseHitInfo, _laserDistance);
            if (laserPoint)
            {
                //ヒットしたオブジェクトのInteractableBaseを取得する
                var hit = laseHitInfo.collider.gameObject;
                var interactable = hit.GetComponentInParent<InteractableBase>()
                        ?? hit.GetComponent<InteractableBase>()
                        ?? hit.GetComponentInChildren<InteractableBase>();
                if (interactable != null)
                {
                    //インタラクションを行う
                    _playerInteractionController.RemoteInteraction(
                        ref _interactionTimer, _interactionTime, interactable);
                }
            }
        }
    }
}
