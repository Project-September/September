namespace InGame.Player.Ult
{
    public interface IUltCondition
    {
        /// <summary>
        /// 必殺技の発動条件を満たしているか
        /// </summary>
        public bool IsAvailable();
        
        /// <summary>
        /// 必殺技が発動された後の処理
        /// </summary>
        public void OnUltActivated();
    }
}