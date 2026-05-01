using Fusion;
using UnityEngine;

namespace September.InGame.Kraken
{
    public class KrakenEventHandler : NetworkBehaviour
    {
        [SerializeField] private KrakenFactory _krakenFactory;
        [SerializeField] private Transform _krakenSpawnPoint;
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_PlayerChangeKraken(PlayerRef target)
        {
            var playerObject = Runner.GetPlayerObject(target);

            if (playerObject == null)
            {
                Debug.LogError("PlayerObject is null");
                return;
            }
            
            var kraken = _krakenFactory.CreateKraken(target, _krakenSpawnPoint.position, _krakenSpawnPoint.rotation,
                playerObject.transform.position, playerObject.transform.rotation);
        }

        [ContextMenu("Test")]
        public void Test()
        {
            RPC_PlayerChangeKraken(Runner.LocalPlayer);
        }
    }
}