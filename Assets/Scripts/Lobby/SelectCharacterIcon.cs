using UnityEngine;
using UnityEngine.UI;

namespace September.Lobby
{
    public class SelectCharacterIcon : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _characterImage;
        [SerializeField] private Image _selectImage;
        public Button Button => _button;
        public Image CharacterImage => _characterImage;

        private void Awake()
        {
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
            if(_selectImage) _selectImage.enabled = true;
        }

        public void DeselectCharacter()
        {
            if(_selectImage) _selectImage.enabled = false;
        }
    }
}