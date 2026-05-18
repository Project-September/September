using UnityEngine;

namespace InGame.Bot
{
    [CreateAssetMenu(fileName = "BotStateData", menuName = "ScriptableObjects/Bot/BotStateData")]
    public class BotStateData : ScriptableObject
    {
        public StateData[] states;
        [System.Serializable]
        public class StateData
        {
            public StateType _stateType;
            public int _probability;
        }

        private int? _sumProbability;
        public int SumProbability
        {
            get
            {
                if( !_sumProbability.HasValue )
                {
                    _sumProbability = GetSumProbability();
                }

                return _sumProbability.Value;
            }
        }

        private int GetSumProbability()
        {
            int sum = 0;

            foreach(var state in states)
            {
                sum += state._probability;
            }
            return sum;
        }
    }
    public enum StateType
    {
        RandomMove,Interact,Attack,
    }
}
