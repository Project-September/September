using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InGame.UI
{
    public class OptionUI : MonoBehaviour
    {
        [SerializeField, Label("表示非表示させるUI")] private GameObject _optionUIPanel;
        [SerializeField, Label("表示時に選択するUI")] private Selectable _selectWhenOpen;

        private GameInput _gameInput;
        private bool _isShow;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _optionUIPanel.SetActive(false);
            _gameInput = GameInput.I;
        }


        private void Update()
        {
            // オプションUIの表示切り替え
            if (_gameInput.UI.Option.triggered)
            {
                Show(!_isShow);
            }
        }

        public void Show(bool isShow)
        {
            _isShow = isShow;
            if (_optionUIPanel)
                _optionUIPanel.SetActive(_isShow);
            
            if (_gameInput != null)
                _gameInput.IsInputBlockedByUI = _isShow;

            if (_isShow)
            {
                if (_optionUIPanel)
                    _optionUIPanel.transform.SetAsLastSibling();
                if (_selectWhenOpen)
                    EventSystem.current.SetSelectedGameObject(_selectWhenOpen.gameObject);
            }

            Cursor.visible = _isShow;
            Cursor.lockState = _isShow ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}