using System.Collections.Generic;
using InGame.Jewelry.Common;

namespace September.InGame.Jewelry
{
    public static class DespawnedJewelryRepository
    {
        private static readonly Dictionary<JewelryType, int> DespawnedJewelryCount = new();

        public static void AddDespawnedJewelry(JewelryType jewelryType)
        {
            if (DespawnedJewelryCount.ContainsKey(jewelryType)) DespawnedJewelryCount[jewelryType]++;
        }

        public static int GetDespawnedJewelryCount(JewelryType jewelryType)
        {
            return DespawnedJewelryCount.GetValueOrDefault(jewelryType, 0);
        }
    }
}
