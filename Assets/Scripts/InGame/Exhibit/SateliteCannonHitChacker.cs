using Fusion;
using InGame.Health;
using UnityEngine;

namespace Ingame.Exhibit
{
    public class SateliteCannonHitChacker : MonoBehaviour
    {
        [SerializeField] private HitChecker _hitChecker;
        [SerializeField] private int _hitDamage = 999;
        [SerializeField] private Transform _underHitPos;

        private PlayerRef _ownerPlayer;
        
        private void Start()
        {
            //ヒット処理
            _hitChecker.OnHit -= OnHitCannon;
            _hitChecker.OnHit += OnHitCannon;
        }

        private void OnHitCannon(Collider hitInfo)
        {
            var damageable = hitInfo.GetComponentInParent<IDamageable>();
            if (damageable == null || hitInfo.transform.position.y < _underHitPos.position.y) return;
            var hitData = new HitData(
                HitActionType.Damage,
                _hitDamage,
                _ownerPlayer,
                damageable.OwnerPlayerRef);
            damageable.TakeHit(ref hitData);
            
            Debug.Log(_ownerPlayer);
        }

        public void SetOwnerPlayer(PlayerRef playerRef)
        {
            _ownerPlayer = playerRef; 
        }
    }
}