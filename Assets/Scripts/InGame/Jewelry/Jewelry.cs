using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Jewelry.Common;
using September.InGame.Jewelry;
using UnityEngine;

namespace InGame.Jewelry
{
    public class Jewelry : NetworkBehaviour, IJewelry
    {
        [Header("この宝石のパラメータ群"), SerializeField] JewelryInfo _jewelryParams;
        [SerializeField] JewelryControl _jewelryControl;
        [SerializeField] ParticleSystemRenderer[] _renderers;

        public JewelryInfo JewelryParams => _jewelryParams;
        public JewelryControl JewelryControl => _jewelryControl;

        private TickTimer _despawnTimer;

        public override void Spawned()
        {
            _despawnTimer = TickTimer.CreateFromSeconds(Runner, _jewelryParams.LifeTime);
        }

        public override void FixedUpdateNetwork()
        {
            if (_despawnTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                DespawnedJewelryRepository.AddDespawnedJewelry(_jewelryParams.JewelryType);
            }
        }

        public override void Render()
        {
            if (_despawnTimer.RemainingTime(Runner) <= _jewelryParams.BlinkStartRemainingTime)
            {
                bool blink = Runner.SimulationTime * _jewelryParams.BlinkSpeed % 1f > 0.5f;

                foreach (ParticleSystemRenderer r in _renderers)
                {
                    r.enabled = blink;
                }
            }
        }

        public async UniTask PickupFrom(PlayerRef player)
        {
            var obj = Runner.GetPlayerObject(player);
            var container = obj.GetComponentInChildren<IJewelryContainer>();

            if (container == null) return;

            await _jewelryControl.PlayGetMove(obj.transform);

            container.PickUp(this);
        }
    }
}
