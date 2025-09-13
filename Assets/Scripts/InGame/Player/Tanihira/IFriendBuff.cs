namespace Ingame.Tanihira
{
    public interface IFriendBuff
    {
        public void SetMask(bool value);
        public void StartBuff(float buffRate);
        public void StopBuff();
    }
}