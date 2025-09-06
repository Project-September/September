using Fusion;
using InGame.Health;
using Ingame.Tanihira;
using UnityEngine;

public class BossPenguinFriend : FriendBase, IFriendBuff
{
    protected override void Awake()
    {
        base.Awake();
        //ペンギンのステートを設定
        _friendStateMappings[FriendState.None] = null;
        _friendStateMappings[FriendState.Idle] = null;
        _friendStateMappings[FriendState.Move] = new FriendMoveState();
        _friendStateMappings[FriendState.Chase] = new FriendChaseState();
        _friendStateMappings[FriendState.Attack] = new FriendAttackState();
        _friendStateMappings[FriendState.Wait] = new FriendWaitState();
    }

    public void StartBuff()
    {
        throw new System.NotImplementedException();
    }

    public void StopBuff()
    {
        throw new System.NotImplementedException();
    }
}