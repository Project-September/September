using UnityEngine;

namespace September.InGame.Kraken.Attack
{
    public struct AttackPredictionShape
    {
        public Vector3 Position;
        public Vector3 HalfExtents;
        public Quaternion Rotation;

        public AttackPredictionShape(Vector3 position, Vector3 halfExtents, Quaternion rotation)
        {
            Position = position;
            HalfExtents = halfExtents;
            Rotation = rotation;
        }
    }

    public class AttackPrediction : MonoBehaviour
    {
        [SerializeField] private GameObject _predictionParticle;

        public void Show(AttackPredictionShape shape)
        {
            _predictionParticle.transform.position = shape.Position;
            _predictionParticle.transform.rotation = shape.Rotation;
            _predictionParticle.transform.localScale = shape.HalfExtents;
            _predictionParticle.SetActive(true);
        }

        public void Hide()
        {
            _predictionParticle.SetActive(false);
        }
    }
}
