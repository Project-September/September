using TMPro;
using UnityEngine;

namespace InGame.Player.Hatano
{
    public class AbilityStatusUIManager : MonoBehaviour
    {
        [Header("選択中のAbilityを表示するUI")]
        [SerializeField] private GameObject _selectedAbilityUIPrefab;
        private GameObject _selectedAbilityUIObject;
        private TextMeshProUGUI  _selectedAbilityUIText;
        
        /// <summary>
        /// ハタノのAbilityを管理するクラス
        /// </summary>
        private HatanoAbilityStatusNameManager _hatanoAbilityStatusNameManager;

        private void Awake()
        {
            _hatanoAbilityStatusNameManager = new  HatanoAbilityStatusNameManager();
            
            //UIの生成
            if (_selectedAbilityUIObject == null)
            {
                _selectedAbilityUIObject = Instantiate(_selectedAbilityUIPrefab);
                
                var textObj = _selectedAbilityUIObject.transform.GetChild(0).gameObject;
                _selectedAbilityUIText = textObj.GetComponent<TextMeshProUGUI>();
                _selectedAbilityUIText.text =
                    _hatanoAbilityStatusNameManager.abilityStatusNames[HatanoAbilityStatus.None];
            }
        }

        /// <summary>
        /// AbilityUIの表示を更新する
        /// </summary>
        /// <param name="status">選択したAbility</param>
        public void SelectedAbilityUITextChanged(HatanoAbilityStatus status)
        {
            _selectedAbilityUIText.text = _hatanoAbilityStatusNameManager.abilityStatusNames[status];
        }
    }
}

