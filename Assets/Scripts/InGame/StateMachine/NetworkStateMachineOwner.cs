using Fusion;
using UnityEngine;

namespace September.Common
{
    public abstract class NetworkStateMachineOwner<TContext> : NetworkBehaviour where TContext : NetworkStateMachineOwner<TContext>
    {
        [SerializeField] protected ImtStateMachine<TContext>.State[] _states;
        
        protected ImtStateMachine<TContext> _stateMachine;
        
        public override void Spawned()
        {
            _stateMachine = new ImtStateMachine<TContext>((TContext)this, _states);
            //  クライアント側でもFixedUpdateNetworkが更新されるようにSetIsSimulated()で登録する
            Runner.SetIsSimulated(Object, true);
            InitializeStateMachine();
        }
        
        public override void FixedUpdateNetwork()
        {
            _stateMachine?.Update();
        }
        
        protected abstract void InitializeStateMachine();
        
        /// <summary>
        /// ステート遷移イベントを送信する
        /// </summary>
        /// <param name="eventId">遷移イベントID。現在のステートに、この遷移イベントに対応する遷移先が存在すれば遷移する。</param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void Rpc_SendEvent(int eventId)
        {
            _stateMachine.SendEvent(eventId);
        }
    }
}