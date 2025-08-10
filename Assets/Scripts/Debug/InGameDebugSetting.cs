using InGame.Player;
using September.Common;
using September.InGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameDebugSetting : MonoBehaviour
{
    [SerializeField] private GameTimerData _gameTimerData;
    
    [SerializeField] private TMP_InputField _beforeStartTimeInputField;
    [SerializeField] private Button _applyBeforeStartTimeButton;
    [SerializeField] private TMP_InputField _ingameTimeInputField;
    [SerializeField] private Button _applyIngameTimeButton;
    [SerializeField] private Toggle _applyInfiniteStamina;
    [SerializeField] private Toggle _autoMoveToggle;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _applyBeforeStartTimeButton.onClick.AddListener( () =>
        {
            if (int.TryParse(_beforeStartTimeInputField.text, out int time))
            {
                _gameTimerData.PreStartTime = time;
            }
        });
        
        _applyIngameTimeButton.onClick.AddListener( () =>
        {
            if (float.TryParse(_ingameTimeInputField.text, out float time))
            {
                _gameTimerData.GameTime = time;
            }
        });

        _applyInfiniteStamina.isOn = false;
        _applyInfiniteStamina.onValueChanged.AddListener(isOn =>
        {
            var players = FindObjectsOfType<PlayerMovement>();
            foreach (var player in players)
            {
                player.InfiniteStamina = isOn;
            }
        });

        _autoMoveToggle.isOn = false;
        _autoMoveToggle.onValueChanged.AddListener(isOn =>
        {
            var players = FindObjectsOfType<InputProvider>();
            foreach (var player in players)
            {
                player.UseAutoMove = isOn;
            }
        });
    }
}
