using Fusion;
using InGame.Jewelry.Common;
using UnityEngine;

namespace InGame.Jewelry
{
    public class Jewelry : NetworkBehaviour, IJewelry
    {
        [Header("この宝石のパラメータ群"), SerializeField] JewelryInfo _jewelryParams;

        public JewelryInfo JewelryParams => _jewelryParams;
    }
}
