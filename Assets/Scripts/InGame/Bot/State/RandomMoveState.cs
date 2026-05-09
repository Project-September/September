using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.Bot
{
    /// <summary>
    /// テスト用ステート
    /// </summary>
    public class RandomMoveState : IBotState
    {
        private CancellationTokenSource _findRootToken = new();
        private bool _isFind = false;
        public List<NodeData> _nodes;
        public int _index;
        public NodeData _nextNode;
        public void OnEnter(BotStateMachine stateMachine)
        {
            GetRandomMoveRoute(stateMachine.transform.position);
        }
        private async void GetRandomMoveRoute(Vector3 position)
        {
            _isFind = true;
            _index = 0;
            var goal = NodeProvider.Instance.GetRandomNode();
            CancelFindRoute();
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

            if (_nodes != null && _nodes.Count > 0)
            {
                _nextNode = _nodes[0];
            }
        }

        public void OnExit(BotStateMachine stateMachine)
        {
            CancelFindRoute();
        }

        private void CancelFindRoute()
        {
            _findRootToken.Cancel();
            _findRootToken?.Dispose();

            _findRootToken = new();
        }

        public void OnUpdate(BotStateMachine stateMachine)
        {
            if (_isFind || _nextNode == null)
            {
                stateMachine._direction = Vector2.zero;
                return;
            }
            float distance = 0;
            distance = Vector3.Distance(stateMachine.transform.position, _nextNode.Position);

            //進めむ方向を求める
            Vector2 position = new Vector2(stateMachine.transform.position.x, stateMachine.transform.position.z);
            Vector2 goal = new Vector2(_nodes[_index].Position.x, _nodes[_index].Position.z);
            stateMachine._direction = goal - position;

            if (distance <= stateMachine._stopDistance)
            {
                _index++;
                //ゴール
                if (_index >= _nodes.Count)
                {
                    GetRandomMoveRoute(stateMachine.transform.position);
                    _nextNode = null;
                }
                else
                {
                    stateMachine._vault = _nextNode.IsValut;
                    _nextNode = _nodes[_index];
                }
            }
        }
    }
}

