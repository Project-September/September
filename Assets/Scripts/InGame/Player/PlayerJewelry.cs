using Fusion;
using September.InGame.Common.Stats;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerJewelry : NetworkBehaviour
    {
        [SerializeField] private PlayerStatus _status;
        public void Start()
        {
            _status.SetBaseValue(StatType.Jewelry, 0);
        }

        public void Update()
        {
            Debug.Log($"JewelryCount {_status.Jewelry}");
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_GetJewelry(int addAmount)
        {
            _status.AddBaseValue(StatType.Jewelry, addAmount);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_RemoveJewelry(int removeAmount)
        {
            _status.AddBaseValue(StatType.Jewelry, removeAmount);
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
