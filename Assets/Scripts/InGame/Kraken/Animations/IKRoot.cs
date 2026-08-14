using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace September.InGame.Kraken.Animations
{
    public class IKRoot : MonoBehaviour
    {
        [SerializeField] private float _radius;
        [SerializeField] private Transform _target;
        [SerializeField] private Transform _rootRef;
        [SerializeField] private Transform _rootRefOrigin;
        [SerializeField] private float _originRadius;
        [SerializeField] private Transform[] _ikRoot;

        private void OnDrawGizmos()
        {
            Handles.CircleHandleCap(0, transform.position, Quaternion.Euler(90f, 0f, 0f), _radius, EventType.Repaint);

            {
                if (_target == null) return;

                var x0 = transform.position.x;
                var y0 = transform.position.z;
                var x1 = _target.position.x;
                var y1 = _target.position.z;
                var r = _radius;

                var a = x1 - x0;
                var b = y1 - y0;
                var a2 = a * a;
                var b2 = b * b;
                var r2 = r * r;

                var ax = r * ((a * r - b * Mathf.Sqrt(a2 + b2 - r2)) / (a2 + b2)) + x0;
                var ay = r * ((b * r + a * Mathf.Sqrt(a2 + b2 - r2)) / (a2 + b2)) + y0;

                var bx = r * ((a * r + b * Mathf.Sqrt(a2 + b2 - r2)) / (a2 + b2)) + x0;
                var by = r * ((b * r - a * Mathf.Sqrt(a2 + b2 - r2)) / (a2 + b2)) + y0;

                var pa = new Vector3(ax, transform.position.y, ay);
                var pb = new Vector3(bx, transform.position.y, by);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_target.position, pa);
                Gizmos.DrawWireSphere(pa, 1f);

                Gizmos.DrawLine(_target.position, pb);
                Gizmos.DrawWireSphere(pb, 1f);
            }

            if (_rootRef == null) return;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_rootRef.position, 2f);

            if (_rootRefOrigin != null)
            {
                Handles.color = Color.cyan;
                Handles.CircleHandleCap(1, _rootRefOrigin.position, Quaternion.Euler(90f, 0f, 0f), _originRadius, EventType.Repaint);

                var o = new Vector2(_rootRefOrigin.position.x, _rootRefOrigin.position.z);
                var t = new Vector2(_target.position.x, _target.position.z);
                var circle = new Circle2D(o, _originRadius);
                var line = new Line2D(o, t);

                var p = AcrossPointLineToCircle(circle, line);
                var P = new Vector3(p.x, transform.position.y, p.y);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(P, 2f);
                _rootRef.transform.position = P;
            }

            if (_ikRoot == null) return;

            foreach (var i in _ikRoot)
            {
                if (i == null) continue;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(i.position, 2f);

                var a = new Vector2(i.position.x, i.position.z);
                var b = new Vector2(_rootRef.position.x, _rootRef.position.z);
                var c = new Vector2(transform.position.x, transform.position.z);

                var line = new Line2D(a, b);
                var circle = new Circle2D(c, _radius);

                var p = AcrossPointLineToCircle(circle, line);

                var o = transform.position;

                Gizmos.DrawWireSphere(new Vector3(p.x, o.y, p.y), 1f);
                Gizmos.DrawLine(new Vector3(b.x, o.y, b.y), new Vector3(p.x, o.y, p.y));
            }
        }

        private Vector2 AcrossPointLineToCircle(Circle2D circle, Line2D line)
        {
            line.GetGeneralNormalized(out float a, out float b, out float c);

            var x0 = circle.Center.x;
            var y0 = circle.Center.y;

            var d = -(a * x0 + b * y0 + c);

            var r = circle.Radius;

            var sqrtR2MinusD2 = Mathf.Sqrt(r * r - d * d);

            var px = a * d - b * sqrtR2MinusD2 + x0;
            var py = b * d + a * sqrtR2MinusD2 + y0;

            return new Vector2(px, py);
        }
    }

    public struct Circle2D
    {
        public readonly Vector2 Center;
        public readonly float Radius;

        public Circle2D(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }

    public struct Line2D
    {
        // ax + by + c = 0
        private readonly float A;
        private readonly float B;
        private readonly float C;

        public Line2D(Vector2 a, Vector2 b)
        {
            A = a.y - b.y;
            B = b.x - a.x;
            C = a.x * b.y - b.x * a.y;

            float magnitude = Mathf.Sqrt(A * A + B * B);

            A /= magnitude;
            B /= magnitude;
            C /= magnitude;
        }

        public Line2D(float a, float b, float c)
        {
            A = a;
            B = b;
            C = c;
        }

        public void GetGeneralNormalized(out float a, out float b, out float c)
        {
            a = A;
            b = B;
            c = C;
        }
    }
}
