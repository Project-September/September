using System.Collections.Generic;
using RootMotion.FinalIK;
using UnityEngine;

namespace September.InGame.Kraken
{
    public class ForcedTentacle : MonoBehaviour
    {
        [SerializeField] private FABRIK _root;
        [SerializeField] private float _radius = 0.5f;
        [SerializeField] private LayerMask _layerMask;

        private List<PointInfo> _oldPoints = new();
        private Collider[] _selfColliders;

        private void Start()
        {
            _root.solver.OnPostUpdate += SolveColliders;

            foreach (var p in _root.solver.GetPoints())
            {
                _oldPoints.Add(new PointInfo(p));
            }

            _selfColliders = GetComponentsInChildren<Collider>();
        }

        private void SolveColliders()
        {
            var points = _root.solver.GetPoints();
            for (int i = 0; i < Mathf.Min(points.Length, _oldPoints.Count); i++)
            {
                var p = points[i].solverPosition;
                var o = _oldPoints[i].Position;

                var dir = p - o;

                if (Physics.SphereCast(o - dir * .1f, _radius, dir, out var hit, dir.magnitude, _layerMask))
                {
                    var solvedPosition = hit.point + hit.normal * _radius;
                    points[i].transform.position = solvedPosition;
                    points[i].UpdateSolverPosition();

                    Debug.DrawLine(o, solvedPosition, Color.red);
                    DebugDrawUtility.DrawWireSphere(p, _radius, Color.blue);
                    DebugDrawUtility.DrawWireSphere(o, _radius, Color.green);
                    DebugDrawUtility.DrawWireSphere(solvedPosition, _radius, Color.red);
                }
            }

            _oldPoints.Clear();
            foreach (var point in _root.solver.GetPoints())
            {
                _oldPoints.Add(new PointInfo(point));
            }
        }

        private struct PointInfo
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public PointInfo(IKSolver.Point point)
            {
                Position = point.transform.position;
                Rotation = point.transform.rotation;
            }
        }
    }
}
