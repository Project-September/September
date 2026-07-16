using TMPro;
using UnityEngine;

namespace September
{
    public class JewelryView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _jewelryCountText;

        /// <summary>
        /// 宝石の個数表示を更新するメソッド
        /// </summary>
        /// <param name="count">宝石の数</param>
        public void UpdateJewelryCount(int count)
        {
            _jewelryCountText.text = "× " + count.ToString();
        }

        int count = -1;
        [ContextMenu("a")]
        void Test()
        {
            count++;
            UpdateJewelryCount(count);
        }
    }
}
