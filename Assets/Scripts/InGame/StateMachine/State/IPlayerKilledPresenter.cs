using Fusion;

namespace September.Common
{
    public interface IPlayerKilledPresenter
    {
        void OnPlayerKilled(PlayerRef killer, PlayerRef victim);
    }
}