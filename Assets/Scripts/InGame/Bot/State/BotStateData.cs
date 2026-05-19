using UnityEngine;

namespace InGame.Bot
{
    [CreateAssetMenu(fileName = "BotStateData", menuName = "ScriptableObjects/Bot/BotStateData")]
    public class BotStateData : ScriptableObject
    {
        [SerializeField] private StateData[] _states;
        public IReadOnlyState[] States => _states;
        public interface IReadOnlyState
        {
            public StateType Type { get; }
            public int Probability { get; }
        }
        [System.Serializable]
        public class StateData : IReadOnlyState
        {
            public StateType _stateType;
            public int _probability;

            public StateType Type => _stateType;

            public int Probability => _probability;
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
                sum += state._probability;
            }
            return sum;
        }
    }
    public enum StateType
    {
        None, RandomMove, Interact, Attack,
    }
}
