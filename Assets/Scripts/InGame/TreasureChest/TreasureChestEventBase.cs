using UnityEngine;

namespace InGame.TreasureChest
{
    [System.Serializable]
    public abstract class TreasureChestEventBase
    {
        [Header("Šm—¦")]
        [SerializeField] private float _probability = 1f;
        public float Probability  => _probability;

        public abstract void Trigger(TriggerContext context);
    }

    public struct TriggerContext
    {
        public readonly Vector3 CenterPosition;

        private TriggerContext(Vector3 centerPosition)
        {
            CenterPosition = centerPosition;
        }

        public static TriggerContext Create(Vector3 centerPosition)
        {
           return new TriggerContext(centerPosition);
        }
    }
}
