using System;
using System.Linq;
using Fusion;
using InGame.Common;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ability
{
    [Serializable]
    public abstract class AbilityBase
    {
        public enum AbilityPhase
        {
            None,
            Started,
            Active,
            Ending,
            Ended
        }

        [SerializeField] private AbilityName _abilityName;
        [SerializeField] private SerializableDictionary<CharacterType, float> _cooldownTimeDictionary = new();
        [SerializeField] private SerializableDictionary<CharacterType, float> _eachCharacterLastActiveTime = new();
        [Header("Startが呼ばれたらすぐにクールダウンを開始するかどうか")]
        [SerializeField] protected bool _startCooldownImmediately = true;
        
        protected ISpawner _spawner;
        public event Action OnEndAbilityEvent;
        public AbilityName AbilityName => _abilityName;
        public bool StartCooldownImmediately => _startCooldownImmediately;
        public SerializableDictionary<CharacterType, float> CooldownTimeDictionary => _cooldownTimeDictionary;
        public SerializableDictionary<CharacterType, float> EachCharacterLastActiveTime => _eachCharacterLastActiveTime;
        public AbilityContext Context { get; private set; }
        protected int OwnerPlayerId { get; private set; } = -1;
        public AbilityPhase Phase { get; private set; } = AbilityPhase.None;
        protected AbilityBase() { }

        protected AbilityBase(AbilityBase abilityReference)
        {
            _abilityName = abilityReference._abilityName;
            _startCooldownImmediately = abilityReference._startCooldownImmediately;
        }

        public abstract AbilityBase Clone(AbilityBase abilityReference);

        public virtual void InitAbility(AbilityContext context, ISpawner spawner)
        {
            Context = context;
            OwnerPlayerId = context.SourcePlayer;
            _spawner = spawner ?? StaticServiceLocator.Instance.Get<ISpawner>();
            Phase = AbilityPhase.Started;
        }

        public void Tick(float deltaTime)
        {
            ProcessPhase(deltaTime);
        }
        
        private void ProcessPhase(float deltaTime)
        {
            switch (Phase)
            {
                case AbilityPhase.None:
                    break;
                case AbilityPhase.Started:
                    OnStart();
                    Phase = AbilityPhase.Active;
                    break;
                case AbilityPhase.Active:
                    OnUpdate(deltaTime);
                    break;
                case AbilityPhase.Ending:
                    ExecuteEndAbility();
                    break;
                case AbilityPhase.Ended:
                    break;
            }
        }

        /// <summary>
        /// 全体にアビリティ開始を通知する
        /// </summary>
        /// <remarks>
        /// この関数は全体への同期を目的としています（RPC等）
        /// アニメーションなど全体に通知すべきものだけを書いてください。
        /// 判定処理などはOnStart、OnUpdateなどのメソッドで行ってください。
        /// </remarks>
        /// <param name="context"></param>
        public virtual void OnStartNotifyAll(AbilityContext context) { }
        protected virtual void OnStart() { }
        protected virtual void OnUpdate(float deltaTime) { }

        public virtual void OnEndAbility() { }

        public virtual void ForceEnd()
        {
            Phase = AbilityPhase.Ending;
        }

        private void ExecuteEndAbility()
        {
            OnEndAbility();
            OnEndAbilityEvent?.Invoke();
            Phase = AbilityPhase.Ended;
        }
    }
}
