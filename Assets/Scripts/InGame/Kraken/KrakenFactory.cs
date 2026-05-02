using Fusion;
using InGame.Player;
using UnityEngine;

namespace September.InGame.Kraken
{
    public class KrakenFactory : NetworkBehaviour
    {
        [SerializeField] private Kraken _krakenPrefab;

        private NetworkRunner _networkRunner;

        private void Awake()
        {
            _networkRunner = FindFirstObjectByType<NetworkRunner>();
            if (_networkRunner == null)
            {
                Debug.LogError("NetworkRunnerがありません");
            }
        }

        public Kraken CreateKraken(PlayerRef owner, Vector3 position, Quaternion rotation, Vector3 finishPosition, Quaternion finishRotation)
        {
            var kraken = _networkRunner.Spawn(_krakenPrefab, position, rotation, owner);
            
            kraken.GetOn(owner);
            
            return kraken;
        }
    }
}