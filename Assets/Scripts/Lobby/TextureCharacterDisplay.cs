using September.Common;
using UnityEngine;
using UnityEngine.UI;

namespace September.Lobby
{
    public class TextureCharacterDisplay : MonoBehaviour
    {
        [SerializeField] private Image _characterImage;
        [SerializeField] private Sprite[] _characterImages;

        public void SetCharacter(int index)
        {
            if (index < 0 || index >= _characterImages.Length)
            {
                Debug.LogWarning("指定されたインデックスは無効です: " + index);
                var color = _characterImage.color;
                color.a = 0;
                _characterImage.color = color;
                return;
            }

            for (int i = 0; i < _characterImages.Length; i++)
            {
                _characterImage.sprite = _characterImages[index];
            }
        }
    }
}