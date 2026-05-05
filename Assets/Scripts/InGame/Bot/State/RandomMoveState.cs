using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.Bot
{
    public class RandomMoveState : IBotState
    {
        private CancellationTokenSource _findRootToken = new();
        private bool _isFind = false;
        public List<NodeData> _nodes;
        public int _index;
        public void  OnEnter(BotStateMachine stateMachine)
        {
            GetRandomMoveRoute(stateMachine.transform.position);
        }
        private async void GetRandomMoveRoute(Vector3 position)
        {
            _isFind = true;
            _index = 0;
            var goal = NodeProvider.Instance.GetRandomNode();
            _findRootToken = new();
            try
            {
                _nodes = await AStarSystem.FindRoute(position, goal.Position)
                    .Timeout(TimeSpan.FromSeconds(5))
                    .AttachExternalCancellation(_findRootToken.Token);
            }
            catch (TimeoutException)
            {
                Debug.Log("経路探索タイムアウト");
                GetRandomMoveRoute(position);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("外部キャンセル");
            }

            if (_nodes == null || _nodes.Count == 0)
            {
                GetRandomMoveRoute(position);
            }
            _isFind = false;
        }

        public void OnExit(BotStateMachine stateMachine)
        {
            _findRootToken.Cancel();
            _findRootToken?.Dispose();

            _findRootToken = new();
        }

        public void OnUpdate(BotStateMachine stateMachine)
        {
            if (_isFind || _nodes == null || _nodes.Count == 0)
            {
                stateMachine._direction = Vector2.zero;
                return;
            }
            _index = Mathf.Clamp(_index, 0, _nodes.Count - 1);
            Debug.Log($"{_index}/{_nodes.Count}");
            if (Vector3.Distance(stateMachine.transform.position, _nodes[_index].Position) <= stateMachine._stopDistance)
            {
                stateMachine._vault = _nodes[_index].IsValut;
                _index++;

                if (_index >= _nodes.Count)
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
