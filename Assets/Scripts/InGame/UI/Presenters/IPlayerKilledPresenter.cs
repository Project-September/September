using Fusion;

namespace September.InGame.UI.Presenters
{
    public interface IPlayerKilledPresenter
    {
        void OnPlayerKilled(PlayerRef killer, PlayerRef victim);
    }
}
