using System;
using Common.UserSettings;
using CriWare;
using NaughtyAttributes;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.UI
{
    public class OptionControl : MonoBehaviour
    {
        [SerializeField, Label("BGMVolume")] private Slider _bgmVolumeSlider;
        [SerializeField, Label("SEVolume")] private Slider _seVolumeSlider;
        [SerializeField, Label("VoiceVolume")] private Slider _voiceVolumeSlider;
        [SerializeField, Label("CameraSensitivity")] private Slider _cameraSensitivitySlider;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            var settings = UserSettings.Get();

            _bgmVolumeSlider.value = settings.BGMVolume;
            _seVolumeSlider.value = settings.SEVolume;
            _voiceVolumeSlider.value = settings.VoiceVolume;
            _cameraSensitivitySlider.value = settings.MouseSensitivity;


            SubscribeVolumeSliderSetting(_bgmVolumeSlider, "BGM", (v, s) => s.BGMVolume = v);
            SubscribeVolumeSliderSetting(_seVolumeSlider, "SE", (v, s) => s.SEVolume = v);
            SubscribeVolumeSliderSetting(_voiceVolumeSlider, "Voice", (v, s) => s.VoiceVolume = v);

            SubscribeSliderSetting(_cameraSensitivitySlider, (v, s) =>
            {
                s.MouseSensitivity = v;
                s.PadSensitivity = v;
            });
        }
        private static void SubscribeVolumeSliderSetting(Slider slider, string category, Action<float, UserSettings> paramUpdate)
        {
            slider
                .OnValueChangedAsObservable()
                .Subscribe(value =>
                {
                    CriAtom.SetCategoryVolume(category, value);

                    var settings = UserSettings.Get();
                    paramUpdate?.Invoke(value, settings);
                    UserSettings.Save(settings);
                })
                .AddTo(slider);
        }

        private static void SubscribeSliderSetting(Slider slider, Action<float, UserSettings> paramUpdate)
        {
            slider
                .OnValueChangedAsObservable()
                .Subscribe(value =>
                {
                    var settings = UserSettings.Get();
                    paramUpdate?.Invoke(value, settings);
                    UserSettings.Save(settings);
                })
                .AddTo(slider);
        }
    }
}
