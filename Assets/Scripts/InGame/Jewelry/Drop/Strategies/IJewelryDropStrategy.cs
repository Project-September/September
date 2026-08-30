using InGame.Health;
using InGame.Jewelry.Common;

namespace September.InGame.Health
{
    public interface IJewelryDropStrategy
    {
        public int GetDropAmount(HitData hitData, JewelryType jewelryType, IJewelryContainer jewelryContainer);
    }
}