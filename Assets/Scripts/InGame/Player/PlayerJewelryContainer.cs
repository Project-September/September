using Fusion;
using InGame.Jewelry.Common;
using System;
using UnityEngine;

namespace InGame.Jewelry
{
    public class PlayerJewelryContainer : NetworkBehaviour, IJewelryContainer
    {
        [SerializeField] private NetworkObject _jewelryPrefab;
        [Header("Throw")]
        [SerializeField] private float _horizontalThrowForce = 5f;
        [SerializeField] private float _upwardThrowForce = 3f;
        [SerializeField] private float _heightOffset;

        event Action<JewelryType> _onGetJewelry;
        event Action<JewelryType> _onDropJewelry;
        public Action OnGetJewelry(Action<JewelryType> act)
        {
            _onGetJewelry += act;
            return () => _onGetJewelry -= act;
        }
        public Action OnDropJewelry(Action<JewelryType> act)
        {
            _onDropJewelry += act;
            return () => _onDropJewelry -= act;
        }

        private const string JewelryTag = "Jewelry";

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

            int result = 0;
            for (int i = 0; i < dropAmount; i++)
            {
                NetworkObject jewelryObj = Runner.Spawn(_jewelryPrefab, spawnCenter, Quaternion.identity, onBeforeSpawned: InitializeSpawnedJewelry);

                if (!jewelryObj.TryGetComponent<IJewelry>(out var jewelry)) continue;
                _onDropJewelry?.Invoke(jewelry.JewelryParams.JewelryType);

                if (resultDropped == null) continue;

                if (resultDropped.Length > i)
                {
                    resultDropped[i] = jewelry;
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
            // 宝石を取得したことを知らせる
            _onGetJewelry(jewelry.JewelryParams.JewelryType);

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
