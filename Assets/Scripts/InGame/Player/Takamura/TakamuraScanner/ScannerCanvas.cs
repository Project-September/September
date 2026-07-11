using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.Player
{
    public class ScannerCanvas : NetworkBehaviour
    {
        [SerializeField] Image _targetImage;

        /// <summary>
        /// 擬態対象を示すImageを展示物の上に重なるように移動するメソッド
        /// </summary>
        /// <param name="pos"></param>
        public void SetImagePosition(Vector3 pos)
        {
            _targetImage.transform.position = pos;
        }
    }
}
