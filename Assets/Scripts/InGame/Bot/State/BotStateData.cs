using System.Collections.Generic;
using UnityEngine;

namespace InGame.Bot
{
    [CreateAssetMenu(fileName = "BotStateData", menuName = "ScriptableObjects/Bot/BotStateData")]
    public class BotStateData : ScriptableObject
    {
        [SerializeField] private StateData[] _states;
        public IReadOnlyList<IReadOnlyState> States => _states;
        public interface IReadOnlyState
        {
            public StateType Type { get; }
            public int Probability { get; }
        }
        [System.Serializable]
        public class StateData : IReadOnlyState
        {
            public StateType Type;
            public int Probability;

            StateType IReadOnlyState.Type => Type;

            int IReadOnlyState.Probability => Probability;
        }

        private int? _sumProbability;
        public int SumProbability
        {
            get
            {
                if (!_sumProbability.HasValue)
                {
                    _sumProbability = GetSumProbability();
                }

                return _sumProbability.Value;
            }
        }

        private int GetSumProbability()
        {
            int sum = 0;

            foreach (var state in _states)
            {
                sum += state.Probability;
            }
            return sum;
        }
    }
    public enum StateType
    {
        None, RandomMove, Interact, Attack,
    }
}
