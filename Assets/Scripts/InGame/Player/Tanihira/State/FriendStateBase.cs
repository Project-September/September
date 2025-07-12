using Ingame.Tanihira;
using UnityEngine;

namespace Ingame.Tanihira
{
    public abstract class FriendStateBase : MonoBehaviour
    {
        protected FriendBase _owner;

        public virtual void Initialize(FriendBase friend)
        {
            _owner = friend;
        }
        public abstract void OnEnter();
        public abstract void OnExit();
        public abstract void OnUpdate();
    }
}
