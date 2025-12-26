using System;
using InGame.Interact;
using InGame.Player.Hatano;
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
        [Header("○○使用時のインタラクション必要時間")] 
        [SerializeField] private float _interactionTime;
        private float _interactionTimer;
        [Header("射撃のステート")]
        [SerializeField] private ShootingStateType _shootingStateType;
        [Header("判定を取るためのBoxの大きさ")]
        [SerializeField] private Vector3 _judgmentBoxSize;
        
        protected override void OnStart()
        {
            //構えている状態にする
            _shootingStateType = ShootingStateType.Stance;
            _aimCameraController.CrosshairToggleChange(true);
            //カメラをAimカメラに変更する
            _aimCameraController.AimCamera();
        }

        protected override void OnUpdate(float deltaTime)
        {
            //現在のAbilityが遠距離インタラクションの場合、処理を通す
            if(AbilityStatusManagement.instance.AbilityStatus != HatanoAbilityStatus.RemoteInteraction) return;
            
            //構えている状態
            if (_shootingStateType == ShootingStateType.Stance)
            {
                //撃つ入力をしている時、Rayを飛ばす
                if (_playerInput.Buttons.IsSet(PlayerButtons.Shooting))
                {
                    LaserTargetDetection();
                }
                else if (!_playerInput.Buttons.IsSet(PlayerButtons.Shooting)) //撃つ入力を離したらインタラクションを中止する
                {
                    _playerInteractionController.RemoteInteractionCancel(ref _interactionTimer);
                }
            }
            
            //構える入力を離したら、アビリティを終了する
            if (!_playerInput.Buttons.IsSet(PlayerButtons.Ability2))
            {
                _phase = AbilityPhase.Ending;
                _aimCameraController.NormalCamera();
                _aimCameraController.CrosshairToggleChange(false);
                
                //一応、インタラクション中にエイムを辞めたときもインタラクションを中止する
                _playerInteractionController.RemoteInteractionCancel(ref _interactionTimer);
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
                //Rayが当たった場所のColliderを取得して、小さいインタラクションオブジェクトも取得出来るようにする
                Collider[] boxColliders = Physics.OverlapBox(
                    laseHitInfo.point,
                    _judgmentBoxSize,
                    Quaternion.identity);
                
                //インタラクションオブジェクトを取得する
                InteractableBase interactableBase = null;
                foreach (var collider in boxColliders)
                {
                    var obj = collider.gameObject;
                    interactableBase = obj.GetComponentInParent<InteractableBase>()
                                       ?? obj.GetComponent<InteractableBase>()
                                       ?? obj.GetComponentInChildren<InteractableBase>();
                    //一番最初にインタラクションオブジェクトを見つけ次第、ループを抜ける
                    if(interactableBase != null) break;
                }

                //インタラクションオブジェクトを取得出来た場合、インタラクションを行う
                if (interactableBase != null)
                {
                    _playerInteractionController.RemoteInteraction(
                        ref _interactionTimer, _interactionTime, interactableBase);
                }
                else //インタラクションを中止する
                {
                    _playerInteractionController.RemoteInteractionCancel(ref _interactionTimer);
                }
            }
        }
    }
}
