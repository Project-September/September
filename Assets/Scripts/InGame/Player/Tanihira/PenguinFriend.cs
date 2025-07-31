using Fusion;
using InGame.Health;
using Ingame.Tanihira;
using UnityEngine;

public class PenguinFriend : FriendBase
{
    protected override void Awake()
    {
        base.Awake();
        //ペンギンのステートを設定
        _friendStateMappings[FriendState.Idle] = null;
        _friendStateMappings[FriendState.Formation] = new FriendFormationState();
        _friendStateMappings[FriendState.Attack] = new FriendAttackState();
    }
}