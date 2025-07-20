using Ingame.Tanihira;
using UnityEngine;

namespace Ingame.Tanihira
{
    public abstract class FriendStateBase : MonoBehaviour
    {
        protected FriendBase _owner;
        protected FriendStatus _status;

        public virtual void Initialize(FriendBase friend, FriendStatus status)
        {
            _owner = friend;
            _status = status;
        }
        public abstract void OnEnter();
        public abstract void OnExit();
        public abstract void OnUpdate();
    }
}
