using UnityEngine;

namespace InGame.Player
{
    [CreateAssetMenu(fileName = "EvasionData", menuName = "Scriptable Objects/EvasionData")]
    public class EvasionData : ScriptableObject
    {
        [SerializeField] private float _rollDistance = 4f;
        [SerializeField] private float _rollDuration = 0.95f;
        [SerializeField,Range(0,360)] private float _inputAngle = 0f;
        [SerializeField] private float _invincibleTime = 0.25f;
        [SerializeField] private float _cooldown = 0;
        [SerializeField] private float _weightDecay = 0.1f;
        [SerializeField] private AnimationCurve _rollSpeed;

        public float RollDistance => _rollDistance;
        public float RollDuration => _rollDuration;
        public float InputAngle => _inputAngle;
        public float InvincibleTime => _invincibleTime;
        public float Cooldown => _cooldown;
        public float WeightDecay => _weightDecay;
        public AnimationCurve RollSpeed => _rollSpeed;
    }
}
