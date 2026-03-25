namespace InGame.Player.Ult
{
    public interface IUltCondition
    {
        public bool IsAvailable();
        public void OnUltActivated();
    }
}