using September.InGame.Jewelry.Drop.Strategies.Chances;

namespace September.InGame.Jewelry.Drop.Strategies.Amounts
{
    public interface IJewelryDropAmount
    {
        public int GetDropAmount(ref JewelryDropContext context);
    }
}
