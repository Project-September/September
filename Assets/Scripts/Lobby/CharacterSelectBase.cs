using System.Collections.Generic;
using CRISound;
using Fusion;
using September.Common;
using UnityEngine;

namespace September.Lobby
{
    public abstract class CharacterSelectBase : MonoBehaviour
    {
        [SerializeField] private SelectCharacterIcon _selectButtonPrefab;
        [SerializeField] private Transform _content;

        protected bool _changeCharacterInfo;
        protected string _currentCharacterName;
        protected int _currentCharacterIndex;
        protected PlayerRef _localPlayerRef;

        protected readonly List<SelectCharacterIcon> _selectCharacterIcons = new();
        protected void CreateCharacterIcons(string[] characterNames)
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
                }
                SelectCharacterIconSetting(selectCharacterIcon, temp);

                selectCharacterIcon.Button.onClick.AddListener(() =>
                {
                    if (!SelectCharacter(characterNames[temp], temp)) return;
                    OnCharacterIconClick(characterNames[temp], temp);
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

        /// <summary>
        /// キャラクターを選択(クリック)した時の動作
        /// </summary>
        private bool SelectCharacter(string characterName, int index)
        {
            if (_changeCharacterInfo || _currentCharacterName == characterName) return false;
            var data = CharacterDataContainer.Instance.GetCharacterData(index);
            //  選択しているキャラクターのインデックスを控える
            _currentCharacterIndex = index;
            CRIAudio.PlaySE("ALLCue", data.SelectedVoice); // キャラ選択ボイス再生
            return true;
        }

        /// <summary>
        /// キャラクターを決定した時の動作
        /// </summary>
        protected void SubmitCharacter()
        {
            var data = CharacterDataContainer.Instance.GetCharacterData(_currentCharacterIndex);
            Debug.Log(PlayerDatabase.Instance);
            PlayerDatabase.Instance.Rpc_SetCharacter(_localPlayerRef, data.Type);
        }

        protected abstract void SelectCharacterIconSetting(SelectCharacterIcon characterIcon, int index);

        protected abstract void OnCharacterIconClick(string characterName, int index);
    }
}
