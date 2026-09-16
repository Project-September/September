using UnityEngine;
using UnityEngine.UI;

namespace September.Lobby
{
    public class SelectCharacterIcon : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _characterImage;
        [SerializeField] private float _selectedScaleMultiplier = 1.2f;
        private Vector3 _originalScale;
        private Image _selectFrame;
        public Button Button => _button;
        public Image CharacterImage => _characterImage;

        private void Awake()
        {
            // 元の縦横比を保ったまま、選択状態を繰り返しても拡大率が累積しないようにする。
            _originalScale = Button.transform.localScale;
            _selectFrame = transform.Find("SelectFrame")?.GetComponent<Image>();
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
            Button.transform.localScale = _originalScale * _selectedScaleMultiplier;
            if (_selectFrame) _selectFrame.enabled = true;
        }

        public void DeselectCharacter()
        {
            // 選択解除時の縮小処理
            Button.transform.localScale = _originalScale;
            if (_selectFrame) _selectFrame.enabled = false;
        }
    }
}
