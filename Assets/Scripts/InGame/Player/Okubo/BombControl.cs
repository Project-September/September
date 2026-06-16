using Fusion;
using UnityEngine;

namespace InGame.Player.Okubo
{
    public class BombControl : NetworkBehaviour
    {
        [SerializeField] private float _waitDuration;
        [SerializeField] private float _range;
        [SerializeField] private int _damageAmount;
        [SerializeField] private Rigidbody _rb;

        private float _waitTimer;

        public override void Spawned()
        {
            _waitTimer = _waitDuration;
        }

        public void AddForce(Vector3 direction, float amount, float upAmount)
        {
            Vector3 force = direction * amount + Vector3.up * upAmount;
            //_rb.AddForce(force, ForceMode.Impulse);
            _rb.linearVelocity = force;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (_waitTimer > 0f)
            {
                _waitTimer -= Runner.DeltaTime;
                return;
            }

            Explode();
        }

        private void Explode()
        {
            Runner.Despawn(Object);
        }
    }
}