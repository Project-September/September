using System;
using Fusion;
using Fusion.Sockets;
using InGame.Health;
using InGame.Player;
using UnityEngine;

public class TyrannoDamageable : NetworkBehaviour, IDamageable
{
    
    public Action OnDeath { get; set; }
    PlayerStatus _status;
    public bool IsAlive =>_currentHealth > 0;
    public PlayerRef OwnerPlayerRef => Object.InputAuthority;

    private bool _isInvincible ;
    
    private int _currentHealth;
    
    [SerializeField] private int _maxHealth;
    
    public override void Spawned()
    {
        _currentHealth = _maxHealth;
    }
    
    public void TakeHit(ref HitData hitData)
    {
        ApplyHit(ref hitData);

        hitData.IsLastHit = !IsAlive;

        if (HasStateAuthority)
        {
            if (!IsAlive) OnDeath?.Invoke();
            hitData.Executor?.HitExecution(hitData);
        }
    }

    void ApplyHit(ref HitData hitData)
    {
        if (!IsAlive)
        {
            hitData.Amount = 0;
            return;
        }
        if (hitData.HitActionType == HitActionType.Damage)
        {
            hitData.Amount = TakeDamage(hitData.Amount);
        }
        else if (hitData.HitActionType == HitActionType.Heal)
        {
            hitData.Amount = TakeHeal(hitData.Amount);
        }
    }
    
    int TakeDamage(int damage)
    {
        if (_isInvincible) return 0;
        var prevHealth = _currentHealth;
        _currentHealth = Mathf.Clamp(_currentHealth - damage, 0, _maxHealth);
        return prevHealth - _status.CurrentHealth;
    }

    int TakeHeal(int heal)
    {
        if (_isInvincible) return 0;
        var prevHealth = _currentHealth;
        _currentHealth = Mathf.Clamp(_currentHealth + heal, 0, _maxHealth);
        return prevHealth - _status.CurrentHealth;
    }
}
