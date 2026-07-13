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
        public void Start()
        {
            _status.SetBaseValue(StatType.Jewelry, 0);
            _health.OnDeath += OnDeath;
        }

        public void Update()
        {
            Debug.LogWarning($"JewelryCount {_status.Jewelry}");
        }

        public void OnDeath(HitData lastHitData)
        {
            if (!HasStateAuthority) return;

            int removeAmount = Mathf.Max(0, _status.Jewelry - 1);
            RPC_RemoveJewelry(removeAmount);

            Vector3 spawnCenter = this.transform.position + Vector3.up * _heightOffset;

            for (int i = 0; i < removeAmount; i++)
            {
                Runner.Spawn(_jewelryPrefab, spawnCenter, Quaternion.identity, onBeforeSpawned: ThrowSpawnedJewelry);
            }
        }

        public void ThrowSpawnedJewelry(NetworkRunner runner, NetworkObject obj)
        {
            if (!obj.TryGetComponent(out JewelryControl jewelry))
                return;

            Vector3 dir = Random.insideUnitSphere;
            dir.y = 0f;
            dir.Normalize();

            Vector3 force = dir * _horizontalThrowForce + Vector3.up * _upwardThrowForce;
            jewelry.Throw(force);
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
        public void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority) return;

            if (other.gameObject.CompareTag("Jewelry")
                && other.TryGetComponent<NetworkObject>(out var networkObject))
            {
                Runner.Despawn(networkObject);
                RPC_GetJewelry(1);
            }
        }
    }
}
