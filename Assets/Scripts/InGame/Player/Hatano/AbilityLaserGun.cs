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
        [SerializeField] private PlayerInteractionController _playerInteractionController;
        
        [Header("○○距離")]
        [SerializeField] private float _laserDistance;
        [Header("○○発射位置")]
        [SerializeField] private Transform _laserStartPoint;
        
        [Header("射撃のステート")]
        [SerializeField] private ShootingStateType _shootingStateType;

        private SessionPlayerData _data;
        
        /// <summary>
        /// true：撃った　false：撃ってない
        /// </summary>
        private bool _isShoot;
        
        //TODO：入力をしているが、アビリティが発動しない
        
        protected override void OnStart()
        {
            //構えている状態にする
            _shootingStateType = ShootingStateType.Stance;
            Debug.Log("構える");
        }

        protected override void OnUpdate(float deltaTime)
        {
            //構えている状態
            if (_shootingStateType == ShootingStateType.Stance)
            {
                //撃つ入力をしていて撃っていない時、Rayを飛ばす
                if (_playerInput.Buttons.IsSet(PlayerButtons.Ability2) && !_isShoot)
                {
                    Debug.Log("Rayを飛ばす");
                }
                
                //離したらフラグをリセットして、再度撃てるようにする
                if (!_playerInput.Buttons.IsSet(PlayerButtons.Ability2) && _isShoot)
                {
                    _isShoot = false;
                    Debug.Log("ボタンを離した");
                }
            }
            
            //構える入力を離したら、アビリティを終了する
            if (!_playerInput.Buttons.IsSet(PlayerButtons.Ability1))
            {
                _phase = AbilityPhase.Ending;
            }
        }

        public override void OnUpdateLocal(float deltaTime, GameObject owner)
        {
            
        }

        /// <summary>
        /// 撃Rayを飛ばして判定を取る
        ///インタラクトオブジェクトに当たったときにインタラクト
        /// </summary>
        private void LaserShooting()
        {
            //修正
            //インタラクションが出来るんじゃなくて、インタラクションを判定を得る
            //→インタラクションできるゲームオブジェクトに近づいてキーを押している状態のこと
            
            //Rayを飛ばして、インタラクトオブジェクトを取得する
            if (Physics.Raycast(_laserStartPoint.position, _laserStartPoint.transform.forward, out RaycastHit hit))
            {
                //InteractableBaseを取得する
                if(hit.collider.gameObject.TryGetComponent<InteractableBase>(out InteractableBase interactableBase))
                {
                    //インタラクションするキャラクターを取得
                    var interactor = _playerInteractionController.Object.InputAuthority.RawEncoded;
                    if (PlayerDatabase.Instance.PlayerDataDic.TryGet(PlayerRef.FromEncoded(interactor), out _data))
                    {
                        //インタラクションを行う
                        var context = new InteractableContext
                        {
                            Interactor = interactor,
                            CharacterType = _data.CharacterType,
                        };
                        interactableBase.Interact(context);
                    }
                }
            }
        }
    }
}
