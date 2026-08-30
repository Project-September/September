using Cysharp.Threading.Tasks;
using DG.Tweening;
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

        private bool _physicsEnabled = true;

        [Header("Get Move")]
        [SerializeField] private Vector3 _offset = new(0f, 1.5f, 0f);
        [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _heightOffsetCurve;
        [SerializeField] private float _heightOffsetScale = 1f;

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

        public async UniTask PlayGetMove(Transform target)
        {
            const float duration = 1f;

            _physicsEnabled = false;
            _collider.enabled = false;
            _rigidbody.isKinematic = true;

            Vector3 startPos = transform.position;

            await DOTween.To(() => 0f, t =>
            {
                Vector3 heightOffset = Vector3.up * (_heightOffsetCurve.Evaluate(t) * _heightOffsetScale);

                float t0 = _curve.Evaluate(t);
                Vector3 targetPos = target.position + target.rotation * _offset;

                transform.position = Vector3.LerpUnclamped(startPos, targetPos, t0) + heightOffset;
            }, 1f, duration).SetEase(Ease.Linear);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _isGrounded || !_physicsEnabled)
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

            if (Physics.Raycast(origin, Vector3.down, out _, _groundCheckDistance + .1f, _groundLayer))
            {
                return true;
            }
            return false;
        }
    }
}
