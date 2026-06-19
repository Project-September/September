using Fusion;
using InGame.Health;
using September.Common;
using UnityEngine;

namespace InGame.Player.Okubo
{
    public class BombControl : NetworkBehaviour
    {
        [SerializeField] private float _waitDuration;
        [SerializeField] private float _range;
        [SerializeField] private int _damageAmount;
        [SerializeField] private float _flyingPower;
        [SerializeField] private Rigidbody _rb;

        private float _waitTimer;
        private PlayerRef _ownerRef;

        public override void Spawned()
        {
            _waitTimer = _waitDuration;
        }

        public void SetData(Vector3 force, PlayerRef ownerRef)
        {
            _ownerRef = ownerRef;
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
            var hitObjects = Physics.OverlapSphere(this.transform.position, _range);

            foreach (var obj in hitObjects)
            {
                GameObject hitObject = obj.transform.root.gameObject;
                if (!hitObject.CompareTag("Player")) continue;

                //ヒットしたオブジェクトからPrayerRefを取得
                foreach (var pair in PlayerDatabase.Instance.PlayerObjectDic)
                {
                    if (pair.Value.gameObject != hitObject || pair.Key == _ownerRef)
                        continue;

                    if (!pair.Value.TryGetComponent(out IDamageable damageable))
                        continue;

                    //ダメージ処理
                    var hitData = new HitData(HitActionType.Damage, _damageAmount, _ownerRef, damageable.OwnerPlayerRef);
                    damageable.TakeHit(ref hitData);

                    if (!pair.Value.TryGetComponent(out PlayerMovement movement))
                        continue;
                    movement.AddFlyingVelocity((movement.transform.position - this.transform.position) * _flyingPower);
                    break; ;
                }
            }
            Runner.Despawn(Object);
        }
    }
}