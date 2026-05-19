using Fusion;
using September.Common;
using UnityEngine;

namespace September.Lobby
{
    public class BotCharacterSelect : CharacterSelectBase
    {
        [SerializeField] private RectTransform _mainPanel;
        [SerializeField] private Vector3 _panelOffset;
        private void Start()
        {
            var characterNames = CharacterDataContainer.Instance.GetNames();
            CreateCharacterIcons(characterNames);

            _mainPanel.gameObject.SetActive(false);
        }

        public void ShowPanel(PlayerRef player, Vector3 buttonPos)
        {
            _mainPanel.gameObject.SetActive(true);
            _localPlayerRef = player;
            _mainPanel.position = buttonPos + _panelOffset;
        }

        public void ClosePanel()
        {
            SubmitCharacter();
            _mainPanel.gameObject.SetActive(false);
        }

        protected override void OnCharacterIconClick(string characterName, int index)
        {
            var data = CharacterDataContainer.Instance.GetCharacterData(index);

        }

        protected override void SelectCharacterIconSetting(SelectCharacterIcon characterIcon, int index)
        {

        }
    }
}
