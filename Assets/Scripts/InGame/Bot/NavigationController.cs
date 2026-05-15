using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.Bot
{
    [System.Serializable]
    public class NavigationController
    {
        public float StopDistance;
        public bool CanVault;
        public bool IsComplete { get; private set; } = false;
        public bool IsVaultInput { get; private set; } = false;
        public Vector2 InputDirection { get; private set; }

        private Vector3 _currentGoalPosition;
        private bool _isFinding;
        private List<NodeData> _navigationData = new();
        private int _currentIndex;
        private NodeData _nextNode;

        private CancellationTokenSource _findRootToken = new();
        public void GetDestinationInput(Vector3 goalPosition, Vector3 playerPosition)
        {
            if (_currentGoalPosition != goalPosition)
            {
                IsComplete = false;
                _currentGoalPosition = goalPosition;
                GetNavigationRoute(playerPosition);
                InputDirection = Vector2.zero;
                return;
            }


            //探索中・ゴール後は入力を入れない
            if (_isFinding || _navigationData.Count == 0 || _nextNode == null)
            {
                InputDirection = Vector2.zero;
                return;
            }

            float distance = 0;
            distance = (playerPosition - _nextNode.Position).sqrMagnitude;

        

            //NextNodeに近づいたら次のノードにする
            if (distance <= StopDistance * StopDistance)
            {
                _currentIndex++;
                //ゴール
                if (_currentIndex >= _navigationData.Count)
                {
                    IsComplete = true;
                    _nextNode = null;
                    InputDirection = Vector2.zero;
                    return;
                }
                else
                {
                    IsVaultInput = _nextNode.IsValut;
                    _nextNode = _navigationData[_currentIndex];
                }
            } 

            //進む方向を求める
            Vector2 playerPos = new Vector2(playerPosition.x, playerPosition.z);
            Vector2 goalPos = new Vector2(_navigationData[_currentIndex].Position.x, _navigationData[_currentIndex].Position.z);

            IsComplete = false;
            InputDirection = (goalPos - playerPos).normalized;
            Debug.Log($"next { _nextNode.Position}");
            return;
        }

        private async void GetNavigationRoute(Vector3 position)
        {
            _isFinding = true;
            _currentIndex = 0;
            var goal = NodeProvider.Instance.GetRandomNode();
            CancelFindRoute();
            try
            {
                _navigationData = await AStarSystem.FindRoute(position, goal.Position)
                    .Timeout(TimeSpan.FromSeconds(5))
                    .AttachExternalCancellation(_findRootToken.Token);
            }
            catch (TimeoutException)
            {
                await UniTask.Yield();
                Debug.Log("経路探索タイムアウト");
                GetNavigationRoute(position);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("外部キャンセル");
            }

            if (_navigationData == null || _navigationData.Count == 0)
            {
                await UniTask.Yield();
                GetNavigationRoute(position);
            }
            _isFinding = false;

            if (_navigationData != null && _navigationData.Count > 0)
            {
                _nextNode = _navigationData[0];
            }
        }
        private void CancelFindRoute()
        {
            _findRootToken.Cancel();
            _findRootToken?.Dispose();

            _findRootToken = new();
        }
        public void ShowGizumo()
        {
            if (_navigationData.Count == 0) return;

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_nextNode.Position,0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_navigationData[_navigationData.Count -1].Position,0.5f);
            //Gizmos.color = Color.green;
            //Gizmos.DrawSphere(_currentGoalPosition,0.5f);
        }
    }
}
