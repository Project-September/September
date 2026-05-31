using CRISound;
using Cysharp.Threading.Tasks;
using Fusion;
using September.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace September.Lobby
{
    public class PlayerCharacterSelect : CharacterSelectBase
    {
        [SerializeField] private Button _submitButton;
        [SerializeField] private CharacterInfoPanel _frontCharacterInfoPanel;
        [SerializeField] private CharacterInfoPanel _backCharacterInfoPanel;
        //[SerializeField] private CharacterDisplay _characterDisplay;
        [SerializeField] private TextureCharacterDisplay _characterDisplay;
        [SerializeField] private ToggleTweenAnimation _toggleTweenAnimation;
        [SerializeField] private Button _closeExplainButton;
        
        [SerializeField] private Image _selectedCharacterImage;
        [SerializeField] private Sprite[] _selectedCharacterSprites;
        
        private CharacterInfoPanel _currentFrontPanel;
        private CharacterInfoPanel _currentBackPanel;
        private void Start()
        {
            _localPlayerRef = NetworkRunner.GetRunnerForScene(SceneManager.GetActiveScene()).LocalPlayer;
            _currentFrontPanel = _frontCharacterInfoPanel;
            _currentBackPanel = _backCharacterInfoPanel;
            _submitButton.onClick.AddListener(SubmitCharacter);
            Initialize().Forget();
        }

        private async UniTaskVoid Initialize()
        {
            await UniTask.Delay(1);
            var characterNames = CharacterDataContainer.Instance.GetNames();
            CreateCharacterIcons(characterNames);
            SetCharacterIconsNavigation();
            _currentCharacterName = characterNames[0];
            _currentCharacterIndex = 0;
            _characterDisplay.SetCharacter(0);
            var data = CharacterDataContainer.Instance.GetCharacterData(0);
            
            _currentBackPanel.ApplyContents (data.DisplayName, data.AbilityName, data.AbilityExplain);
            _currentFrontPanel.ApplyContents (data.DisplayName, data.AbilityName, data.AbilityExplain);
            _closeExplainButton.onClick.AddListener(() =>
            {
               //ボイス鳴らす
                CRIAudio.PlaySE("ALLCue", data.SelectedVoice);
            });
        }

        protected override void OnCharacterIconClick(string characterName, int index)
        {
            var data = CharacterDataContainer.Instance.GetCharacterData(index);

            _characterDisplay.SetCharacter(_currentCharacterIndex);
            //  表示を切り替え
            ChangeCharacterInfo(characterName, data.AbilityName, data.AbilityExplain).Forget();
            if (index < 0 || index >= _selectedCharacterSprites.Length) return;
            _selectedCharacterImage.sprite = _selectedCharacterSprites[index];
        }
        private void SetCharacterIconsNavigation()
        {
            if (_selectCharacterIcons.Count == 1)
            {
                _selectCharacterIcons[0].SetNavigation(right: _submitButton);
                return;
            }
            for (int i = 0; i < _selectCharacterIcons.Count; i++)
            {
                if (i == 0)
                {
                    _selectCharacterIcons[i].SetNavigation(
                        down: _selectCharacterIcons[i + 1].Button, 
                        right: _submitButton);
                }
                else if (i == _selectCharacterIcons.Count - 1)
                {
                    _selectCharacterIcons[i].SetNavigation(
                        up: _selectCharacterIcons[i - 1].Button, 
                        right: _submitButton);
                }
                else
                {
                    _selectCharacterIcons[i].SetNavigation(
                        up: _selectCharacterIcons[i - 1].Button, 
                        down: _selectCharacterIcons[i + 1].Button,
                        right: _submitButton);
                }
            }
        }
        /// <summary>
        /// キャラクターの表示を切り替える(アニメーション)
        /// </summary>
        private async UniTaskVoid ChangeCharacterInfo(string characterName,string abilityName, string abilityExplain)
        {
            _changeCharacterInfo = true;
            _currentCharacterName = characterName;
            _currentFrontPanel.FadeOut().Forget();
            await _currentBackPanel.FadeIn(characterName, abilityName, abilityExplain);
            (_currentFrontPanel, _currentBackPanel) = (_currentBackPanel, _currentFrontPanel);
            _changeCharacterInfo = false;
        }

        protected override void SelectCharacterIconSetting(SelectCharacterIcon characterIcon, int index)
        {
            if (index != 0) return;
            var nav = _submitButton.navigation;
            nav.selectOnLeft = characterIcon.Button;
            _submitButton.navigation = nav;
            _toggleTweenAnimation.SelectWhenOpen = characterIcon.Button;
        }
    }
}
