using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;

namespace InGame.Player
{
    public static class VaultChecker
    {
        public static bool TryVault(VaultParameter parameter, out VaultResult result)
        {
            result = new();
            result.vaultStart = Vector3.zero;
            result.vaultTop = Vector3.zero;
            result.vaultEnd = Vector3.zero;

            Vector3 moveDirection = parameter.moveDirection;
            Vector3 position = parameter.Position;

            Vector3 moveDir3 = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;

            // --- ステップ1 ---
            Vector3 point1 = position + Vector3.up * (parameter.maxLedgeHeight - parameter.capsuleRadius) - moveDir3 * 0.01f;
            Vector3 point2 = position + Vector3.up * (parameter.minLedgeHeight + parameter.capsuleRadius) - moveDir3 * 0.01f;

            if (!Physics.CapsuleCast(point1, point2, parameter.capsuleRadius, moveDir3,
                out var frontHitInfo, parameter.reachDistance, parameter.groundLayer))
                return false;

            bool walkable = Vector3.Angle(Vector3.up, frontHitInfo.normal) <= parameter.groundSlopeThreshold;
            if (walkable) return false;

            // --- ステップ2 ---
            Vector3 origin = frontHitInfo.point - frontHitInfo.normal * 0.3f;
            origin.y = position.y + parameter.maxLedgeHeight + parameter.capsuleRadius;

            if (!Physics.SphereCast(origin, parameter.capsuleRadius, Vector3.down,
                out var heightHitInfo, parameter.maxLedgeHeight - parameter.minLedgeHeight, parameter.groundLayer))
                return false;

            float ledgeHeight = heightHitInfo.point.y - position.y;

            if (ledgeHeight > parameter.maxLedgeHeight || ledgeHeight < parameter.minLedgeHeight)
                return false;

            // 上にスペースあるか
            if (Physics.CheckCapsule(
                heightHitInfo.point + heightHitInfo.normal * parameter.capsuleRadius,
                heightHitInfo.point + Vector3.up * (parameter.capsuleRadius + parameter.capsuleHeight),
                parameter.capsuleRadius - 0.01f,
                parameter.groundLayer))
                return false;

            // --- ステップ3 ---
            float halfHeight = parameter.capsuleHeight * 0.5f;

            Vector3 p1 = frontHitInfo.point;
            p1.y = position.y + parameter.capsuleRadius + parameter.capsuleHeight;

            Vector3 p2 = frontHitInfo.point;
            p2.y = position.y + parameter.capsuleRadius;

            if (Physics.CapsuleCast(p1, p2, parameter.capsuleRadius,
                -frontHitInfo.normal,
                out var secondHit,
                parameter.maxLedgeDepth + frontHitInfo.distance,
                parameter.groundLayer))
                return false;

            Vector3 reverseOrigin = p2 + Vector3.up * halfHeight
                - frontHitInfo.normal * (parameter.maxLedgeDepth + frontHitInfo.distance);

            if (!Physics.CapsuleCast(
                reverseOrigin + Vector3.up * halfHeight,
                reverseOrigin + Vector3.down * halfHeight,
                parameter.capsuleRadius,
                frontHitInfo.normal,
                out var backHit,
               parameter.maxLedgeDepth + frontHitInfo.distance,
                parameter.groundLayer))
                return false;

            if (backHit.distance < frontHitInfo.distance)
                return false;

            result.vaultEnd =
                reverseOrigin
                - frontHitInfo.normal * (frontHitInfo.distance)
                + Vector3.down * (halfHeight + parameter.capsuleRadius);

            if (Physics.CheckCapsule(
                result.vaultEnd + Vector3.up * parameter.capsuleRadius,
                result.vaultEnd + Vector3.up * (parameter.capsuleRadius + parameter.capsuleHeight),
                parameter.capsuleRadius - 0.01f,
                parameter.groundLayer))
                return false;

            result.vaultStart = position;
            result.vaultTop = (frontHitInfo.point + backHit.point) * 0.5f;
            result.vaultTop.y = heightHitInfo.point.y;

            return true;
        }
    }
    public ref struct VaultParameter
    {
        public Vector2 moveDirection;
        public Vector3 Position;
        public float capsuleRadius;
        public float capsuleHeight;
        public float reachDistance;
        public float maxLedgeHeight;
        public float minLedgeHeight;
        public float maxLedgeDepth;
        public float groundSlopeThreshold;
        public LayerMask groundLayer;
    }
    public ref struct VaultResult
    {
        public Vector3 vaultStart;
        public Vector3 vaultTop;
        public Vector3 vaultEnd;

    }
    public struct CapsuleCastData
    {
        public Vector3 P1;
        public Vector3 P2;
        public float Radius;
        public Vector3 Direction;
        public Vector3 Distance;
        public bool IsHit;
        public RaycastHit HitInfo;
    }
}
