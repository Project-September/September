using UnityEngine;

namespace InGame.Jewelry.Common
{
    /// <summary>宝石の定義を持つクラス</summary>
    [CreateAssetMenu(fileName = "JewelryInfo", menuName = "Jewelry/JewelryInfo")]
    public class JewelryInfo : ScriptableObject
    {
        [Header("宝石の種類"), SerializeField] JewelryType _jewelryType;
        [Header("獲得した時のスコア"), SerializeField] int _score = 1;
        [Header("UIとして表示するときのアイコン"), SerializeField] Sprite _jewelrySprite;

        public JewelryType JewelryType => _jewelryType;
        public int Score => _score;
        public Sprite JewelrySprite => _jewelrySprite;
    }
}
