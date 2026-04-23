using Fusion;
using InGame.Player;
using UnityEngine;

namespace InGame.StatusEffect
{
    /// <summary>
    /// Stat、Effect、外部、Network、Inspectorの仲介をする
    /// </summary>
    public class PlayerStatusEffect : NetworkBehaviour
    {
        PlayerStatus p;

        [SerializeField] ParameterData[] _parameters;

        private StatContainer _statContainer;
        private EffectManager _effectManager;

        public override void Spawned()
        {
            _statContainer = new(_parameters);
            _effectManager = new();
        }

    }
}
