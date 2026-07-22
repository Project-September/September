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

        private Vector3 _velocity;
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
            _velocity = velocity;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _isGrounded)
                return;

            _velocity += Vector3.down * _gravity * Runner.DeltaTime;

            Vector3 nextPos = this.transform.position + _velocity * Runner.DeltaTime;

            if (IsGrounded(out var hitPos))
            {
                _collider.enabled = true;
                nextPos.y = hitPos.y + _collider.bounds.extents.y;
                _velocity = Vector3.zero;
                _isGrounded = true;
            }

            this.transform.position = nextPos;
        }

        private bool IsGrounded(out Vector3 hitPos)
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;

            if (Physics.Raycast(origin, Vector3.down, out var hit, _groundCheckDistance, _groundLayer))
            {
                hitPos = hit.point;
                return true;
            }
            hitPos = Vector3.zero;
            return false;
        }
    }
}
