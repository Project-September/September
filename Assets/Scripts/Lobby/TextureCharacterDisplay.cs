using September.Common;
using UnityEngine;
using UnityEngine.UI;

namespace September.Lobby
{
    public class TextureCharacterDisplay : MonoBehaviour
    {
        [SerializeField] private Image _characterImage;

        public void SetCharacter(int index)
        {
            if (index < 0 || index >= CharacterDataContainer.DataCount)
            {
                Debug.LogWarning("指定されたインデックスは無効です: " + index);
                var color = _characterImage.color;
                color.a = 0;
                _characterImage.color = color;
                return;
            }

            var data = CharacterDataContainer.Instance.GetCharacterData(index);
            _characterImage.sprite = data.CharacterPortrait;
        }
    }
}
