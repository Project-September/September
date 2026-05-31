using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace September.Lobby
{
    public class BuildSelectObject : MonoBehaviour
    {
        [Header("アイコンなどの画像表示")]
        [SerializeField] Button _icon;
        [SerializeField] Image _selectedIcon;
        [SerializeField] Image _arrow;

        public void Init()
        {
            _selectedIcon.enabled = false;
        }

        /// <summary>
        /// ボタンにアクション登録をするメソッド
        /// </summary>
        /// <param name="act">登録するアクション</param>
        public void RegisterAction(UnityAction act)
        {
            _icon.onClick.AddListener(act);
        }

        /// <summary>
        /// 選択時のメソッド
        /// </summary>
        public void Select()
        {
            if (_arrow) _arrow.color = Color.white;
        }

        /// <summary>
        /// 未選択時のメソッド
        /// </summary>
        public void Unselect()
        {
            if (_arrow) _arrow.color = Color.clear;
        }

        /// <summary>
        /// 決定時の描画処理
        /// </summary>
        public void Decision()
        {
            _selectedIcon.enabled = true;
        }

        /// <summary>
        /// 未決定時の描画処理
        /// </summary>
        public void Cancel()
        {
            _selectedIcon.enabled = false;
        }
    }
}
