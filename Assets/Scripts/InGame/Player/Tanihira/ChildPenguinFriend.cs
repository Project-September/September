using Fusion;
using InGame.Health;
using UnityEngine;

namespace Ingame.Tanihira
{
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
            _mecanimAnimator?.SetTrigger("Damage");
            if (_currentHealth - damage <= 0)
            {
                _stockFriendState = _currentState;
                ChangeState(FriendState.Stun);
            }
            _currentHealth -= damage;
        }

        /// <summary>
        /// Hpをmaxまで回復
        /// </summary>
        public void RecoverHp()
        {
            _currentHealth = _maxHealth;
        }
    
        public void TakeHit(ref HitData hitData)
        {
            if (IsAlive)
            {
                AddDamage(hitData.Amount);
            }
        }

        [ContextMenu("ダメージ")]
        private void TestAddDamage()
        {
            if (IsAlive)
            {
                AddDamage(100);
            }
        }
    }
}