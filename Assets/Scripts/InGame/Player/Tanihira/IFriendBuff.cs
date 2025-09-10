namespace Ingame.Tanihira
{
    public interface IFriendBuff
    {
        public void StartBuff();
        public void StartBuff(float buffRate);
        public void StopBuff();
    }
}