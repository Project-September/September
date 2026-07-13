using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace InGame
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
            Initialize();
        }
        public async void Initialize()
        {
            _collider.enabled = false;
            await UniTask.WaitForSeconds(_getCoolTime);
            _collider.enabled = true;
        }
        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _isGrounded)
                return;

            _velocity += Vector3.down * _gravity * Runner.DeltaTime;

            Vector3 nextPos = this.transform.position + _velocity * Runner.DeltaTime;
            Ray ray = new Ray(nextPos + Vector3.up * 0.1f, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, _groundCheckDistance, _groundLayer))
            {
                //nextPos.y = hit.point.y + (_collider.bounds.size.y);
                _velocity = Vector3.zero;
                _isGrounded = true;
            }

            this.transform.position = nextPos;
        }
        public void Throw(Vector3 velocity)
        {
            _velocity = velocity;
        }
    }
}
