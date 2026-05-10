using Fusion;
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

        public Kraken CreateKraken(Vector3 position, Quaternion rotation)
        {
            var kraken = _networkRunner.Spawn(_krakenPrefab, position, rotation);
            
            return kraken;
        }
    }
}