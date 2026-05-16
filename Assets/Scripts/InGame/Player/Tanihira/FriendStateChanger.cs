using Ingame.Tanihira;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendStateChanger : MonoBehaviour
    {
        [SerializeField] private FormationManager _formationManager;
        [SerializeField] private GameObject _friendOwner;
        
        public void SetMoveState()
        {
            if (_formationManager == null)
                return;

            foreach (var friend in _formationManager.FriendsList)
            {
                friend.SetDestination(_friendOwner.transform);
                friend.ChangeState(FriendState.Move);
                //隊列の整理をする
                _formationManager.SortFormation();
            }
        }

        public void IndependentFrienState(Transform destination)
        {
            if (_formationManager == null)
                return;

            foreach (var friend in _formationManager.FriendsList)
            {
                friend.SetDestination(destination);
                friend.ChangeState(FriendState.Move);
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
                if (friend.IsAttackPossible) //攻撃処理
                {
                    friend.SetDestination(target);
                    friend.ChangeState(FriendState.Chase);
                }
            }
        }
    }
}