using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace InGame.Bot
{
    public static class AStarSystem
    {
        public static bool _isFind = false;
        private static NavMeshPath _path;
        public static NavMeshPath Path
        {
            get
            {
                if( _path == null)
                {
                    _path = new NavMeshPath();
                }
                return _path;
            }
        }

        public static async UniTask<List<NodeData>> FindRoute(Vector3 start, Vector3 end)
        {
            if (_isFind) return new();
            _isFind = true;
            List<NodeData> nodeDatas = new(NodeProvider.Instance.Nodes);

            NodeData startNode = null;
            float startNodeDis = float.MaxValue;
            NodeData endNode = null;
            float endNodeDis = float.MaxValue;
            foreach (NodeData nodeData in nodeDatas)
            {
                nodeData.ResetState();

                float startDis = Vector3.Distance(nodeData.Position, start);
                float endDis = Vector3.Distance(nodeData.Position, end);

                if ( startNodeDis > startDis && ConnectivityCheck(start,nodeData.Position))
                {
                    startNode = nodeData;
                    startNodeDis = startDis;
                }
                if (endNodeDis > endDis && ConnectivityCheck(end, nodeData.Position))
                {
                    endNode = nodeData;
                    endNodeDis = endDis;
                }
            }
            if (startNode == null || endNode == null || nodeDatas == null)
            {
                Debug.LogError("åoòHíTçıé∏îs");
                _isFind = false;
                return new List<NodeData>();
            }
            var result = await AStar(nodeDatas, startNode, endNode);
            _isFind = false;
            return result;
        }
        private static async UniTask<List<NodeData>> AStar(List<NodeData> nodeDatas, NodeData start, NodeData goal, bool isVault = true)
        {
            List<NodeData> result = new();
            List<NodeData> openNodes = new();
            Debug.DrawLine(start.Position, goal.Position, Color.yellow, 0.1f);
            void SetNodeDistance(NodeData target, float parentDis)
            {
                float g = (target.Parent != null ? target.Parent.StartDistance : 0f) + parentDis;
                float h = Vector3.Distance(target.Position, goal.Position);
                target.OpenNode(g, h);

                if (!openNodes.Contains(target))
                    openNodes.Add(target);
            }

            start.SetParent(null);
            SetNodeDistance(start, 0);

            NodeData crrentNode = start;
            int count = 1000;
            while (openNodes.Count > 0)
            {
                crrentNode = GetSmallCost(openNodes);
                Debug.DrawLine(crrentNode.Position, crrentNode.Position + Vector3.up * 2, Color.red, 0.1f);
                List<NodeData> connectNodes = new();

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
                if (isVault && crrentNode.VaultConnect != null)
                {
                    float distance = Vector3.Distance(crrentNode.VaultConnect.Position, crrentNode.Position);
                    crrentNode.IsValut = true;
                    crrentNode.VaultConnect.SetParent(crrentNode);
                    SetNodeDistance(crrentNode.VaultConnect, distance);
                }
                crrentNode.Clause();
                openNodes.Remove(crrentNode);

                if (openNodes.Contains(goal))
                {
                    crrentNode = goal;
                    break;
                }

                count--;
                if (count == 0)
                {
                    Debug.Log("ñ≥å¿ÉãÅ[Év");
                    break;
                }
            }
            if (crrentNode != goal)
            {
                Debug.LogError("åoòHíTçıé∏îs");
                return new();
            }

            NodeData prev = null;

            count = 100;
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
                    Debug.Log("ñ≥å¿ÉãÅ[Év");
                    break;
                }
            }

            result.Reverse();
            await UniTask.DelayFrame(1);
            return result;
        }
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

        private static bool HasObstacle(Vector3 from, Vector3 to)
        {
            from += Vector3.up * 0.5f;
            to += Vector3.up * 0.5f;

            Vector3 dir = to - from;
            float distance = dir.magnitude;

            if (distance <= 0.0001f) return false;
            return Physics.Raycast(
                from,
                dir / distance,
            distance
            );
        }

        public static bool ConnectivityCheck(Vector3 from,Vector3 to)
        {
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, Path))
                return false;

            if (Path.status != NavMeshPathStatus.PathComplete)
                return false;

            // ã»Ç™ÇÈïKóvÇ™Ç†ÇÈåoòHÇÕèúäOÅiï«âzÇ¶ñhé~Åj
            if (Path.corners.Length > 2)
                return false;

            // NavMeshè„ÇÃíºê¸É`ÉFÉbÉN
            if (NavMesh.Raycast(from, to, out var hit, NavMesh.AllAreas))
                return false;

            // çÇí·ç∑êßå¿
            float heightDiff = Mathf.Abs(from.y - to.y);
            if (heightDiff > 1.5f)
                return false;

            return true;
        }
    }
}
