using System.Collections.Generic;
using Fusion;
using September.InGame.Common.Stats;
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

        public void Start()
        {
            _status.SetBaseValue(StatType.Jewelry, 10);
        }

        public IEnumerable<IJewelry> DropJewelry(int removeAmount)
        {
            Vector3 spawnCenter = transform.position + Vector3.up * _heightOffset;
            _status.AddBaseValue(StatType.Jewelry, -removeAmount);

            for (int i = 0; i < removeAmount; i++)
            {
                NetworkObject jewelryObj = Runner.Spawn(_jewelryPrefab, spawnCenter, Quaternion.identity, onBeforeSpawned: InitializeSpawnedJewelry);
                yield return jewelryObj.GetComponent<IJewelry>();
            }

            yield break;

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
            _status.AddBaseValue(StatType.Jewelry, jewelry.Score);
        }

        public int GetJewelryCount()
        {
            return _status.Jewelry;
        }

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
    }
}
