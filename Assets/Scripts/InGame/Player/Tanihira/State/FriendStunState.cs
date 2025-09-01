using Ingame.Tanihira;
using UnityEngine;

public class FriendStunState : IFriendState
{
    private float _stunTime;
    private float _stunTimer;
    private bool _isStun;
    
    public void OnEnter(FriendBase friend)
    {
        _stunTime = friend.FriendStatus.FriendStunTime;
        _stunTimer = 0;
        _isStun = true;
        //隊列から離れる
        friend.FormationManager.DeleteFriend(friend);
        //スタン時のアニメーション
        friend.Animator.Play("StunStart");
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
        if (_stunTimer >= _stunTime && _isStun)
        {
            friend.Animator.Play("Getup");
            _isStun = false;
        }
    }
}
