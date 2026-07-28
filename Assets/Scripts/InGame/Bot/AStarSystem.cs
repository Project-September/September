using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;

namespace InGame.Bot
{
    public static class AStarSystem
    {
        private static bool _isFinding = false;
        private static NavMeshPath _path;
        public static NavMeshPath Path
        {
            get
            {
                if (_path == null)
                {
                    _path = new NavMeshPath();
                }
                return _path;
            }
        }

        public static List<NodeData> FindRoute(Vector3 start, Vector3 end)
        {
            if (_isFinding) return new();
            _isFinding = true;
            List<NodeData> nodeDatas = NodeProvider.Instance.Nodes;

            NodeData startNode = null;
            float startNodeDis = float.MaxValue;
            NodeData endNode = null;
            float endNodeDis = float.MaxValue;
            //最短のノードを検索する
            foreach (NodeData nodeData in nodeDatas)
            {
                nodeData.ResetState();

                float startDis = (nodeData.Position - start).sqrMagnitude;
                float endDis = (nodeData.Position - end).sqrMagnitude;

                if (startNodeDis > startDis && ConnectivityCheck(start, nodeData.Position, false))
                {
                    startNode = nodeData;
                    startNodeDis = startDis;
                }
                if (endNodeDis > endDis && ConnectivityCheck(end, nodeData.Position, false))
                {
                    endNode = nodeData;
                    endNodeDis = endDis;
                }
            }
            if (startNode == null || endNode == null || nodeDatas == null)
            {
                Debug.Log("経路探索失敗");
                _isFinding = false;
                return new List<NodeData>();
            }
            //実際のA*アルゴリズム
            var result = AStar(nodeDatas, startNode, endNode);
            _isFinding = false;
            return result;
        }

        /// <summary>
        /// 経路探索をする
        /// </summary>
        /// <param name="nodeDatas">nodeData</param>
        /// <param name="start">開始ノード</param>
        /// <param name="goal">ゴールノード</param>
        /// <param name="isVault">飛び越えをするか</param>
        /// <returns>経路順のNodeData</returns>
        private static List<NodeData> AStar(List<NodeData> nodeDatas, NodeData start, NodeData goal, bool isVault = true)
        {
            List<NodeData> result = new();
            NodeHeap openNodes = new();
            Debug.DrawLine(start.Position, goal.Position, Color.yellow, 0.1f);
            //オープン処理
            void SetNodeDistance(NodeData target, float parentDis)
            {
                float g = (target.Parent != null ? target.Parent.StartDistance : 0f) + parentDis;
                float h = Vector3.Distance(target.Position, goal.Position);
                target.OpenNode(g, h);

                openNodes.Enqueue(target);
            }

            start.SetParent(null);
            SetNodeDistance(start, 0);

            NodeData crrentNode = start;
            int count = nodeDatas.Count;
            //探索本体
            while (openNodes.Count > 0)
            {
                crrentNode = openNodes.Dequeue();

                if (crrentNode.State == NodeState.Closed)
                    continue;

                //ゴールしたら探索を終える
                if (crrentNode == goal)
                {
                    break;
                }

                Debug.DrawLine(crrentNode.Position, crrentNode.Position + Vector3.up * 2, Color.red, 0.1f);
                List<NodeData> connectNodes = new();

                //接続ノードをOpenにする
                foreach (var connectNode in crrentNode.ConnectNode)
                {
                    Debug.DrawLine(crrentNode.Position, connectNode.Position, Color.gray, 0.1f);

                    float parentDistance = Vector3.Distance(connectNode.Position, crrentNode.Position);

                    if (connectNode.State == NodeState.Closed) continue;

                    float newG = crrentNode.StartDistance + parentDistance;

                    if (connectNode.State == NodeState.None)
                    {
                        connectNode.SetParent(crrentNode);
                        SetNodeDistance(connectNode, parentDistance);
                    }
                    else if (connectNode.State == NodeState.Open)
                    {
                        if (newG < connectNode.StartDistance)
                        {
                            connectNode.SetParent(crrentNode);
                            SetNodeDistance(connectNode, parentDistance);
                        }
                    }
                }
                //飛び越え接続の処理
                if (isVault && crrentNode.VaultConnect != null)
                {
                    if (crrentNode.VaultConnect.State != NodeState.Closed)
                    {
                        float distance = Vector3.Distance(crrentNode.VaultConnect.Position, crrentNode.Position);
                        crrentNode.IsValut = true;
                        crrentNode.VaultConnect.SetParent(crrentNode);
                        SetNodeDistance(crrentNode.VaultConnect, distance);
                    }
                }
                //探索したノードをCloseにする
                crrentNode.Close();


                count--;
                if (count == 0)
                {
                    Debug.Log("無限ループ");
                    break;
                }
            }
            if (crrentNode != goal)
            {
                Debug.LogError($"経路探索失敗 \n start{start.Position}  goal{goal.Position}");
                return new();
            }

            NodeData prev = null;

            count = 100;

            //Parentを元に復元をする
            while (crrentNode != null)
            {
                result.Add(crrentNode);

                if (prev != null)
                {
                    Debug.DrawLine(prev.Position, crrentNode.Position, Color.green, 1f);
                }

                prev = crrentNode;
                crrentNode = crrentNode.Parent;
                count--;
                if (count == 0)
                {
                    Debug.Log("無限ループ");
                    break;
                }
            }

            result.Reverse();
            return result;
        }

