using System;
using UnityEngine;

namespace InGame.Jewelry.Common
{
    [CreateAssetMenu(fileName = "PlayerJewelryModel", menuName = "Jewelry/PlayerJewelryModel")]
    public class PlayerJewelryModel : ScriptableObject
    {
        [Header("どの種類の宝石を何個持っているか"), SerializeField] HoldingJewelryInfo[] _holdingJewelryInfos;

        public HoldingJewelryInfo[] HoldingJewelryInfos => _holdingJewelryInfos;
    }

    /// <summary>持っている宝石の数についての構造体</summary>
    [Serializable]
    public class HoldingJewelryInfo
    {
        [SerializeField] JewelryType _jewelryType;
        [SerializeField] int _jewelryCount;
        [SerializeField, Tooltip("UIとして表示するときのアイコン")] Sprite _jewelrySprite;

        public JewelryType JewelryType => _jewelryType;
        public int JewelryCount => _jewelryCount;
        public Sprite JewelrySprite => _jewelrySprite;
    }
}
