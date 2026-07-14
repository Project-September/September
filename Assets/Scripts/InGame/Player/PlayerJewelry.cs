using Fusion;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerJewelry : NetworkBehaviour, IJewelryContainer
    {
        [SerializeField] private PlayerStatus _status;
        [SerializeField] private NetworkObject _jewelryPrefab;
        [Header("Throw")]
        [SerializeField] private float _horizontalThrowForce = 5f;
        [SerializeField] private float _upwardThrowForce = 3f;
        [SerializeField] private float _heightOffset;

        private const string JewelryTag = "Jewelry";

        [Networked] public int JewelryCount { get; private set; }

        private void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority) return;

            if (other.gameObject.CompareTag(JewelryTag)
                && other.TryGetComponent<NetworkObject>(out var networkObject)
                && other.TryGetComponent<IJewelry>(out var jewelry))
            {
                Runner.Despawn(networkObject);
                PickUp(jewelry);
            }
        }

        public int DropJewelry(int removeAmount, IJewelry[] resultDropped)
        {
            Vector3 spawnCenter = transform.position + Vector3.up * _heightOffset;
            JewelryCount -= removeAmount;

            int result = 0;
            for (int i = 0; i < removeAmount; i++)
            {
                NetworkObject jewelryObj = Runner.Spawn(_jewelryPrefab, spawnCenter, Quaternion.identity, onBeforeSpawned: InitializeSpawnedJewelry);

                if (resultDropped.Length > i)
                {
                    resultDropped[i] = jewelryObj.GetComponent<IJewelry>();
                    result = i;
                }
                else
                {
                    Debug.LogWarning("result buffer size is too small");
                }
            }

            return result;

            void InitializeSpawnedJewelry(NetworkRunner runner, NetworkObject obj)
            {
                if (!obj.TryGetComponent(out JewelryControl jewelry))
                    return;

                Vector3 dir = Random.insideUnitSphere;
                dir.y = 0f;
                dir.Normalize();

                Vector3 force = dir * _horizontalThrowForce + Vector3.up * _upwardThrowForce;
                jewelry.Throw(force);
            }
        }

        public void PickUp(IJewelry jewelry)
        {
            JewelryCount++;
        }

        public int GetJewelryCount()
        {
            return JewelryCount;
        }
    }
}
