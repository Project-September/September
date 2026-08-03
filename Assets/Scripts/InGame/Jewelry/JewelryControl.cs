using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace InGame.Jewelry
{
    public class JewelryControl : NetworkBehaviour
    {
        [SerializeField] private float _gravity = 20f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private float _groundCheckDistance = 0.2f;
        [SerializeField] private float _getCoolTime = 3f;
        [SerializeField] private Collider _collider;
        [SerializeField] private Rigidbody _rigidbody;

        private Vector3 Velocity { get => _rigidbody.linearVelocity; set => _rigidbody.linearVelocity = value; }
        private bool _isGrounded;

        public void Start()
        {
            Initialize().Forget();
        }

        public async UniTask Initialize()
        {
            _collider.enabled = false;
            await UniTask.WaitForSeconds(_getCoolTime, cancellationToken: destroyCancellationToken);
            _collider.enabled = true;
        }

        public void RandomThrow(float horizontalThrowForce, float upwardThrowForce)
        {
            Vector3 dir = Random.insideUnitSphere;
            dir.y = 0f;
            dir.Normalize();

            Vector3 force = dir * horizontalThrowForce + Vector3.up * upwardThrowForce;
            Throw(force);
        }

        public void Throw(Vector3 velocity)
        {
            Velocity = velocity;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _isGrounded)
                return;

            Velocity += Vector3.down * _gravity * Runner.DeltaTime;

            if (IsGrounded())
            {
                _collider.enabled = true;
                _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                _isGrounded = true;
            }
        }

        private bool IsGrounded()
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;

            if (Physics.Raycast(origin, Vector3.down, out var _, _groundCheckDistance + .1f, _groundLayer))
            {
                return true;
            }
            return false;
        }
    }
}
