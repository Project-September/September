using Fusion;
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
        
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void Rpc_RequestInteraction(PlayerRef requestingPlayer)
        {
            // ここで他のプレイヤーに通知
            foreach (var player in Runner.ActivePlayers)
            {
                if (player != requestingPlayer)
                {
                    NetworkObject playerObject = Runner.GetPlayerObject(player);
                    ShotCannon(playerObject.transform);
                }
            }
        }

        private void ShotCannon(Transform playerPos)
        {
            //上からrayを下ろし、当たった箇所にサテライトキャノンを降らせる
            RaycastHit hit;
            Vector3 rayCastOrigin = new Vector3(playerPos.position.x, _rayCastHeight, playerPos.position.z);
            if (Physics.Raycast(rayCastOrigin, Vector3.down, out hit, _hitDistance, _raycastMask))
            {
                Instantiate(_sateliteCannonPrefab, hit.point, Quaternion.identity);
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