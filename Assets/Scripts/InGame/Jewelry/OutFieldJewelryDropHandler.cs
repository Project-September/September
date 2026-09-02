using DG.Tweening;
using InGame.Jewelry.Common;
using InGame.Player;
using September.Common.Attribute;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace September.InGame.Jewelry
{
    public class OutFieldJewelryDropHandler : MonoBehaviour
    {
        [SerializeField] private PlayerRespawn _playerRespawn;
        [SerializeField, RequireInterface(typeof(IJewelryContainer))] private Component _jewelryContainerObj;
        [SerializeField] private PlayerMovement _playerMovement;

        [SerializeField] private float _height = 1f;
        [SerializeField] private float _randomHeight = 0.3f;
        [SerializeField] private float _duration = 0.7f;
        [SerializeField] private float _randomDuration = 0.1f;
        [SerializeField] private AnimationCurve _ease;

        private IJewelryContainer _jewelryContainer;

        private Vector3 _prevGroundedPos;

        public void Start()
        {
            _jewelryContainer = _jewelryContainerObj as IJewelryContainer;
            _playerRespawn.OnOutFieldEvent += OnOutField;
        }

        private void Update()
        {
            if (_playerMovement.IsGroundNet)
            {
                _prevGroundedPos = transform.position;
            }
        }

        private void OnOutField()
        {
            int count = _jewelryContainer.GetJewelryCount(JewelryType.NormalGem) / 2;
            var dropped = new IJewelry[count];
            _jewelryContainer.DropJewelry(JewelryType.NormalGem, count, dropped);

            DebugDrawUtility.DrawWireSphere(_prevGroundedPos, 1f, Color.green, 5f);

            foreach (IJewelry drop in dropped)
            {
                if (drop is global::InGame.Jewelry.Jewelry jewelry)
                {
                    Vector3 pos = GetRandomPosition();
                    DebugDrawUtility.DrawWireSphere(pos, 1f, Color.red, 5f);
                    jewelry.JewelryControl.ThrowToNonPhysics(pos, (pos.y - transform.position.y) + _height + Random.Range(-.5f, .5f) * _randomHeight, _duration + Random.Range(-.5f, .5f) * _randomDuration, _ease);
                }
            }
        }

        private Vector3 GetRandomPosition()
        {
            var insideUnitCircle = Random.insideUnitCircle;
            var randomOffset = new Vector3(insideUnitCircle.x, 0, insideUnitCircle.y) * 2f;
            var target = _prevGroundedPos + randomOffset;
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return _prevGroundedPos;
        }
    }
}
