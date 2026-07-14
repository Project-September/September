using Fusion;
using September.InGame.Jewelry;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerJewelryContainer : NetworkBehaviour, IJewelryContainer
    {
        [SerializeField] private NetworkObject _jewelryPrefab;
        [Header("Throw")]
        [SerializeField] private float _horizontalThrowForce = 5f;
        [SerializeField] private float _upwardThrowForce = 3f;
        [SerializeField] private float _heightOffset;

        private const string JewelryTag = "Jewelry";

        [Networked] public int JewelryCount { get; private set; }

        /// <summary>
        /// 触れた宝石を拾う処理
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority) return;

            if (other.gameObject.CompareTag(JewelryTag)
                && other.TryGetComponent<IJewelry>(out var jewelry))
            {
                PickUp(jewelry);
            }
        }

        public int DropJewelry(int dropAmount, IJewelry[] resultDropped)
        {
            Vector3 spawnCenter = transform.position + Vector3.up * _heightOffset;

            // 現在の所持数より多くの宝石はドロップしない
            dropAmount = Mathf.Min(dropAmount, JewelryCount);
            JewelryCount -= dropAmount;

            int result = 0;
            for (int i = 0; i < dropAmount; i++)
            {
                NetworkObject jewelryObj = Runner.Spawn(_jewelryPrefab, spawnCenter, Quaternion.identity, onBeforeSpawned: InitializeSpawnedJewelry);

                if (resultDropped == null) continue;

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

                jewelry.RandomThrow(_horizontalThrowForce, _upwardThrowForce);
            }
        }

        public void PickUp(IJewelry jewelry)
        {
            JewelryCount++;

            if (!HasStateAuthority) return;

            if (jewelry is Jewelry jewelComponent)
            {
                if (jewelComponent.TryGetComponent<NetworkObject>(out var jewelObj))
                {
                    Runner.Despawn(jewelObj);
                }
                else
                {
                    Destroy(jewelComponent.gameObject);
                }
            }
        }
    }
}
