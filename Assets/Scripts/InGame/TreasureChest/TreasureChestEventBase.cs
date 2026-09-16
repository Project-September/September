using Fusion;
using UnityEngine;

namespace InGame.TreasureChest
{
    [System.Serializable]
    public abstract class TreasureChestEventBase
    {
        [Header("Šm—¦")]
        [SerializeField] private float _probability = 1f;
        public float Probability => _probability;

        public abstract void Trigger(TriggerContext context);
    }

    public struct TriggerContext
    {
        public readonly Vector3 CenterPosition;
        public readonly NetworkObject TreasureChestObject;
        public readonly NetworkRunner Runner;

        private TriggerContext(Vector3 centerPosition, NetworkObject treasureChest, NetworkRunner runner)
        {
            CenterPosition = centerPosition;
            TreasureChestObject = treasureChest;
            Runner = runner;
        }

        public static TriggerContext Create(Vector3 centerPosition, NetworkObject treasureChest, NetworkRunner runner)
        {
            return new TriggerContext(centerPosition, treasureChest, runner);
        }
    }
}
