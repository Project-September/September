using CRISound;
using Fusion;
using InGame.Health;
using September.Common;
using September.InGame.Effect;
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
        [SerializeField] private EffectType _explosion;
        [SerializeField] private LayerMask _groundLayer = ~0;
        [SerializeField] private GameObject _countDownEffect;
        [SerializeField] private string _explodeSoundName;

        private float _explodeTime;
        private PlayerRef _ownerRef;
        private EffectSpawner _effectSpawner;

        public override void Spawned()
        {
            _explodeTime = Runner.SimulationTime + _waitDuration;
            _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
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

            if (Runner.SimulationTime < _explodeTime)
            {
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

                //�q�b�g�����I�u�W�F�N�g����PrayerRef���擾
                foreach (var pair in PlayerDatabase.Instance.PlayerObjectDic)
                {
                    if (pair.Value.gameObject != hitObject || pair.Key == _ownerRef)
                        continue;

                    if (!pair.Value.TryGetComponent(out IDamageable damageable))
                        continue;

                    //�_���[�W����
                    var hitData = new HitData(HitActionType.Damage, _damageAmount, _ownerRef, damageable.OwnerPlayerRef);
                    damageable.TakeHit(ref hitData);

                    if (!pair.Value.TryGetComponent(out PlayerMovement movement))
                        continue;

                    //������΂�����
                    var dir = movement.transform.position - transform.position;
                    var distance = dir.magnitude;

                    var power = _flyingPower / Mathf.Max(distance, 0.1f);

                    movement.AddFlyingVelocity(dir.normalized * power);

                    break;

                }
            }
            _effectSpawner.RequestPlayOneShotEffect(_explosion, this.transform.position, Quaternion.identity);
            CRIAudio.PlaySE(transform.position, "ALLCue", _explodeSoundName);

            Runner.Despawn(Object);

        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsInLayerMask(collision.gameObject, _groundLayer))
            {
                _countDownEffect?.gameObject.SetActive(true);
            }
        }

        private bool IsInLayerMask(GameObject target, LayerMask layerMask)
        {
            return (layerMask.value & (1 << target.layer)) != 0;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _range);
        }
    }
}
