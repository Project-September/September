using System.Linq;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendPlayerDetector : MonoBehaviour
    {
        [SerializeField] private Transform _detectionCenter;
        [SerializeField] private float _detectionRadius;
        [SerializeField] private LayerMask _detectionMask;
        [SerializeField] private LayerMask _obstacleMask;
        [SerializeField] private FriendStateChanger _friendStateChanger;
        
        private Transform _currentTarget;
        

        private void Update()
        {
            DetectePlayer();
        }

        //プレイヤーを索敵する処理
        private void DetectePlayer()
        {
            if (IsTargetValid(_currentTarget))
                return;
            
            //範囲内のプレイヤーを検出
            Collider[] players = Physics.OverlapSphere(_detectionCenter.position, _detectionRadius, _detectionMask);
            
            //近い順にソート
            var uniqueRoots  = players
                .Select(col => col.transform.root)                         // ルート Transform を抽出
                .Where(root => root != transform.root)                   // 自分自身は除外
                .Distinct()                                                // 重複排除
                .OrderBy(root => (root.position - _detectionCenter.position).sqrMagnitude) // 距離順
                .ToList();

            foreach (Transform player in uniqueRoots)
            {
                //自身は除く
                if (player.gameObject == gameObject)
                    continue;
                
                Vector3 direction = (player.transform.position - _detectionCenter.position).normalized;
                float distance = Vector3.Distance(_detectionCenter.position, player.transform.position);
                
                //間に障害物があった場合には無視
                if(Physics.Raycast(_detectionCenter.position, direction, out RaycastHit hit, distance, _obstacleMask))
                    continue;
                
                //近い物をターゲットにして攻撃させる
                _currentTarget = player.gameObject.transform;
                Debug.Log(player.gameObject.name);
            }
            
            //ペンギンに攻撃指示を飛ばす
            if (_currentTarget != null)
            {
                _friendStateChanger.SetChaseState(_currentTarget);
                Debug.Log("攻撃！！");
            }
            else
            {
                _friendStateChanger.SetMoveState();
                Debug.Log("隊列に戻る");
            }
        }
        
        //ターゲットが視野角内にいるか
        private bool IsTargetValid(Transform target)
        {
            if (target == null) 
                return false;

            Vector3 direction = (target.position - _detectionCenter.position).normalized;
            float distance = Vector3.Distance(_detectionCenter.position, target.position);

            // 範囲外
            if (distance > _detectionRadius)
            {
                _currentTarget = null;
                return false;
            }

            // 視線が通らない
            if (Physics.Raycast(_detectionCenter.position, direction, distance, _obstacleMask))
            {
                _currentTarget = null;
                return false;
            }

            return true;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_detectionCenter == null)
                return;

            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 赤色で半透明
            Gizmos.DrawWireSphere(_detectionCenter.position, _detectionRadius);
            Gizmos.DrawSphere(_detectionCenter.position, 0.05f); // 中心点のマーカー
        }
    }
}