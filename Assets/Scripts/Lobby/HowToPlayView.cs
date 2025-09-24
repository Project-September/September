using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace September.Lobby
{
    public class HowToPlayView : MonoBehaviour
    {
        [SerializeField] GameObject _howToPlayPanel;
        [SerializeField] Button _closeButton;
        [SerializeField] Selectable _selectWhenShow;
        [SerializeField] Selectable _selectWhenHide;
        private GameInput _gameInput;

        private void Awake()
        {
            _gameInput = GameInput.I;
            _closeButton.onClick.AddListener(CloseHowToPlayPanel);
            EventSystem.current.SetSelectedGameObject(_closeButton.gameObject);
        }

        private void Update()
        {
            if (_gameInput.UI.Option.triggered || _gameInput.Debug.Title.triggered)
            {
                CloseHowToPlayPanel();
            }
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(CloseHowToPlayPanel);
        }

        public void OpenHowToPlayPanel()
        {
            _howToPlayPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(_selectWhenShow.gameObject);
        }

        public void CloseHowToPlayPanel()
        {
            _howToPlayPanel.SetActive(false);
            EventSystem.current.SetSelectedGameObject(_selectWhenHide.gameObject);
        }
    }
}