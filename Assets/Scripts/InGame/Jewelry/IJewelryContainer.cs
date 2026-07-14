namespace InGame.Player
{
    public interface IJewelryContainer
    {
        public void PickUp(IJewelry jewelry);
        public int DropJewelry(int removeAmount, IJewelry[] resultDropped = null);
        public int GetJewelryCount();
    }
}
