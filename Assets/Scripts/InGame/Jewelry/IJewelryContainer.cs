using System.Collections.Generic;

namespace InGame.Player
{
    public interface IJewelryContainer
    {
        public void PickUp(IJewelry jewelry);
        public IEnumerable<IJewelry> DropJewelry(int amount);
        public int GetJewelryCount();
    }
}
