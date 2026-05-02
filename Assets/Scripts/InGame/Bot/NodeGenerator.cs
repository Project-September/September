using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using InGame.Player;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using static Codice.Client.Common.EventTracking.TrackFeatureUseEvent.Features.DesktopGUI.Filters;

namespace InGame.Bot
{
    public class NodeGenerator : MonoBehaviour
    {
        [SerializeField] private NodeMapData _mapData;
        [SerializeField] private Transform _origin;
        [SerializeField] private bool _isDrawGizumoIcon;

        [Header("Subdivision")]
        [SerializeField] private int _subdivisionHierarchy = 3;
        [SerializeField] private float _subdivisionDistance = 5f;

        [Header("Node Clean")]
        [SerializeField] private float _removeCloseDistance = 2f;

        [Header("Connect")]
        [SerializeField] private float _connectDistance = 10f;
        [SerializeField] private int _maxConnectCount = 4;

        [Header("Raycast")]
        [SerializeField] private float _rayDistance = 10f;
        [SerializeField] private float _rayUpAmount = 1f;
        [SerializeField] private float _groundRayUp = 2f;
        [SerializeField] private LayerMask _obstacleMask = ~0;

        [Header("Precision")]
        [SerializeField] private float _positionQuantize = 1000f;

        [Header("NavMesh")]
        [SerializeField] private float _navMeshTolerance = 2f;
        [Header("Vault")]
        [SerializeField] private CapsuleCollider _botCapsuleCollider;
        [SerializeField, Tooltip("最大高さ")] private float _maxLedgeHeight;
        [SerializeField, Tooltip("最小高さ")] private float _minLedgeHeight;
        [SerializeField, Tooltip("最大奥行")] private float _maxLedgeDepth;
        [SerializeField] private float _reachDistance;
        [SerializeField] private float _timeToVault;
        [SerializeField] private AnimationCurve _vaultCurve;
        [SerializeField, Tooltip("地面と認識する最大角度")] private float _groundSlopeThreshold = 45f;
        [SerializeField] private LayerMask _groundLayer = ~0;

        private List<Vector3> _nodePositions = new();
        public List<NodeData>[,,] Nodes;
        private NavMeshPath _path = new();

        private Vector3 _offset;
        private float _invCell;

        private bool _isGenerating;

        private static readonly Vector3[] directions =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
        };

        private static readonly Vector3Int[] neighborOffsets = BuildNeighborOffsets();

        private static Vector3Int[] BuildNeighborOffsets()
        {
            var list = new List<Vector3Int>(26);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;
                        list.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            return list.ToArray();
        }

