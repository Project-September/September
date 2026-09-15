using System.Collections.Generic;
using Fusion;
using September.Common;
using UnityEngine;

namespace September.InGame.NauticalChart
{
    /// <summary> 
    /// 海図の霧効果を受けているプレイヤーの頭に、もやもやを付けるクラス
    /// </summary>
    public class ChartHeadMistApplier : MonoBehaviour
    {
        [Tooltip("頭に付けるもやもやプレハブ(VFX_Chart_HeadMist)")]
        [SerializeField] private GameObject _headMistPrefab;

        private readonly List<GameObject> _headMistInstances = new();

        /// <summary>
        /// インタラクトした人以外の頭に付ける。全クライアントで実行する
        /// </summary>
        /// <param name="interactPlayerRef">海図を使ったプレイヤー。この人には付けない</param>
        public void ShowHeadMist(PlayerRef interactPlayerRef)
        {
            HideHeadMist();

            if (_headMistPrefab == null)
            {
                Debug.LogWarning("[ChartHeadMistApplier] Head mist prefab が未設定です", this);
                return;
            }

            if (PlayerDatabase.Instance == null)
            {
                Debug.LogWarning("[ChartHeadMistApplier] PlayerDatabase がありません", this);
                return;
            }

            foreach (var kv in PlayerDatabase.Instance.PlayerObjectDic)
            {
                if (kv.Key == interactPlayerRef) continue; // 使った本人には付けない

                NetworkObject playerObject = kv.Value;
                if (playerObject == null) continue;

                Transform head = FindHead(playerObject);
                if (head == null)
                {
                    Debug.LogWarning($"[ChartHeadMistApplier] Head タグが見つかりません: {playerObject.name}", playerObject);
                    continue;
                }

                // 頭ボーンの子にする
                GameObject headMist = Instantiate(_headMistPrefab, head);
                _headMistInstances.Add(headMist);
            }
        }

        public void HideHeadMist()
        {
            foreach (GameObject headMist in _headMistInstances)
            {
                if (headMist == null) continue;
                Destroy(headMist);
            }

            _headMistInstances.Clear();
        }

        /// <summary>
        /// 名前ではなくHeadタグを子孫から探す
        /// </summary>
        private Transform FindHead(NetworkObject player)
        {
            if (player == null) return null;

            Transform[] children = player.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.CompareTag("Head"))
                {
                    return child;
                }
            }

            return null;
        }
    }
}
