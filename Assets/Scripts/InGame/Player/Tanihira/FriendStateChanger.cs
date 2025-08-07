using Ingame.Tanihira;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendStateChanger : MonoBehaviour
    {
        [SerializeField] private FormationManager _formationManager;

        public void SetMoveState()
        {
            if (_formationManager == null)
                return;

            foreach (var friend in _formationManager.FriendsList)
            {
                // ここで攻撃状態に変更（必要に応じてターゲット設定も）
                friend.ChangeState(FriendState.Move);
                //隊列の整理をする
                _formationManager.SortFormation();
            }
        }
        
        /// <summary>
        /// 攻撃処理を行う
        /// </summary>
        /// <param name="target"></param>
        public void SetChaseState(Transform target)
        {
            if (_formationManager == null || target == null)
                return;

            foreach (var friend in _formationManager.FriendsList)
            {
                // ここで攻撃状態に変更（必要に応じてターゲット設定も）
                friend.ChangeState(FriendState.Chase);
                friend.SetDestination(target);
            }
        }
    }
}