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
        private bool _isFinding = false;
        private List<NodeData> _nodes;
        private int _currentIndex;
        private NodeData _nextNode;
        public void OnEnter(BotStateMachine stateMachine)
        {
            GetRandomMoveRoute(stateMachine.transform.position);
        }
        private async void GetRandomMoveRoute(Vector3 position)
        {
            _isFinding = true;
            _currentIndex = 0;
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
            _isFinding = false;

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
            //探索中は入力を入れない
            if (_isFinding || _nextNode == null)
            {
                stateMachine.InputDirection = Vector2.zero;
                return;
            }
            float distance = 0;
            distance = Vector3.Distance(stateMachine.transform.position, _nextNode.Position);

            //進む方向を求める
            Vector2 position = new Vector2(stateMachine.transform.position.x, stateMachine.transform.position.z);
            Vector2 goal = new Vector2(_nodes[_currentIndex].Position.x, _nodes[_currentIndex].Position.z);
            stateMachine.InputDirection = goal - position;

            //NextNodeに近づいたら次のノードにする
            if (distance <= stateMachine.StopDistance)
            {
                _currentIndex++;
                //ゴール
                if (_currentIndex >= _nodes.Count)
                {
                    GetRandomMoveRoute(stateMachine.transform.position);
                    _nextNode = null;
                }
                else
                {
                    stateMachine.InputIsVault = _nextNode.IsValut;
                    _nextNode = _nodes[_currentIndex];
                }
            }
        }
    }
}

