using Fusion;
using InGame.Health;
using Ingame.Tanihira;
using NUnit.Framework;
using UnityEngine;

public class ChildPenguinFriend : FriendBase, IDamageable
{
    private int _maxHealth;
    private int _currentHealth;
    private float _attackPower;
    private FriendState _stockFriendState;
    
    public bool IsAlive => _currentHealth > 0;
    public PlayerRef OwnerPlayerRef => Object.InputAuthority;
    public FriendState StockFriendState => _stockFriendState;
    
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
        _friendStateMappings[FriendState.Stun] = new FriendStunState();
    }

    public override void ChangeState(FriendState newState)
    {
        //スタンしている時には、ステートを記録して変更を加えないようにする
        if (!IsAlive)
        {
            _stockFriendState = newState;
        }
        else
        {
            base.ChangeState(newState);
        }
    }

    private void AddDamage(int damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0)
        {
            ChangeState(FriendState.Stun);
        }
    }
    
    public void TakeHit(ref HitData hitData)
    {
        if (IsAlive)
        {
            AddDamage(hitData.Amount);
        }
    }
}