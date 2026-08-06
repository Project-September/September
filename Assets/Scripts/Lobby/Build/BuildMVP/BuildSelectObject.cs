using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace September.Lobby
{
    public class BuildSelectObject : MonoBehaviour
    {
        [Header("アイコンなどの画像表示")]
        [SerializeField] Button _icon;
        [SerializeField] Image _iconImage;
        [SerializeField] Image _selectedframe;
        [SerializeField] Image _check;

        public void Init()
        {
            _selectedframe.enabled = false;
            _check.enabled = false;
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
        /// アイコンを設定するメソッド
        /// </summary>
        /// <param name="sprite">アイコン画像</param>
        public void SetIconImage(Sprite sprite)
        {
            _iconImage.sprite = sprite;
        }

        /// <summary>
        /// 選択時のメソッド
        /// </summary>
        public void Select()
        {
            EventSystem.current.SetSelectedGameObject(_icon.gameObject);
            _check.enabled = true;
        }

        /// <summary>
        /// 未選択時のメソッド
        /// </summary>
        public void Unselect()
        {
            _check.enabled = false;
        }

        /// <summary>
        /// 決定時の描画処理
        /// </summary>
        public void Decision()
        {
            _selectedframe.enabled = true;
        }

        /// <summary>
        /// 未決定時の描画処理
        /// </summary>
        public void Cancel()
        {
            _selectedframe.enabled = false;
        }
    }
}
