using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace InGame.TreasureChest
{
    public class TreasureChestEventController : NetworkBehaviour
    {
        [SerializeReference, SubclassSelector] private List<TreasureChestEventBase> _events;
        [SerializeField] private float _intervalSecond;

        private float _nextTime;

        public override void Spawned()
        {
            _nextTime =  Runner.SimulationTime + _intervalSecond;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            if(Runner.SimulationTime >= _nextTime)
            {
                _nextTime = Runner.SimulationTime + _intervalSecond;
                TriggerRandomEvent();
            }
        }

        private void TriggerRandomEvent()
        {
            var randomEvent = GetRandomEvent();
            var context = TriggerContext.Create(this.transform.position);
            randomEvent?.Trigger(context);
        }

        private TreasureChestEventBase GetRandomEvent()
        {
            float totalWeight = 0;

            foreach (TreasureChestEventBase eventData in _events)
            {
                totalWeight += eventData.Probability;
            }

            float randomValue = Random.Range(0, totalWeight);

            foreach (TreasureChestEventBase eventData in _events)
            {
                randomValue -= eventData.Probability;

                if (randomValue < 0)
                {
                    return eventData;
                }
            }

            Debug.LogError("ƒGƒ‰[•¶‚ð‘‚¢‚Ä");
            return null;
        }
    }
}
