using Fusion;
using InGame.Health;
using Ingame.Tanihira;
using UnityEngine;

public class ChildPenguinFriend : FriendBase, IDamageable
{
    private int _maxHealth;
    private int _currentHealth;
    private float _attackPower;
    
    public bool IsAlive => _currentHealth > 0;
    public PlayerRef OwnerPlayerRef => Object.InputAuthority;
    
    protected override void Awake()
    {
        base.Awake();
        _maxHealth = FriendStatus.MaxHealth;
        _currentHealth = _maxHealth;
        _attackPower = FriendStatus.AttackPower;
        //ペンギンのステートを設定
        _friendStateMappings[FriendState.Idle] = null;
        _friendStateMappings[FriendState.Move] = new FriendMoveState();
        _friendStateMappings[FriendState.Chase] = new FriendChaseState();
        _friendStateMappings[FriendState.Attack] = new FriendAttackState();
    }
    
    public void TakeHit(ref HitData hitData)
    {
        
    }
}