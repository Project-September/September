using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace September.Common
{
    /// <summary>
    /// 外部制御用のスライダーコンポーネント。
    /// 単体では値を動かす能力を持たない。
    /// </summary>
    public class ManualSlider : Slider
    {
        public float stepSize = 0.1f;
        
        /// <summary>
        /// ユーザー入力の処理。
        /// ナビゲーションのみを処理し、自らスライダーを動かさない。
        /// </summary>
        public override void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Right:
                    Navigate(eventData, FindSelectableOnRight());
                    break;

                case MoveDirection.Up:
                    Navigate(eventData, FindSelectableOnUp());
                    break;

                case MoveDirection.Left:
                    Navigate(eventData, FindSelectableOnLeft());
                    break;

                case MoveDirection.Down:
                    Navigate(eventData, FindSelectableOnDown());
                    break;
            }
        }
        
        protected override void Set(float input, bool sendCallback = true)
        {
            input = Mathf.Round(input / stepSize) * stepSize;
            base.Set(input, sendCallback);
        }
        
        private static void Navigate(AxisEventData eventData, Selectable sel)
        {
            if (sel != null && sel.IsActive())
                eventData.selectedObject = sel.gameObject;
        }
    }
}