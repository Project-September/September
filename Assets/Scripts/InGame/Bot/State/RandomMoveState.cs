using System.Collections.Generic;
using InGame.Bot;
using UnityEditorInternal;
using UnityEngine;

namespace September
{
    public class RandomMoveState : IBotState
    {
        private List<NodeData> _nodes;
        private int _index;
        public void  OnEnter(BotStateMachine stateMachine)
        {
            GetRandomMoveRoute(stateMachine.transform.position);
        }
        private async void GetRandomMoveRoute(Vector3 position)
        {
            _index = 0;
            var goal = NodeProvider.Instance.GetRandomNode();
            _nodes = await AStarSystem.FindRoute(position, goal.Position);
        }

        public void OnExit(BotStateMachine stateMachine)
        {

        }

        public void OnUpdate(BotStateMachine stateMachine)
        {
            if (Vector3.Distance(stateMachine.transform.position,_nodes[_index].Position) <= stateMachine._stopDistance)
            {
                _index++;

                if(_index >= _nodes.Count)
                {
                    GetRandomMoveRoute(stateMachine.transform.position);
                }
            }
            Vector2 position = new Vector2(stateMachine.transform.position.x, stateMachine.transform.position.z);
            Vector2 goal = new Vector2(_nodes[_index].Position.x, _nodes[_index].Position.z);
            stateMachine._direction = goal - position;
        }
    }
}
