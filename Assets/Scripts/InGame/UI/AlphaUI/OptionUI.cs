using System;
using Common.UserSettings;
using CriWare;
using NaughtyAttributes;
using UniRx;
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
                _isShow = !_isShow;
                _optionUIPanel.SetActive(_isShow);

                if (_isShow)
                {
                    _optionUIPanel.transform.SetAsLastSibling();
                    EventSystem.current.SetSelectedGameObject(_selectWhenOpen.gameObject);
                }
                
                Cursor.visible = _isShow;
                Cursor.lockState = _isShow ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }
    }
}