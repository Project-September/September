using UnityEngine;
using UnityEngine.UI;

namespace September.Lobby
{
    public class SelectCharacterIcon : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _characterImage;
        [SerializeField] private float _selectedScaleMultiplier = 1.2f;
        private float _originalScale;
        public Button Button => _button;
        public Image CharacterImage => _characterImage;

        private void Awake()
        {
            // アイコンは正方形であることを前提としているため、X軸のスケールを基準にする
            _originalScale = Button.transform.localScale.x;
            DeselectCharacter();
        }

        public void SetNavigation(Selectable up = null, Selectable down = null, Selectable left = null, Selectable right = null)
        {
            if (_button == null) return;
            var nav = _button.navigation;
            nav.selectOnUp = up;
            nav.selectOnDown = down;
            nav.selectOnLeft = left;
            nav.selectOnRight = right;
            _button.navigation = nav;
        }

        public void SelectCharacter()
        {
            // 選択時の拡大処理
            Button.transform.localScale *= _selectedScaleMultiplier;
        }

        public void DeselectCharacter()
        {
            // 選択解除時の縮小処理
            Button.transform.localScale = new Vector3(_originalScale, _originalScale, _originalScale);
        }
    }
}