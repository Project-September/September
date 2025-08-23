using Ingame.Tanihira;
using UnityEngine;

public class FriendStunState : IFriendState
{
    private float _stunTime;
    private float _stunTimer;
    
    public void OnEnter(FriendBase friend)
    {
        _stunTime = friend.FriendStatus.FriendStunTime;
        _stunTimer = 0;
        //隊列から離れる
        friend.FormationManager.DeleteFriend(friend);
        //スタン時のアニメーション
        Debug.Log("スタンしました！");
    }

    public void OnExit(FriendBase friend)
    {
        Debug.Log("スタンから回復");
    }

    public void OnUpdate(FriendBase friend)
    {
        _stunTimer += friend.Runner.DeltaTime;
        //スタン時間が終わった後の処理
        if (_stunTimer >= _stunTime)
        {
            ChildPenguinFriend childPenguinFriend = friend as ChildPenguinFriend;
            if (childPenguinFriend != null)
            {
                friend.FormationManager.Register(friend);
                childPenguinFriend.RecoverHp();
                friend.ChangeState(childPenguinFriend.StockFriendState);
            }
        }
    }
}
