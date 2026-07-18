using UnityEngine;

namespace InGame.Jewelry.Common
{
    /// <summary>宝石のパラメータを定義したクラス</summary>
    [CreateAssetMenu(fileName = "JewelryParams", menuName = "Jewelry/JewelryParams")]
    public class JewelryParams : ScriptableObject
    {
        [Header("宝石の種類"), SerializeField] JewelryType _jewelryType;
        [Header("獲得した時のスコア"), SerializeField] int _score = 1;

        public JewelryType JewelryType => _jewelryType;
        public int Score => _score;
    }
}
