using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeGenerator : MonoBehaviour
{
    [SerializeField] private Transform _origin;
    [SerializeField] private Transform[] _streetWayPoint;
    [SerializeField] private bool _isDrawGizumoIcon;
    [SerializeField] private int _subdivisionCount;
    [SerializeField] private int _subdivisionHierarchy;
    [SerializeField] private int _subdivisionDistance;
    [SerializeField] private float _overhangTolerance;
    [SerializeField] private float _removeCloseDistanse;
    [SerializeField] private float _rayDistanse;
    private List<Vector3> _nodePositon;

    [Button("ÉmÅ[ÉhÇê∂ê¨")]
    public void Generate()
    {
        GenerateAsync().Forget();
    }

    private async UniTask GenerateAsync()
    {
        _nodePositon.Clear();

        Debug.Log("CreateNode...");
        foreach (var point in _streetWayPoint)
        {
            if (!IsReachable(_origin.position, point.position) || !IsOnNavMesh(point.position)) continue;

            _nodePositon.Add(point.position);
            await SubdivisionNode(point.position);

            await UniTask.DelayFrame(1);
        }

        Debug.Log("MoveGround...");
        for (int i = 0; i < _nodePositon.Count; i++)
        {
            _nodePositon[i] = GetHitPoint(_nodePositon[i]);
            await UniTask.DelayFrame(1);
        }

        Debug.Log("RemoveClose...");
        List<Vector3> removeNode = new();

        foreach (var a in _nodePositon)
        {
            foreach (var b in _nodePositon)
            {
                if (a == b || removeNode.Contains(a) || removeNode.Contains(b)) continue;

                if (Vector3.Distance(a, b) <= _removeCloseDistanse)
                {
                    removeNode.Add(a);
                }
            }
            await UniTask.DelayFrame(1);
        }

        foreach (var node in removeNode)
        {
            _nodePositon.Remove(node);
        }


        Debug.Log("====All Complete====");
    }

    private async UniTask SubdivisionNode(Vector3 position)
    {
        for (int i = 1; i <= _subdivisionHierarchy; i++)
        {
            foreach (var subPosition in GetCirclePointsXZ(_subdivisionDistance * i, _subdivisionCount, position))
            {
                if (!IsReachable(position, subPosition) || !IsOnNavMesh(position)) continue;

                _nodePositon.Add(subPosition);

                await UniTask.DelayFrame(1);
            }
        }
    }

    private List<Vector3> GetCirclePointsXZ(float r, int n, Vector3 center)
    {
        var points = new List<Vector3>();

        for (int k = 0; k < n; k++)
        {
            float angle = 2f * Mathf.PI * k / n;

            Vector3 p = center + new Vector3(
                Mathf.Cos(angle) * r,
                0f,
                Mathf.Sin(angle) * r
            );

            points.Add(p);
        }

        return points;
    }

    private bool IsReachable(Vector3 from, Vector3 to)
    {
        var path = new UnityEngine.AI.NavMeshPath();

        bool found = UnityEngine.AI.NavMesh.CalculatePath(
            from,
            to,
            UnityEngine.AI.NavMesh.AllAreas,
            path
        );

        return found && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
    }

    private bool IsOnNavMesh(Vector3 pos)
    {
        return UnityEngine.AI.NavMesh.SamplePosition(
            pos,
            out UnityEngine.AI.NavMeshHit hit,
            _overhangTolerance,
            UnityEngine.AI.NavMesh.AllAreas
        );
    }

    private Vector3 GetHitPoint(Vector3 pos)
    {
        Ray ray = new Ray(pos, -Vector3.up);
        if (Physics.Raycast(ray, out var hit, _rayDistanse))
        {
            return hit.point;
        }
        return pos;
    }

    public void OnDrawGizmos()
    {
        if (!_isDrawGizumoIcon) return;
        foreach (var node in _nodePositon)
        {
            Gizmos.DrawSphere(node, 0.2f);
        }
    }
}
