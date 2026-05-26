using Fusion;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace Ingame.Exhibit
{
    public class SateliteCannonInteractRPCInvoker : NetworkBehaviour
    {
        [Header("サテライトキャノンプレハブ")]
        [SerializeField] private GameObject _sateliteCannonPrefab;

        [Header("Rayの設定")]
        [SerializeField] private float _rayCastHeight;
        [SerializeField] private LayerMask _raycastMask;
        [SerializeField] private float _hitDistance;

        [Header("Debug用")]
        [SerializeField] private bool _isDebug;

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void Rpc_RequestInteraction(PlayerRef requestingPlayer, Vector3 target)
        {
            // ここで他のプレイヤーに通知
            foreach (var kv in PlayerDatabase.Instance.PlayerObjectDic)
            {
                var player = kv.Key;
                if (!_isDebug && player != requestingPlayer)
                {
                    NetworkObject playerObject = kv.Value;
                    ShotCannon(playerObject.transform, requestingPlayer);
                }
            }

            //プレイヤーの向きを後ろにする
            if (PlayerDatabase.Instance.PlayerObjectDic.TryGet(requestingPlayer, out var playerNetworkObject))
            {
                Vector3 targetPos = target;
                Vector3 flatDirection = targetPos - playerNetworkObject.transform.position;
                flatDirection.y = 0f; // 上下を無視して水平成分だけ
                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                    playerNetworkObject.transform.rotation = targetRot;
                    if (playerNetworkObject.TryGetComponent<CameraController>(out var cameraController))
                    {
                        cameraController.SmoothRotateCameraTo(targetRot);
                    }
                }
            }
        }

        private void ShotCannon(Transform playerPos, PlayerRef requestingPlayer)
        {
            //上からrayを下ろし、当たった箇所にサテライトキャノンを降らせる
            RaycastHit hit;
            Vector3 rayCastOrigin = new Vector3(playerPos.position.x, _rayCastHeight, playerPos.position.z);
            if (Physics.Raycast(rayCastOrigin, Vector3.down, out hit, _hitDistance, _raycastMask))
            {
                var satelite = Instantiate(_sateliteCannonPrefab, hit.point, Quaternion.identity);
                satelite.GetComponent<SateliteCannonHitChacker>().SetOwnerPlayer(requestingPlayer);
            }
            else
            {
                Debug.Log("発射できませんでした");
            }
        }
#if UNITY_EDITOR       
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 0, 1, 0.5f); // 半透明
            Vector3 startPos = new Vector3(0, _rayCastHeight, 0);
            Gizmos.DrawWireCube(startPos, new Vector3(10, 1, 10));
        }
#endif
    }
}