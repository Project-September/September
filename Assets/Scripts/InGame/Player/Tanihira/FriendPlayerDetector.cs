using System.Linq;
using Fusion;
using September.Common;
using September.InGame.Common;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendPlayerDetector : NetworkBehaviour
    {
        [SerializeField] private Transform _detectionCenter;
        [SerializeField] private float _detectionRadius;
        [SerializeField] private LayerMask _detectionMask;
        [SerializeField] private LayerMask _obstacleMask;
        [SerializeField] private FriendStateChanger _friendStateChanger;
        [SerializeField] private FormationManager _formationManager;
        [SerializeField] private bool _isWaiting;
        
        private Transform _currentTarget;
        private InGameManager _inGameManager;

        private void Start()
        {
            if (HasStateAuthority)
            {
                _inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();
                if (_inGameManager)
                {
                    _inGameManager.GameStarted += GameStart;
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            
            if (_isWaiting)
            {
                DetectePlayer();
            }
        }
        
        private void GameStart()
        {
            _isWaiting = true;
        }

        //プレイヤーを索敵する処理
        private void DetectePlayer()
        {
            if (IsTargetValid(_currentTarget) || _formationManager.CurrentFriendsList.FirstOrDefault().CurrentState == FriendState.None)
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
                
                // Rayの始点を少し上に
                Vector3 start = _detectionCenter.position + Vector3.up * 0.5f;

                // ターゲットの中心も少し上に
                Vector3 targetCenter = player.position + Vector3.up * 0.5f;

                Vector3 direction = (targetCenter - start).normalized;
                float distance = Vector3.Distance(start, targetCenter);
                
                //間に障害物があった場合には無視
                if (Physics.Raycast(start, direction, out RaycastHit hit, distance, _obstacleMask))
                {
                    //Debug.Log("障害物ヒット: " + hit.collider.name + " / Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
                    continue;
                }
                
                //近い物をターゲットにして攻撃させる
                _currentTarget = player.gameObject.transform;
            }
            
            //ペンギンに攻撃指示を飛ばす
            if (_currentTarget)
            {
                _friendStateChanger.SetChaseState(_currentTarget);
            }
            else
            {
                _friendStateChanger.SetMoveState();
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