using Fusion;
using UnityEngine;
using September.Common;
using TMPro;

namespace InGame.Player.Hatano
{
    /// <summary>
    /// ハタノ・エリクソンのAbility選択状態を管理する
    /// </summary>
    public class AbilityStatusManagement : NetworkBehaviour
    {
        public static AbilityStatusManagement instance;

        [Header("現在の選択中のAbility")]
        [SerializeField] private HatanoAbilityStatus _abilityStatus;
        public HatanoAbilityStatus AbilityStatus => _abilityStatus;
        
        private AbilityStatusUIManager _abilityStatusUIManager;
        private AimCameraController _aimCameraController;
        private bool _isChangeAbilityInput; //Abilityの変更入力

        private void Awake()
        {
            if (instance == null) instance = this;
            _abilityStatus = HatanoAbilityStatus.None;
            
            _abilityStatusUIManager = GetComponent<AbilityStatusUIManager>();
            _aimCameraController = GetComponent<AimCameraController>();
        }

        public override void FixedUpdateNetwork()
        {
            //入力がなかったら処理を行わない
            if (!GetInput<PlayerInput>(out var input)) return;
            //構えているときはアビリティの変更を行えないようにする
            if(_aimCameraController.IsAim) return;

            //アビリティの入力でアビリティの変更を行う
            if (input.Buttons.IsSet(PlayerButtons.Ability1) && !_isChangeAbilityInput)
            {
                _isChangeAbilityInput = true;
                //現在のアビリティに応じて、アビリティを変更する
                switch (_abilityStatus)
                {
                    case HatanoAbilityStatus.None:
                        var abilityD1 = HatanoAbilityStatus.DoubleBarreledGun;
                        ChangeAbilityStatus(abilityD1);
                        _abilityStatusUIManager.SelectedAbilityUITextChanged(abilityD1);
                        break;
                    
                    case HatanoAbilityStatus.DoubleBarreledGun:
                        var abilityRi = HatanoAbilityStatus.RemoteInteraction;
                        ChangeAbilityStatus(abilityRi);
                        _abilityStatusUIManager.SelectedAbilityUITextChanged(abilityRi);
                        break;
                    
                    case HatanoAbilityStatus.RemoteInteraction:
                        var abilityRl = HatanoAbilityStatus.RocketLauncher;
                        ChangeAbilityStatus(abilityRl);
                        _abilityStatusUIManager.SelectedAbilityUITextChanged(abilityRl);
                        break;
                    
                    case HatanoAbilityStatus.RocketLauncher:
                        var abilityD2 = HatanoAbilityStatus.DoubleBarreledGun;
                        ChangeAbilityStatus(abilityD2);
                        _abilityStatusUIManager.SelectedAbilityUITextChanged(abilityD2);
                        break;
                }
            }

            //再度Abilityの変更を行えるようにする
            if (!input.Buttons.IsSet(PlayerButtons.Ability1) && _isChangeAbilityInput)
            {
                _isChangeAbilityInput = false;
            }
        }

        /// <summary>
        /// 現在の選択中のアビリティを変更する
        /// </summary>
        /// <param name="abilityStatus">変更するアビリティ</param>
        private void ChangeAbilityStatus(HatanoAbilityStatus abilityStatus)
        {
            _abilityStatus = abilityStatus;
        }
    }
}
