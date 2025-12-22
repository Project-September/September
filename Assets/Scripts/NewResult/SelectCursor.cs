using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace September.NewResult
{
    /// <summary>
    /// 選択メニューのカーソルをEventSystemの選択状態に合わせて操作するコンポーネント
    /// </summary>
    public class SelectCursor : MonoBehaviour
    {
        [SerializeField] private Image _cursor;
        [SerializeField] private Selectable[] _selectables;

        private void MoveCursor(GameObject selectable)
        {
            var p = selectable.transform.position;
            var c = _cursor.transform.position;
            c.y = p.y;
            _cursor.transform.position = c;
        }
        
        private void Start()
        {
            // カーソルの移動と可視性の切り替え
            Observable.EveryUpdate()
                .Select(_ => EventSystem.current.currentSelectedGameObject)
                .DistinctUntilChanged()
                .Subscribe(selected =>
                {
                    bool isMenuItem = selected != null && _selectables.Select(x => x.gameObject).Contains(selected);

                    if (isMenuItem)
                    {
                        MoveCursor(selected);
                        _cursor.enabled = true;
                    }
                    else
                    {
                        _cursor.enabled = false;
                    }
                })
                .AddTo(this);
        }
    }
}