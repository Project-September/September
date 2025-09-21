using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using September.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace September.Lobby
{
    public class SelectCharacterPanel : MonoBehaviour
    {
        [SerializeField] private SelectCharacterIcon _selectButtonPrefab;
        [SerializeField] private Button _submitButton;
        [SerializeField] private CharacterInfoPanel _frontCharacterInfoPanel;
        [SerializeField] private CharacterInfoPanel _backCharacterInfoPanel;
        [SerializeField] private Transform _content;
        [SerializeField] private CharacterDisplay _characterDisplay;
        [SerializeField] private ToggleTweenAnimation _toggleTweenAnimation;
        private CharacterInfoPanel _currentFrontPanel;
        private CharacterInfoPanel _currentBackPanel;
        private bool _changeCharacterInfo;
        private string _currentCharacterName;
        private int _currentCharacterIndex;
        private PlayerRef _localPlayerRef;
        private readonly List<SelectCharacterIcon> _selectCharacterIcons = new();
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
        }

        private void CreateCharacterIcons(string[] characterNames)
        {
            for (int i = 0; i < characterNames.Length; i++)
            {
                var selectCharacterIcon = Instantiate(_selectButtonPrefab, _content);
                var data = CharacterDataContainer.Instance.GetCharacterData(i);
                selectCharacterIcon.CharacterImage.sprite = data.CharacterIcon;
                _selectCharacterIcons.Add(selectCharacterIcon);
                var temp = i;
                if (i == 0)
                {
                    selectCharacterIcon.SelectCharacter();
                    var nav = _submitButton.navigation;
                    nav.selectOnLeft = selectCharacterIcon.Button;
                    _submitButton.navigation = nav;
                    _toggleTweenAnimation.SelectWhenOpen = selectCharacterIcon.Button;
                    EventSystem.current.SetSelectedGameObject(selectCharacterIcon.gameObject);
                }
                selectCharacterIcon.Button.onClick.AddListener(()=>
                {
                    if (!SelectCharacter(characterNames[temp], temp)) return;
                    foreach (var icon in _selectCharacterIcons)
                    {
                        if (icon == selectCharacterIcon)
                            icon.SelectCharacter();
                        else
                            icon.DeselectCharacter();
                    }
                });
            }
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
        /// <summary>
        /// キャラクターを選択(クリック)した時の動作
        /// </summary>
        private bool SelectCharacter(string characterName, int index)
        {
            if (_changeCharacterInfo || _currentCharacterName == characterName) return false;
            var data = CharacterDataContainer.Instance.GetCharacterData(index);
            //  表示を切り替え
            ChangeCharacterInfo(characterName, data.AbilityName, data.AbilityExplain).Forget();
            //  選択しているキャラクターのインデックスを控える
            _currentCharacterIndex = index;
            _characterDisplay.SetCharacter(_currentCharacterIndex);
            return true;
        }
        /// <summary>
        /// キャラクターを決定した時の動作
        /// </summary>
        private void SubmitCharacter()
        {
            var data = CharacterDataContainer.Instance.GetCharacterData(_currentCharacterIndex);
            PlayerDatabase.Instance.Rpc_SetCharacter(_localPlayerRef, data.Type);
        }
    }
}