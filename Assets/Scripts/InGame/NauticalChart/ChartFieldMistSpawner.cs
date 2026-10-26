using System;
using System.Collections.Generic;
using UnityEngine;

namespace September.InGame.NauticalChart
{
    /// <summary>
    /// 海図インタラクト時のフィールド霧を、ワールド座標の範囲へ格子配置するクラス
    /// </summary>
    public class ChartFieldMistSpawner : MonoBehaviour
    {
        [Header("Prefab参照")]
        [SerializeField] private ParticleSystem _mistPrefab;

        [Serializable]
        private struct FieldMistVolume
        {
            [Tooltip("展示物の位置とは独立した、マップ上の中心")]
            public Vector3 Center;
            [Tooltip("XYZ のワールド基準サイズ（メートル）")]
            public Vector3 Size;

            /// <summary>
            /// ワールド範囲が有効か（X,Zのサイズが正の値か）
            /// </summary>
            public bool IsValidVolume => Size is { x: > 0f, z: > 0f };
        }

        [Header("発生範囲（ワールド座標）")]
        [SerializeField] private FieldMistVolume[] _volumes;

        [Header("配置")]
        [SerializeField] private int _gridCountX = 3;
        [SerializeField] private int _gridCountZ = 3;

        [Tooltip("霧インスタンス同士の最低距離")]
        [SerializeField] private float _minSpacing = 10f;
        [SerializeField] private float _yOffset;

        [Tooltip("高さのばらつき。立ち込め感用")]
        [SerializeField] private float _yRandomRange = 1f;

        private readonly List<ParticleSystem> _mistInstances = new();

        /// <summary>
        /// ワールド範囲へフィールド霧を格子配置する。位置はクライアントごとにランダムに配置する
        /// </summary>
        public void ShowFieldMist()
        {
            HideFieldMist();

            if (_mistPrefab == null || _gridCountX <= 0 || _gridCountZ <= 0 || !HasValidVolume())
            {
                Debug.LogWarning("[ChartFieldMistSpawner] Prefab / 範囲 / 分割数が未設定です", this);
                return;
            }

            foreach (var volume in _volumes)
            {
                if (!volume.IsValidVolume) continue;

                Bounds bounds = new(volume.Center, volume.Size);

                for (int x = 0; x < _gridCountX; x++)
                {
                    for (int z = 0; z < _gridCountZ; z++)
                    {
                        float posX = PickOnAxis(bounds.min.x, bounds.max.x, x, _gridCountX);
                        float posZ = PickOnAxis(bounds.min.z, bounds.max.z, z, _gridCountZ);
                        float posY = bounds.center.y + _yOffset + UnityEngine.Random.Range(-_yRandomRange, _yRandomRange);

                        // フィールド霧を配置
                        ParticleSystem mist = Instantiate(_mistPrefab, new Vector3(posX, posY, posZ), _mistPrefab.transform.rotation);
                        var main = mist.main;
                        main.stopAction = ParticleSystemStopAction.Destroy;
                        _mistInstances.Add(mist);
                    }
                }
            }

        }

        /// <summary>
        /// フィールド霧を非表示にする
        /// </summary>
        public void HideFieldMist()
        {
            foreach (ParticleSystem mist in _mistInstances)
            {
                if (mist == null) continue;

                mist.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            _mistInstances.Clear();
        }

        /// <summary>
        /// 1マスの内側から、重ならない範囲で座標を1つ返す
        /// </summary>
        private float PickOnAxis(float min, float max, int index, int count)
        {
            float cellMin = Mathf.Lerp(min, max, index / (float)count);
            float cellMax = Mathf.Lerp(min, max, (index + 1) / (float)count);

            float pad = _minSpacing * 0.5f;
            float usableMin = cellMin + pad;
            float usableMax = cellMax - pad;

            // マスが狭いときは乱数せず中心に置く
            if (usableMin >= usableMax)
            {
                return (cellMin + cellMax) * 0.5f;
            }

            return UnityEngine.Random.Range(usableMin, usableMax);
        }

        /// <summary>
        /// 有効なワールド範囲が設定されているか
        /// </summary>
        /// <returns></returns>
        private bool HasValidVolume()
        {
            if (_volumes == null) return false;

            foreach (var volume in _volumes)
            {
                if (volume.IsValidVolume) return true;
            }

            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// ワールド範囲をGizmosで表示する
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!HasValidVolume()) return;

            foreach (var volume in _volumes)
            {
                if (!volume.IsValidVolume) continue;

                Bounds bounds = new(volume.Center, volume.Size);
                Gizmos.color = new Color(0.55f, 0.8f, 1f, 0.5f);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                DrawGrid(bounds);
            }
        }

        /// <summary>
        /// ワールド範囲のグリッドをGizmosで表示する
        /// </summary>
        private void DrawGrid(Bounds bounds)
        {
            if (_gridCountX <= 0 || _gridCountZ <= 0) return;
            float y = bounds.center.y;
            Gizmos.color = new Color(0.55f, 0.8f, 1f, 0.45f);
            for (int x = 1; x < _gridCountX; x++)
            {
                float posX = Mathf.Lerp(bounds.min.x, bounds.max.x, x / (float)_gridCountX);
                Gizmos.DrawLine(new Vector3(posX, y, bounds.min.z), new Vector3(posX, y, bounds.max.z));
            }
            for (int z = 1; z < _gridCountZ; z++)
            {
                float posZ = Mathf.Lerp(bounds.min.z, bounds.max.z, z / (float)_gridCountZ);
                Gizmos.DrawLine(new Vector3(bounds.min.x, y, posZ), new Vector3(bounds.max.x, y, posZ));
            }
        }
#endif
    }
}
