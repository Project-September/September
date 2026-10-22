using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

namespace InGame.Player.Hatano
{
    public class HatanoAbilityStatusUIManager : NetworkBehaviour
    {
        [Header("選択中のAbilityを表示するUI")]
        [SerializeField] private GameObject _selectedAbilityUIPrefab;
        private GameObject _selectedAbilityUIObject;
        private TextMeshProUGUI  _selectedAbilityUIText;

        /// <summary>
        /// 各Abilityの名前を保持
        /// </summary>
        private readonly Dictionary<HatanoAbilityStatus, string> _abilityStatusNames = new()
        {
            {HatanoAbilityStatus.DoubleBarreledGun, "2丁銃" },
            {HatanoAbilityStatus.LaserGun, "遠距離インタラクション"},
            {HatanoAbilityStatus.RocketLauncher, "ロケットランチャー"},
            {HatanoAbilityStatus.None, "装備なし"}
        };

        public override void Spawned()
        {
            if (HasInputAuthority)
            {
                //UIの生成
                _selectedAbilityUIObject = Instantiate(_selectedAbilityUIPrefab);
                var textObj = _selectedAbilityUIObject.transform.GetChild(0).gameObject;
                _selectedAbilityUIText = textObj.GetComponent<TextMeshProUGUI>();
                _selectedAbilityUIText.text = _abilityStatusNames[HatanoAbilityStatus.None];
            }
        }

        /// <summary>
        /// AbilityUIの表示を更新する
        /// </summary>
        /// <param name="status">選択したAbility</param>
        public void SelectedAbilityUITextChanged(HatanoAbilityStatus status)
        {
            if(!HasInputAuthority) return;
            _selectedAbilityUIText.text = _abilityStatusNames[status];
        }
    }
}
