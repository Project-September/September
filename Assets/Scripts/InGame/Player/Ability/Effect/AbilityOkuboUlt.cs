using InGame.Player.Okubo;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    public class AbilityOkuboUlt : AbilityUltBase
    {
        [SerializeField] private ThrowBomb _throwBomb;
        [SerializeField] private float _duration;
        [SerializeField] private float _bombThrowDelay;

        private bool _isThrow;
        private float _startTime;

        protected override void OnCutInStart()
        {
            _startTime = Runner.SimulationTime;
            _isThrow = true;
        }

        protected override void OnCutInUpdate(float deltaTime)
        {
            if (_isThrow && Runner.SimulationTime - _startTime >= _bombThrowDelay)
            {
                _throwBomb.OnThrow();
                _isThrow = false;
            }
        }

        protected override void OnUpdateUlt(float deltaTime)
        {
            if (TimeSinceCutInEnd > _duration)
            {
                RequestEndAbility();
            }
        }
    }
}