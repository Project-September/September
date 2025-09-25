using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[DisallowMultipleComponent]
public class SplineMirrorTool : MonoBehaviour
{
    public SplineContainer Container;          // 未設定なら同じGOから自動取得
    [Min(0)] public int SourceSplineIndex = 0; // 複製元

    public MirrorMode Mode = MirrorMode.Plane;

    [Header("Reference (Plane origin / Axis origin)")]
    public Transform Reference;                // 基準点（position を使う）

    [Header("Axis for plane normal or line direction")]
    public AxisType Axis = AxisType.Up;                // Up/Forward/Right のどれを使うか

    [Header("After duplicate, move in world space")]
    public Vector3 WorldOffset;

    [Header("Plane options")]
    public bool MirrorOrientation = true;      // 向きも対称にする

    public enum MirrorMode { Plane, AxisHalfTurn }
    public enum AxisType { Up, Forward, Right }

    public SplineContainer GetContainerOrSelf()
        => Container != null ? Container : GetComponent<SplineContainer>();

    public Vector3 GetAxisDir()
    {
        var t = Reference != null ? Reference : transform;
        return Axis switch
        {
            AxisType.Up      => t.up,
            AxisType.Forward => t.forward,
            AxisType.Right   => t.right,
            _ => t.up
        };
    }

    public Vector3 GetRefPoint()
        => Reference != null ? Reference.position : transform.position;
}

public static class SplineMirrorOps
{
    // 平面鏡映：位置のみ対称（回転はそのまま）
    public static int MirrorDuplicateAcrossPlane(
        SplineContainer container, int srcIndex,
        Vector3 planePoint, Vector3 planeNormal,
        Vector3 worldOffset)
    {
        var src = container[srcIndex];

        var clone = new Spline();
        clone.Copy(src);
        container.AddSpline(clone);

        int dstIndex = container.Splines.Count - 1;
        var dst = container[dstIndex];

        var n = planeNormal.normalized;

        for (int i = 0; i < dst.Count; i++)
        {
            var k = dst[i];

            Vector3 wp   = container.transform.TransformPoint(k.Position);
            Vector3 win  = wp + container.transform.TransformVector(k.TangentIn);
            Vector3 wout = wp + container.transform.TransformVector(k.TangentOut);

            Vector3 mp   = ReflectPoint(wp,  planePoint, n);
            Vector3 min  = ReflectPoint(win, planePoint, n);
            Vector3 mout = ReflectPoint(wout,planePoint, n);

            mp   += worldOffset;
            min  += worldOffset;
            mout += worldOffset;

            Vector3 lp   = container.transform.InverseTransformPoint(mp);
            Vector3 lin  = container.transform.InverseTransformPoint(min)  - lp;
            Vector3 lout = container.transform.InverseTransformPoint(mout) - lp;

            k.Position   = lp;
            k.TangentIn  = lin;
            k.TangentOut = lout;

            // 回転はそのまま：位置だけ対称
            dst.SetKnot(i, k);
        }

        SplineUtility.CopyKnotLinks(container, srcIndex, dstIndex);
        return dstIndex;
    }

    // 平面鏡映：位置も向きも対称（右手系に補正して“鏡っぽい向き”にする）
    public static int MirrorDuplicateAcrossPlaneSymmetric(
        SplineContainer container, int srcIndex,
        Vector3 planePoint, Vector3 planeNormal,
        Vector3 worldOffset)
    {
        var src = container[srcIndex];

        var clone = new Spline();
        clone.Copy(src);
        container.AddSpline(clone);

        int dstIndex = container.Splines.Count - 1;
        var dst = container[dstIndex];

        var n = planeNormal.normalized;

        for (int i = 0; i < dst.Count; i++)
        {
            var k = dst[i];

            Vector3 wp   = container.transform.TransformPoint(k.Position);
            Vector3 win  = wp + container.transform.TransformVector(k.TangentIn);
            Vector3 wout = wp + container.transform.TransformVector(k.TangentOut);

            Vector3 mp   = ReflectPoint(wp,  planePoint, n);
            Vector3 min  = ReflectPoint(win, planePoint, n);
            Vector3 mout = ReflectPoint(wout,planePoint, n);

            mp   += worldOffset;
            min  += worldOffset;
            mout += worldOffset;

            Vector3 lp   = container.transform.InverseTransformPoint(mp);
            Vector3 lin  = container.transform.InverseTransformPoint(min)  - lp;
            Vector3 lout = container.transform.InverseTransformPoint(mout) - lp;

            k.Position   = lp;
            k.TangentIn  = lin;
            k.TangentOut = lout;

            // 回転を“鏡映の見た目”にする
            var f = math.mul(k.Rotation, Vector3.forward);
            var u = math.mul(k.Rotation, Vector3.up);

            var mf = ReflectVector(f, n);
            var mu = ReflectVector(u, n);

            // 鏡映は左手系になるので up を反転して右手系へ戻す
            var muFixed = -mu;
            k.Rotation = Quaternion.LookRotation(mf, muFixed);

            dst.SetKnot(i, k);
        }

        SplineUtility.CopyKnotLinks(container, srcIndex, dstIndex);
        return dstIndex;
    }

    // 任意軸の線対称（= 180度回転）複製
    public static int HalfTurnDuplicateAroundLine(
        SplineContainer container, int srcIndex,
        Vector3 axisPoint, Vector3 axisDir,
        Vector3 worldOffset)
    {
        var src = container[srcIndex];

        var clone = new Spline();
        clone.Copy(src);
        container.AddSpline(clone);

        int dstIndex = container.Splines.Count - 1;
        var dst = container[dstIndex];

        var q = Quaternion.AngleAxis(180f, axisDir.normalized);

        for (int i = 0; i < dst.Count; i++)
        {
            var k = dst[i];

            Vector3 wp   = container.transform.TransformPoint(k.Position);
            Vector3 win  = wp + container.transform.TransformVector(k.TangentIn);
            Vector3 wout = wp + container.transform.TransformVector(k.TangentOut);

            Vector3 rp   = axisPoint + q * (wp   - axisPoint);
            Vector3 rin  = axisPoint + q * (win  - axisPoint);
            Vector3 rout = axisPoint + q * (wout - axisPoint);

            rp   += worldOffset;
            rin  += worldOffset;
            rout += worldOffset;

            Vector3 lp   = container.transform.InverseTransformPoint(rp);
            Vector3 lin  = container.transform.InverseTransformPoint(rin)  - lp;
            Vector3 lout = container.transform.InverseTransformPoint(rout) - lp;

            k.Position   = lp;
            k.TangentIn  = lin;
            k.TangentOut = lout;
            k.Rotation   = q * k.Rotation;

            dst.SetKnot(i, k);
        }

        SplineUtility.CopyKnotLinks(container, srcIndex, dstIndex);
        return dstIndex;
    }

    static Vector3 ReflectPoint(Vector3 p, Vector3 p0, Vector3 n)
    {
        float d = Vector3.Dot(p - p0, n);
        return p - 2f * d * n;
    }

    static Vector3 ReflectVector(Vector3 v, Vector3 n)
    {
        return v - 2f * Vector3.Dot(v, n) * n;
    }
}