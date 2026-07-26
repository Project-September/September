using System;
using UnityEngine;

namespace InGame.Jewelry.Common
{
    [CreateAssetMenu(fileName = "PlayerJewelryDefinition", menuName = "Jewelry/PlayerJewelryDefinition")]
    public class PlayerJewelryDefinition : ScriptableObject
    {
        [Header("どの種類の宝石を何個持っているか"), SerializeField] HoldJewelryInfo[] _holdingJewelryInfos;

        public HoldJewelryInfo[] HoldingJewelryInfos => _holdingJewelryInfos;
    }

    /// <summary>持っている宝石の数についてのクラス</summary>
    [Serializable]
    public class HoldJewelryInfo
    {
        [SerializeField] JewelryInfo _jewelryInfo;
        [SerializeField] int _jewelryCount;

        public JewelryInfo JewelryInfo => _jewelryInfo;
        public int JewelryCount => _jewelryCount;
    }
}
