using System;
using System.Linq;
using Fusion;
using UnityEngine;

namespace  InGame.Player.Ability
{
    public class AbilityParameter
    {
        public NetworkObject Owner { get; set; }
    }
    
    /// <summary>
    /// アビリティの基底クラス
    /// EndedかNoneのときにStartAbility()を呼ぶことで開始できる
    /// </summary>
    [Serializable]
    public class AbilityBase
    {
        public enum AbilityPhase
        {
            Available,
            Started,
            Active,
            Ending,
            Cooldown,
        }

        [SerializeField] protected float _cooldown;
        [SerializeField] protected AbilityPhase _phase = AbilityPhase.Available;
        private NetworkRunner Runner => NetworkRunner.Instances.FirstOrDefault();
        public AbilityParameter Parameter { get; set; }
        protected float CooldownEndTime { get; private set; }
        public AbilityPhase Phase => _phase;

        public void Start(AbilityParameter parameter)
        {
            Parameter = parameter;
            _phase = AbilityPhase.Started;
        }
        
        public void Tick(float deltaTime)
        {
            ProcessPhase(deltaTime);
        }

        private void ProcessPhase(float deltaTime)
        {
            switch (_phase)
            {
                case AbilityPhase.Started:
                    OnStart();
                    _phase = AbilityPhase.Active;
                    break;
                case AbilityPhase.Active:
                    OnUpdate(deltaTime);
                    break;
                case AbilityPhase.Ending:
                    OnEndAbility();
                    break;
                case AbilityPhase.Cooldown:
                    OnCooldown();
                    break;
            }
        }
     
        /// <summary>
        /// Trueを返すとAbilityがつかえる
        /// </summary>
        /// <returns></returns>
        public virtual bool CanStartAbilityOverride() => true;
        protected virtual void OnStart() { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnEndAbility()
        {
            CooldownEndTime = Runner ? Runner.SimulationTime + _cooldown : Time.time + _cooldown;
            _phase = AbilityPhase.Cooldown;
        }

        protected void OnCooldown()
        {
            if (Runner ? Runner.SimulationTime >= CooldownEndTime : Time.time >= CooldownEndTime)
            {
                _phase = AbilityPhase.Available;
            }
        }
    }
}
