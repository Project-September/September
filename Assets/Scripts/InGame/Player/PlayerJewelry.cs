using Fusion;
using InGame.Health;
using September.InGame.Common.Stats;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerJewelry : NetworkBehaviour
    {
        [SerializeField] private PlayerStatus _status;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private NetworkObject _jewelryPrefab;
        [Header("Throw")]
        [SerializeField] private float _horizontalThrowForce = 5f;
        [SerializeField] private float _upwardThrowForce = 3f;
        [SerializeField] private float _heightOffset;

        private const string JewelryTag = "Jewelry";
        public void Start()
        {
            _status.SetBaseValue(StatType.Jewelry, 0);
            _health.OnDeath += OnDeath;
        }

        public void OnDeath(HitData lastHitData)
        {
            if (!HasStateAuthority) return;

            int removeAmount = Mathf.Max(0, _status.Jewelry - 1);
            RPC_RemoveJewelry(removeAmount);

            DropJewelry(removeAmount);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_GetJewelry(int addAmount)
        {
            _status.AddBaseValue(StatType.Jewelry, addAmount);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_RemoveJewelry(int removeAmount)
        {
            _status.AddBaseValue(StatType.Jewelry, -removeAmount);
        }

        private void DropJewelry(int removeAmount)
        {
            Vector3 spawnCenter = this.transform.position + Vector3.up * _heightOffset;

            for (int i = 0; i < removeAmount; i++)
            {
                Runner.Spawn(_jewelryPrefab, spawnCenter, Quaternion.identity, onBeforeSpawned: InitializeSpawnedJewelry);
            }
        }

        private void InitializeSpawnedJewelry(NetworkRunner runner, NetworkObject obj)
        {
            if (!obj.TryGetComponent(out JewelryControl jewelry))
                return;

            Vector3 dir = Random.insideUnitSphere;
            dir.y = 0f;
            dir.Normalize();

            Vector3 force = dir * _horizontalThrowForce + Vector3.up * _upwardThrowForce;
            jewelry.Throw(force);
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority) return;

            if (other.gameObject.CompareTag(JewelryTag)
                && other.TryGetComponent<NetworkObject>(out var networkObject))
            {
                Runner.Despawn(networkObject);
                RPC_GetJewelry(1);
            }
        }

        private void OnDestroy()
        {
            _health.OnDeath -= OnDeath;
        }
    }
}
