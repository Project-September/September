using TMPro;
using UnityEngine;

namespace September.Lobby
{
    public class BuildSelectObject : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _buildName;
        [SerializeField] TextMeshProUGUI _buildInfo;
        [SerializeField] CanvasGroup _canvasGroup;
        bool _initialized;

        public void Init(string name, string info)
        {
            _initialized = _buildName && _buildInfo;
            if (_initialized)
            {
                _buildName.text = name;
                _buildInfo.text = info;
            }
        }

        /// <summary>
        /// 選択時のメソッド
        /// </summary>
        public void Select()
        {
            if (!_initialized) return;
            if (_canvasGroup) _canvasGroup.alpha = 1;
        }

        /// <summary>
        /// 未選択時のメソッド
        /// </summary>
        public void Unselect()
        {
            if (!_initialized) return;
            if (_canvasGroup) _canvasGroup.alpha = 0;
        }
    }
}
