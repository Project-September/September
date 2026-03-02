using Common.UserSettings;
using CriWare;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InGame.UI
{
    public class OptionUI : MonoBehaviour
    {
        [SerializeField, Label("表示非表示させるUI")] private GameObject _optionUIPanel;
        [SerializeField, Label("BGMVolume")] private Slider _bgmVolumeSlider;
        [SerializeField, Label("SEVolume")] private Slider _seVolumeSlider;
        [SerializeField, Label("VoiceVolume")] private Slider _voiceVolumeSlider;
        [SerializeField] private Selectable _selectWhenOpen;

        private GameInput _gameInput;
        private bool _isShow;
        private Vector2 _prevVolumeInput;
        
        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            _bgmVolumeSlider.onValueChanged.RemoveAllListeners();
            _seVolumeSlider.onValueChanged.RemoveAllListeners();
            _voiceVolumeSlider.onValueChanged.RemoveAllListeners();
        }

        private void Initialize()
        {
            var settings = UserSettings.Load();
            
            _optionUIPanel.SetActive(false);
            _gameInput = GameInput.I;
            _bgmVolumeSlider.value = settings.BGMVolume;
            _seVolumeSlider.value = settings.SEVolume;
            _voiceVolumeSlider.value = settings.VoiceVolume;

            _bgmVolumeSlider.onValueChanged.AddListener(_ => OnChangeCriBGMVolume());
            _seVolumeSlider.onValueChanged.AddListener(_ => OnChangeCriSEVolume());
            _voiceVolumeSlider.onValueChanged.AddListener(_ => OnChangeCriVoiceVolume());
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

        private void OnChangeCriBGMVolume()
        {
            CriAtom.SetCategoryVolume("BGM", _bgmVolumeSlider.normalizedValue);
            
            var settings = UserSettings.Load();
            settings.BGMVolume = _bgmVolumeSlider.normalizedValue;
            UserSettings.Save(settings);
        }

        private void OnChangeCriSEVolume()
        {
            CriAtom.SetCategoryVolume("SE", _seVolumeSlider.normalizedValue);
            
            var settings = UserSettings.Load();
            settings.SEVolume = _seVolumeSlider.normalizedValue;
            UserSettings.Save(settings);
        }

        private void OnChangeCriVoiceVolume()
        {
            CriAtom.SetCategoryVolume("Voice", _voiceVolumeSlider.normalizedValue);
            
            var settings = UserSettings.Load();
            settings.VoiceVolume = _voiceVolumeSlider.normalizedValue;
            UserSettings.Save(settings);
        }
    }
}