        /// <summary>
        /// 最小コストのノードを取得する
        /// </summary>
        /// <param name="nodeDatas">検索するノードリスト</param>
        /// <returns>最小コストのノード</returns>
        private static NodeData GetSmallCost(List<NodeData> nodeDatas)
        {
            NodeData best = nodeDatas[0];
            float bestCost = best.Cost;

            for (int i = 1; i < nodeDatas.Count; i++)
            {
                if (nodeDatas[i].Cost < bestCost)
                {
                    best = nodeDatas[i];
                    bestCost = best.Cost;
                }
            }
            return best;
        }

        /// <summary>
        /// 直線的に接続ができるかを確認する
        /// </summary>
        public static bool ConnectivityCheck(Vector3 from, Vector3 to, bool isNavMeshCheck)
        {
            // 高低差制限
            float heightDiff = Mathf.Abs(from.y - to.y);
            if (heightDiff > 1.5f)
                return false;

            if (!isNavMeshCheck)
                return true;

            // NavMesh上の直線チェック
            if (NavMesh.Raycast(from, to, out var hit, NavMesh.AllAreas))
                return false;

            // 経路探索
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, Path))
                return false;

            if (Path.status != NavMeshPathStatus.PathComplete)
                return false;

            // 曲がる必要がある経路は除外（壁越え防止）
            if (Path.corners.Length > 2)
                return false;

            return true;
        }
    }
    public class NodeHeap
    {
        private readonly List<NodeData> _heap = new();

        public int Count => _heap.Count;

        public void Enqueue(NodeData node)
        {
            _heap.Add(node);

            int index = _heap.Count - 1;

            while (index > 0)
            {
                int parent = (index - 1) / 2;

                if (_heap[parent].Cost <= _heap[index].Cost)
                    break;

                (_heap[parent], _heap[index]) =
                    (_heap[index], _heap[parent]);

                index = parent;
            }
        }

        public NodeData Dequeue()
        {
            NodeData result = _heap[0];

            int lastIndex = _heap.Count - 1;

            _heap[0] = _heap[lastIndex];

            _heap.RemoveAt(lastIndex);

            int index = 0;

            while (true)
            {
                int left = index * 2 + 1;
                int right = index * 2 + 2;

                if (left >= _heap.Count)
                    break;

                int smallest = left;

                if (right < _heap.Count &&
                    _heap[right].Cost < _heap[left].Cost)
                {
                    smallest = right;
                }

                if (_heap[index].Cost <= _heap[smallest].Cost)
                    break;

                (_heap[index], _heap[smallest]) =
                    (_heap[smallest], _heap[index]);

                index = smallest;
            }

            return result;
        }
    }
}
