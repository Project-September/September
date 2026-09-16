using UniRx;
using UniRx.Triggers;
using CRISound;
using Cysharp.Threading.Tasks;
using Fusion;
using September.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace September.Lobby
{
    public class PlayerCharacterSelect : CharacterSelectBase
    {
        [SerializeField] private Button _submitButton;
        [SerializeField] private CharacterInfoPanel _frontCharacterInfoPanel;
        [SerializeField] private CharacterInfoPanel _backCharacterInfoPanel;
        //[SerializeField] private CharacterDisplay _characterDisplay;
        [SerializeField] private TextureCharacterDisplay _characterDisplay;
        [SerializeField] private ToggleTweenAnimation _toggleTweenAnimation;
        [SerializeField] private Button _closeExplainButton;
        
        [SerializeField] private Image _selectedCharacterImage;

        [SerializeField, Tooltip("アイコンを配置するGrid Layout Group。未設定なら最初のアイコンの親から取得します。")]
        private GridLayoutGroup _iconLayoutGroup;
        private Vector2[] _navigationPositions;
        private bool _navigationWasVisible;
        private bool _isConfirming;
        private Transform _confirmationArrow;

        private CharacterInfoPanel _currentFrontPanel;
        private CharacterInfoPanel _currentBackPanel;
        private void Start()
        {
            _localPlayerRef = NetworkRunner.GetRunnerForScene(SceneManager.GetActiveScene()).LocalPlayer;
            // 決定ボタンの矢印だけを制御し、選択イベントだけでは表示させない。
            _confirmationArrow = _submitButton.transform.Find("Arrow");
            if (_confirmationArrow) _confirmationArrow.gameObject.SetActive(false);
            _submitButton.interactable = false;
            _currentFrontPanel = _frontCharacterInfoPanel;
            _currentBackPanel = _backCharacterInfoPanel;
            _submitButton.onClick.AddListener(() =>
            {
                SubmitCharacter();
                SetConfirmationPhase(false);
            });
            _submitButton.OnCancelAsObservable().Subscribe(eventData =>
            {
                if (!_isConfirming || _selectCharacterIcons.Count == 0) return;
                SetConfirmationPhase(false);
                _selectCharacterIcons[_currentCharacterIndex].Button.Select();
                eventData.Use();
            }).AddTo(this);
            Initialize().Forget();
        }

        private async UniTaskVoid Initialize()
        {
            await UniTask.Delay(1);
            var characterNames = CharacterDataContainer.Instance.GetNames();
            CreateCharacterIcons(characterNames);
            SetCharacterIconsNavigation();
            _currentCharacterName = characterNames[0];
            _currentCharacterIndex = 0;
            _selectedCharacterImage.sprite = CharacterDataContainer.Instance.GetCharacterData(0).CharacterPortrait;
            _characterDisplay.SetCharacter(0);
            var data = CharacterDataContainer.Instance.GetCharacterData(0);
            
            _currentBackPanel.ApplyContents (data.DisplayName, data.AbilityName, data.AbilityExplain);
            _currentFrontPanel.ApplyContents (data.DisplayName, data.AbilityName, data.AbilityExplain);
            SetConfirmationPhase(false);
            _closeExplainButton.onClick.AddListener(() =>
            {
               //ボイス鳴らす
                CRIAudio.PlaySE("ALLCue", CharacterDataContainer.Instance.GetCharacterData(_currentCharacterIndex).SelectedVoice);
            });
        }

        protected override void OnCharacterIconClick(string characterName, int index)
        {
            var data = CharacterDataContainer.Instance.GetCharacterData(index);

            _characterDisplay.SetCharacter(index);
            //  表示を切り替え
            ChangeCharacterInfo(data.DisplayName, data.AbilityName, data.AbilityExplain);
            _selectedCharacterImage.sprite = data.CharacterPortrait;
        }
        private void SetCharacterIconsNavigation()
        {
            if (_selectCharacterIcons.Count == 0) return;
            if (!_iconLayoutGroup)
                _iconLayoutGroup = _selectCharacterIcons[0].GetComponentInParent<GridLayoutGroup>(true);
            if (!_iconLayoutGroup)
            {
                Debug.LogError("PlayerCharacterSelectにアイコン配置用のGridLayoutGroupを設定してください。", this);
                return;
            }
            // Gridの列数・開始方向・開始角を反映した実際の配置から移動先を決める。
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_iconLayoutGroup.transform);
            _navigationPositions = new Vector2[_selectCharacterIcons.Count];
            RefreshIconNavigation();
        }

        private Vector2 GetIconPosition(int index)
        {
            var rect = (RectTransform)_selectCharacterIcons[index].Button.transform;
            Vector3 center = rect.TransformPoint(rect.rect.center);
            // 共通の座標系で比較し、Canvasの拡縮や入れ子の配置にも対応する。
            Transform layout = _iconLayoutGroup ? _iconLayoutGroup.transform : transform;
            return layout.InverseTransformPoint(center);
        }

        private void OnEnable()
        {
            // このコンポーネントごと再表示された場合も、初期選択をやり直す。
            _navigationWasVisible = false;
        }

        private void OnDisable()
        {
            _isConfirming = false;
            if (_confirmationArrow) _confirmationArrow.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_navigationPositions == null || _selectCharacterIcons.Count == 0) return;
            if (!_selectCharacterIcons[0].gameObject.activeInHierarchy)
            {
                _navigationWasVisible = false;
                return;
            }
            if (!_navigationWasVisible)
            {
                _navigationWasVisible = true;
                SetConfirmationPhase(false);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_iconLayoutGroup.transform);
                RefreshIconNavigation();
                // 表示された最初のフレームに、先頭アイコンへ十字キー操作の選択を合わせる。
                _selectCharacterIcons[0].Button.Select();
            }
            // キャラ選択中に背景クリックでフォーカスが外れても、十字キー操作を継続する。
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (!_isConfirming && eventSystem && !eventSystem.currentSelectedGameObject &&
                _selectCharacterIcons[_currentCharacterIndex].Button.IsInteractable())
                _selectCharacterIcons[_currentCharacterIndex].Button.Select();

            // パネルを開いた後や画面サイズ変更後に配置が変わった場合だけ再設定する。
            for (int i = 0; i < _navigationPositions.Length; i++)
            {
                if ((GetIconPosition(i) - _navigationPositions[i]).sqrMagnitude <= 0.01f) continue;
                RefreshIconNavigation();
                break;
            }
        }

        private void RefreshIconNavigation()
        {
            for (int i = 0; i < _navigationPositions.Length; i++)
                _navigationPositions[i] = GetIconPosition(i);
            for (int i = 0; i < _navigationPositions.Length; i++)
            {
                var button = _selectCharacterIcons[i].Button;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = FindIconInDirection(i, Vector2.up);
                nav.selectOnDown = FindIconInDirection(i, Vector2.down);
                nav.selectOnLeft = FindIconInDirection(i, Vector2.left);
                // 方向入力はアイコン間だけを移動し、決定フェーズには進まない。
                nav.selectOnRight = FindIconInDirection(i, Vector2.right);
                button.navigation = nav;
            }
        }

        private Button FindIconInDirection(int origin, Vector2 direction)
        {
            Button nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < _navigationPositions.Length; i++)
            {
                if (i == origin) continue;
                var button = _selectCharacterIcons[i].Button;
                if (!button.isActiveAndEnabled || !button.IsInteractable()) continue;
                Vector2 delta = _navigationPositions[i] - _navigationPositions[origin];
                float forward = Vector2.Dot(delta, direction);
                float sideways = Mathf.Abs(Vector2.Dot(delta, new Vector2(-direction.y, direction.x)));
                // 同じ行・列の最も近いアイコンを選ぶ。端や欠けたセルから斜めには移動しない。
                // 位置計算の丸め誤差だけを許容する。
                if (forward <= 0.01f || sideways > 0.1f) continue;
                if (forward >= nearestDistance) continue;
                nearestDistance = forward;
                nearest = button;
            }
            return nearest;
        }
        /// <summary>
        /// キャラクターの表示を切り替える(アニメーション)
        /// </summary>
        private void ChangeCharacterInfo(string characterName, string abilityName, string abilityExplain)
        {
            // 前の選択のTweenと待機処理を止め、新しい内容の表示を開始する。
            _currentFrontPanel.CancelAnimation();
            _currentBackPanel.CancelAnimation();
            _currentFrontPanel.FadeOut().Forget();
            _currentBackPanel.FadeIn(characterName, abilityName, abilityExplain).Forget();
            // 非同期完了を待たず表示先を更新し、古い処理による入れ替えを防ぐ。
            (_currentFrontPanel, _currentBackPanel) = (_currentBackPanel, _currentFrontPanel);
        }

        private void PreviewCharacter(int index)
        {
            if (_isConfirming) return;
            var data = CharacterDataContainer.Instance.GetCharacterData(index);
            bool changed = _currentCharacterIndex != index || string.IsNullOrEmpty(_currentCharacterName);
            _currentCharacterIndex = index;
            _currentCharacterName = data.DisplayName;
            // 既存の拡大処理は乗算なので、先に全て戻してから選択中の一つだけ拡大する。
            foreach (var icon in _selectCharacterIcons)
            {
                icon.DeselectCharacter();
                var frame = icon.transform.Find("SelectFrame")?.GetComponent<Image>();
                if (frame) frame.enabled = icon == _selectCharacterIcons[index];
            }
            _selectCharacterIcons[index].SelectCharacter();
            if (!changed) return;
            CRIAudio.PlaySE("ALLCue", data.SelectedVoice);
            OnCharacterIconClick(data.DisplayName, index);
        }

        protected override bool HandleIconSubmit(int index)
        {
            if (_isConfirming) return true;
            // アイコンの決定は確認ボタンへの移動。ネットワークへの確定は確認ボタンで行う。
            PreviewCharacter(index);
            SetConfirmationPhase(true);
            _submitButton.Select();
            // フォーカス移動で消されるフレームを戻し、選択キャラを示し続ける。
            var frame = _selectCharacterIcons[index].transform.Find("SelectFrame")?.GetComponent<Image>();
            if (frame) frame.enabled = true;
            return true;
        }

        private void SetConfirmationPhase(bool confirming)
        {
            // 決定中はキャラを変更できなくし、戻る入力で選択を再開する。
            _isConfirming = confirming;
            // 決定フェーズ以外は、Imageの状態にかかわらず矢印自体を非表示にする。
            if (_confirmationArrow) _confirmationArrow.gameObject.SetActive(confirming);
            _submitButton.interactable = confirming;
            foreach (var icon in _selectCharacterIcons)
                icon.Button.interactable = !confirming;
            if (!confirming && _navigationPositions != null) RefreshIconNavigation();
        }

        protected override void SelectCharacterIconSetting(SelectCharacterIcon characterIcon, int index)
        {
            characterIcon.Button.OnSelectAsObservable()
                .Subscribe(_ => PreviewCharacter(index)).AddTo(characterIcon);
            if (index != 0) return;
            // 決定中の方向入力では選択フェーズへ戻さず、Cancelで戻す。
            _submitButton.navigation = new Navigation { mode = Navigation.Mode.None };
            _toggleTweenAnimation.SelectWhenOpen = characterIcon.Button;
        }
    }
}
