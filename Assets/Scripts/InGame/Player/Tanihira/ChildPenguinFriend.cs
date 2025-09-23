using Fusion;
using InGame.Health;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class ChildPenguinFriend : FriendBase, IDamageable
    {
        private int _maxHealth;
        private int _currentHealth;
        private FriendState _stockFriendState;
    
        public bool IsAlive => _currentHealth > 0;
        public PlayerRef OwnerPlayerRef => Object.InputAuthority;
        public FriendState StockFriendState => _stockFriendState;

        public override void Spawned()
        {
            _maxHealth = _friendStatus.MaxHealth;
            _currentHealth = _maxHealth;
            //ペンギンのステートを設定
            _friendStateMappings[FriendState.None] = null;
            _friendStateMappings[FriendState.Idle] = null;
            _friendStateMappings[FriendState.Move] = new FriendMoveState();
            _friendStateMappings[FriendState.Chase] = new FriendChaseState();
            _friendStateMappings[FriendState.Attack] = new FriendAttackState();
            _friendStateMappings[FriendState.Stun] = new FriendStunState();
            _friendStateMappings[FriendState.Wait] = new FriendWaitState();
            base.Spawned();
        }
        public override void ChangeState(FriendState newState)
        {
            if (!HasStateAuthority)
                return;
            
            if (_isEnd)
                return;
            
            //スタンしている時には、ステートを記録して変更を加えないようにする
            if (!IsAlive)
            {
                //attackの場合はchaseに変えておく
                if(newState == FriendState.Attack)
                {
                    newState = FriendState.Chase;
                }

                _stockFriendState = newState;
            }
            else
            {
                base.ChangeState(newState);
            }
        }

        private void AddDamage(int damage)
        {
            if (_currentHealth - damage <= 0)
            {
                _stockFriendState = _currentState;
                ChangeState(FriendState.Stun);
            }
            else
            {
                _mecanimAnimator?.SetTrigger("Damage");
                IsAttack = false;
            }
            _currentHealth -= damage;
        }

        /// <summary>
        /// Stun終了時の処理
        /// </summary>
        public void RecoverStun()
        {
            if (!HasStateAuthority)
                return;
            
            _formationManager.Register(this.gameObject.GetComponent<FriendBase>());
            _currentHealth = _maxHealth;
            ChangeState(_stockFriendState);
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