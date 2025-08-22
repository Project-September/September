using Ingame.Tanihira;
using UnityEngine;

namespace Ingame.Tanihira
{
    public interface IFriendState
    {
        public abstract void OnEnter(FriendBase friend);
        public abstract void OnExit(FriendBase friend);
        public abstract void OnUpdate(FriendBase friend);
    }
}
