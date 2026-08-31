using InGame.Health;
using InGame.Jewelry.Common;

namespace September.InGame.Jewelry.Drop.Strategies
{
    public interface IJewelryDropStrategy
    {
        public int GetDropAmount(HitData hitData, JewelryType jewelryType, IJewelryContainer jewelryContainer, ref DropInfo info);
    }
}
