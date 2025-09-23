using InGame.Player;
using September.Common;
using September.InGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using InGame.Common;

public class InGameDebugSetting : MonoBehaviour
{
    [SerializeField] private GameTimerData _gameTimerData;
    
    [SerializeField] private TMP_InputField _beforeStartTimeInputField;
    [SerializeField] private Button _applyBeforeStartTimeButton;
    [SerializeField] private TMP_InputField _ingameTimeInputField;
    [SerializeField] private Button _applyIngameTimeButton;
    [SerializeField] private Toggle _applyInfiniteStamina;
    [SerializeField] private Toggle _autoMoveToggle;
    [SerializeField] private TMP_InputField _motionNameInputField;
    [SerializeField] private Button _playMotionButton;
    [SerializeField] private Button _logPlayableMotionButton;
    
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

        _playMotionButton.onClick.AddListener(PlayMotionOnLocalPlayer);

        // 利用可能なアニメーションクリップをログ出力
        _logPlayableMotionButton.onClick.AddListener(LogAvailableAnimations);
    }

    private void PlayMotionOnLocalPlayer()
    {
        string motionName = "";
        if (_motionNameInputField != null && !string.IsNullOrEmpty(_motionNameInputField.text))
        {
            motionName = _motionNameInputField.text;
        }
        
        if (string.IsNullOrEmpty(motionName))
        {
            Debug.LogWarning("モーション名が入力されていません");
            return;
        }

        // AnimationClipsContainerからアニメーションクリップを検索
        AnimationClip targetClip = null;
        if (AnimationClipsContainer.Instance?.AnimationMontages != null)
        {
            foreach (var montage in AnimationClipsContainer.Instance.AnimationMontages)
            {
                if (montage.AnimClip != null && montage.AnimClip.name.ToLower().Contains(motionName.ToLower()))
                {
                    targetClip = montage.AnimClip;
                    break;
                }
            }
        }

        // ローカルプレイヤーを探してモーションを再生
        var players = FindObjectsOfType<AnimationClipPlayer>();
        foreach (var animPlayer in players)
        {
            var playerComponent = animPlayer.GetComponent<PlayerManager>();
            if (playerComponent != null && playerComponent.Object.HasInputAuthority)
            {
                if (targetClip != null)
                {
                    animPlayer.PlayClip(targetClip);
                }
                else
                {
                    // フォールバック: 気絶アニメーションを再生
                    Debug.LogWarning($"アニメーションクリップが見つかりません: {motionName}");
                }
                break;
            }
        }
    }

    private void LogAvailableAnimations()
    {
        var stringBuilder = new System.Text.StringBuilder();
        if (AnimationClipsContainer.Instance?.AnimationMontages != null)
        {
            stringBuilder.AppendLine("=== 利用可能なアニメーションクリップ ===");
            foreach (var montage in AnimationClipsContainer.Instance.AnimationMontages)
            {
                if (montage.AnimClip != null)
                {
                    stringBuilder.AppendLine($"{montage.AnimClip.name}");
                }
            }
            Debug.Log(stringBuilder.ToString());
        }
        else
        {
            Debug.LogWarning("AnimationClipsContainer が見つかりません");
        }
    }
}