        public void Awake()
        {
            Destroy(this);

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        [Button("ノード生成")]
        public void Generate()
        {
#if UNITY_EDITOR
            GenerateAsync().Forget(Debug.LogException);
#endif
        }

        public async UniTask GenerateAsync()
        {
            if (_isGenerating) return;
            _isGenerating = true;


            try
            {
                ValidateSettings();

                _nodePositions.Clear();
                Nodes = null;

#if UNITY_EDITOR
                _mapData?.ClearList();
#endif

                Debug.Log("=== Generate Start ===");

                //細分化
                foreach (Transform child in transform)
                {
                    var root = GetGroundPoint(child.position);
                    if (!IsOnNavMesh(root)) continue;

                    _nodePositions.Add(root);
                    await SubdivisionNode(root);
                }

                if (_nodePositions.Count == 0)
                {
                    Debug.LogWarning("生成対象ノードがありません。");
                    return;
                }

                //地面に移動
                for (int i = 0; i < _nodePositions.Count; i++)
                {
                    _nodePositions[i] = GetGroundPoint(_nodePositions[i]);

                    if ((i & 63) == 0)
                        await UniTask.Yield();
                }

                //近接ノード削除
                _nodePositions = RemoveCloseNodes(_nodePositions);

                if (_nodePositions.Count == 0)
                {
                    Debug.LogWarning("近距離削除後にノードが0件になりました。");
                    return;
                }


                //タイル化
                Vector3 min = Vector3.one * float.MaxValue;
                Vector3 max = Vector3.one * float.MinValue;

                foreach (var p in _nodePositions)
                {
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }

                Vector3 size = max - min;

                Vector3Int gridSize = new(
                    Mathf.Max(1, Mathf.CeilToInt(size.x / _connectDistance) + 1),
                    Mathf.Max(1, Mathf.CeilToInt(size.y / _connectDistance) + 1),
                    Mathf.Max(1, Mathf.CeilToInt(size.z / _connectDistance) + 1)
                );

                //NodeData化
                Nodes = new List<NodeData>[gridSize.x, gridSize.y, gridSize.z];

                _invCell = 1f / _connectDistance;
                _offset = -min;

                int index = 0;
                for (index = 0; index < _nodePositions.Count; index++)
                {
                    CreateNodeData(_nodePositions[index],index);
                }

                //飛び越え計算

                Dictionary<NodeData, Vector3> tryVaultResult = new();

                foreach (var cell in Nodes)
                {
                    if(cell == null) continue;
                    foreach(var node in cell)
                    {
                        foreach(var direction in directions)
                        {
                          if(!VaultCheck(node.Position,direction,out Vector3 endPos)) continue;

                          tryVaultResult.Add(node, endPos);
                        }
                    }
                }
                foreach(var data in tryVaultResult)
                {
                    index++;
                    var nodeData = CreateNodeData(data.Value, index);
                    if (nodeData == null) continue;
                    data.Key.SetVaultConnect(nodeData);
                }

                //接続を作る

                float maxDistSq = _connectDistance * _connectDistance;

                int sx = Nodes.GetLength(0);
                int sy = Nodes.GetLength(1);
                int sz = Nodes.GetLength(2);

                for (int x = 0; x < sx; x++)
                {
                    for (int y = 0; y < sy; y++)
                    {
                        for (int z = 0; z < sz; z++)
                        {
                            var cell = Nodes[x, y, z];
                            if (cell == null) continue;

                            foreach (var node in cell)
                            {
                                int currentCount = node.ConnectNode?.Count ?? 0;
                                if (currentCount >= _maxConnectCount) continue;

                                var candidates = new List<(NodeData other, float distSq)>(32);

                                for (int ox = -1; ox <= 1; ox++)
                                {
                                    for (int oy = -1; oy <= 1; oy++)
                                    {
                                        for (int oz = -1; oz <= 1; oz++)
                                        {
                                            int nx = x + ox;
                                            int ny = y + oy;
                                            int nz = z + oz;

                                            if (nx < 0 || ny < 0 || nz < 0 ||
                                                nx >= sx || ny >= sy || nz >= sz)
                                                continue;

                                            var neighbor = Nodes[nx, ny, nz];
                                            if (neighbor == null) continue;

                                            foreach (var other in neighbor)
                                            {
                                                if (ReferenceEquals(node, other)) continue;
                                                if (node.ConnectNode != null && node.ConnectNode.Contains(other)) continue;

                                                float distSq = (node.Position - other.Position).sqrMagnitude;
                                                if (distSq > maxDistSq) continue;

                                                candidates.Add((other, distSq));
                                            }
                                        }
                                    }
                                }

                                candidates.Sort((a, b) => a.distSq.CompareTo(b.distSq));

                                foreach (var (other, distSq) in candidates)
                                {
                                    if ((node.ConnectNode?.Count ?? 0) >= _maxConnectCount) break;
                                    if (other == null) continue;
                                    if (node.ConnectNode != null && node.ConnectNode.Contains(other)) continue;
                                    if (!HasValidPath(node.Position, other.Position)) continue;

                                    if (HasObstacle(node.Position, other.Position)) continue;

                                    float dist = Mathf.Sqrt(distSq);

                                    node.AddConnect(other);
                                    other.AddConnect(node);
                                }
                            }
                        }
                    }

                    if ((x & 1) == 0)
                        await UniTask.Yield();
                }

#if UNITY_EDITOR
                foreach (var nodeList in Nodes)
                {
                    if (nodeList == null) continue;

                    foreach (var node in nodeList)
                    {
                        _mapData?.AddNodeData(node);
                    }
                }

                if (_mapData != null)
                {
                    EditorUtility.SetDirty(_mapData);
                    AssetDatabase.SaveAssets();
                }
#endif


                Debug.Log("=== Generate Complete ===");
            }
            finally
            {
                _isGenerating = false;
            }
        }

        private NodeData CreateNodeData(Vector3 position,int index)
        {
            if (!HasValidPath(position, _origin.position)) return null;
            var node = new NodeData(position,index);
            var idx = WorldToIndex(position);

            Nodes[idx.x, idx.y, idx.z] ??= new List<NodeData>(2);
            Nodes[idx.x, idx.y, idx.z].Add(node);

            return node;
        }

        private void ValidateSettings()
        {
            if (_subdivisionDistance <= 0f)
                throw new ArgumentOutOfRangeException(nameof(_subdivisionDistance), "Subdivision Distance は 0 より大きくしてください。");

            if (_removeCloseDistance <= 0f)
                throw new ArgumentOutOfRangeException(nameof(_removeCloseDistance), "Remove Close Distance は 0 より大きくしてください。");

            if (_connectDistance <= 0f)
                throw new ArgumentOutOfRangeException(nameof(_connectDistance), "Connect Distance は 0 より大きくしてください。");

            if (_positionQuantize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(_positionQuantize), "Position Quantize は 0 より大きくしてください。");
        }

        private async UniTask SubdivisionNode(Vector3 start)
        {
            Queue<Vector3> queue = new();
            HashSet<Vector3Int> visited = new();

            Vector3Int ToKey(Vector3 v)
            {
                return new Vector3Int(
                    Mathf.RoundToInt(v.x * _positionQuantize),
                    Mathf.RoundToInt(v.y * _positionQuantize),
                    Mathf.RoundToInt(v.z * _positionQuantize)
                );
            }

            queue.Enqueue(start);
            visited.Add(ToKey(start));

            int depth = 0;

            while (queue.Count > 0 && depth < _subdivisionHierarchy)
            {
                int count = queue.Count;

                for (int i = 0; i < count; i++)
                {
                    var current = queue.Dequeue();

                    foreach (var dir in directions)
                    {
                        var next = current + dir * _subdivisionDistance;
                        var key = ToKey(next);

                        if (visited.Contains(key)) continue;
                        if (!IsOnNavMesh(next)) continue;

                        visited.Add(key);
                        _nodePositions.Add(next);
                        queue.Enqueue(next);
                    }

                    if ((i & 31) == 0)
                        await UniTask.Yield();
                }

                depth++;
            }
        }
        private bool VaultCheck(Vector3 position ,Vector3 direction,out Vector3 endPos)
        {
            bool isVault = VaultChecker.TryVault(new VaultParameter
            {
                Position = position,
                moveDirection = direction,

                capsuleRadius = _botCapsuleCollider.radius,
                capsuleHeight = _botCapsuleCollider.height,

                reachDistance = _reachDistance,

                maxLedgeHeight = _maxLedgeHeight,
                minLedgeHeight = _minLedgeHeight,
                maxLedgeDepth = _maxLedgeDepth,

                groundSlopeThreshold = _groundSlopeThreshold,
                groundLayer = _groundLayer
            }, out var result);

            endPos = result.vaultEnd;

            return isVault;
        }
        private List<Vector3> RemoveCloseNodes(List<Vector3> nodes)
        {
            if (nodes == null || nodes.Count == 0) return new List<Vector3>();

            float cell = _removeCloseDistance;
            Dictionary<Vector3Int, Vector3> grid = new();
            List<Vector3> result = new();

            foreach (var pos in nodes)
            {
                var key = new Vector3Int(
                    Mathf.FloorToInt(pos.x / cell),
                    Mathf.FloorToInt(pos.y / cell),
                    Mathf.FloorToInt(pos.z / cell)
                );

                if (grid.ContainsKey(key)) continue;

                grid[key] = pos;
                result.Add(pos);
            }

            return result;
        }

        private bool HasObstacle(Vector3 from, Vector3 to)
        {
            from += Vector3.up * _rayUpAmount;
            to += Vector3.up * _rayUpAmount;

            Vector3 dir = to - from;
            float distance = dir.magnitude;

            if (distance <= 0.0001f) return false;

            return Physics.Raycast(
                from,
                dir / distance,
                distance,
                _obstacleMask,
                QueryTriggerInteraction.Ignore
            );
        }

        private bool IsOnNavMesh(Vector3 pos)
        {
            return NavMesh.SamplePosition(
                pos,
                out _,
                _navMeshTolerance,
                NavMesh.AllAreas
            );
        }

        private Vector3 GetGroundPoint(Vector3 pos)
        {
            Ray ray = new(pos + Vector3.up * _groundRayUp, Vector3.down);

            if (Physics.Raycast(ray, out var hit, _rayDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return pos;
        }

        private Vector3Int WorldToIndex(Vector3 pos)
        {
            int ix = Mathf.FloorToInt((pos.x + _offset.x) * _invCell);
            int iy = Mathf.FloorToInt((pos.y + _offset.y) * _invCell);
            int iz = Mathf.FloorToInt((pos.z + _offset.z) * _invCell);

            ix = Mathf.Clamp(ix, 0, Nodes.GetLength(0) - 1);
            iy = Mathf.Clamp(iy, 0, Nodes.GetLength(1) - 1);
            iz = Mathf.Clamp(iz, 0, Nodes.GetLength(2) - 1);

            return new Vector3Int(ix, iy, iz);
        }

        private bool HasValidPath(Vector3 from, Vector3 to)
        {
            _path ??= new NavMeshPath();

            if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _path))
            {
                return _path.status == NavMeshPathStatus.PathComplete;
            }
            return false;
        }
        private void OnDrawGizmos()
        {
            if (!_isDrawGizumoIcon || Nodes == null) return;

            var result = Nodes
           .Cast<List<NodeData>>()
           .Where(l => l != null)
           .SelectMany(l => l)
           .ToList();

            DrawGizmos(result);
        }

        public static void DrawGizmos(List<NodeData> nodes)
        {
           

            var offset = Vector3.up * 0.3f;
            var cam = Camera.current;

            if (cam == null) return;
            var camPos = cam.transform.position;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null) continue;

                // カメラから遠いノードはスキップ
                if ((node.Position - camPos).sqrMagnitude > 400f) continue;

                var pos = node.Position + offset;

                var connects = node.ConnectNode;
                if (connects == null) continue;

                for (int j = 0; j < connects.Count; j++)
                {
                    var connect = connects[j];
                    if (connect == null) continue;

                    // 重複描画防止（超重要）
                    if (node.GetHashCode() > connect.GetHashCode()) continue;
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(
                        pos,
                        connect.Position + offset
                    );
                    if (node.VaultConnect == null) continue;
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(
                        pos,
                        node.VaultConnect.Position
                    );

                }
            }
        }
    }
}