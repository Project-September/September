using Cysharp.Threading.Tasks;
using Fusion;
using September.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace September.Lobby
{
    public class SelectCharacterPanel : MonoBehaviour
    {
        [SerializeField] private Button _selectButtonPrefab;
        [SerializeField] private Button _submitButton;
        [SerializeField] private CharacterInfoPanel _frontCharacterInfoPanel;
        [SerializeField] private CharacterInfoPanel _backCharacterInfoPanel;
        [SerializeField] private Transform _content;
        [SerializeField] private CharacterDisplay _characterDisplay;
        private CharacterInfoPanel _currentFrontPanel;
        private CharacterInfoPanel _currentBackPanel;
        private bool _changeCharacterInfo;
        private string _currentCharacterName;
        private int _currentCharacterIndex;
        private PlayerRef _localPlayerRef;
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
            for (int i = 0; i < characterNames.Length; i++)
            {
                var btn = Instantiate(_selectButtonPrefab, _content);
                var temp = i;
                btn.onClick.AddListener(()=>SelectCharacter(characterNames[temp], temp));
            }
            _currentCharacterName = characterNames[0];
            _currentCharacterIndex = 0;
            var data = CharacterDataContainer.Instance.GetCharacterData(0);
            _currentFrontPanel.PlayVideo(data.ExplainVideo).Forget();
            _characterDisplay.SetCharacter(0);
        }
        /// <summary>
        /// キャラクターの表示を切り替える(アニメーション)
        /// </summary>
        private async UniTaskVoid ChangeCharacterInfo(string characterName, VideoClip videoClip)
        {
            _changeCharacterInfo = true;
            _currentCharacterName = characterName;
            _currentFrontPanel.StopVideo();
            _currentFrontPanel.FadeOut().Forget();
            _currentBackPanel.PlayVideo(videoClip).Forget();
            await _currentBackPanel.FadeIn(characterName);
            (_currentFrontPanel, _currentBackPanel) = (_currentBackPanel, _currentFrontPanel);
            _changeCharacterInfo = false;
        }
        /// <summary>
        /// キャラクターを選択(クリック)した時の動作
        /// </summary>
        private void SelectCharacter(string characterName, int index)
        {
            if (_changeCharacterInfo || _currentCharacterName == characterName) return;
            var data = CharacterDataContainer.Instance.GetCharacterData(index);
            //  表示を切り替え
            ChangeCharacterInfo(characterName, data.ExplainVideo).Forget();
            //  選択しているキャラクターのインデックスを控える
            _currentCharacterIndex = index;
            _characterDisplay.SetCharacter(_currentCharacterIndex);
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