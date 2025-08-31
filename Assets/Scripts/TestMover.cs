using System;
using Cinemachine;
using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using InGame.Player;
using September.Common;
using UnityEngine;
using UnityEngine.UI;

public class TestMover : NetworkTRSP
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] float moveSpeed = 6f;   // 目標水平速度 [m/s]
    [SerializeField] float accel     = 40f;  // 加速・減速 [m/s^2]（大きいほどキビキビ）
    [SerializeField] CameraController _cameraController;
    private PlayerInput _input;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            _cameraController = GetComponent<CameraController>();
            _cameraController.Init(this);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput<PlayerInput>(out var input)) return;
        if (!Object.HasStateAuthority) return; // 物理は通常State Authorityのみ

        Vector2 md = GetMoveDirection(input.MoveDirection, input.CameraYaw);
        if (md.sqrMagnitude > 1f) md.Normalize();
        if (md.sqrMagnitude < 0.0004f) md = Vector2.zero;

        Vector3 targetHoriz = new Vector3(md.x, 0f, md.y) * moveSpeed;

        float dt = Runner.DeltaTime;
        Vector3 v = _rb.linearVelocity;
        Vector3 curHoriz = new Vector3(v.x, 0f, v.z);
        Vector3 nextHoriz = Vector3.MoveTowards(curHoriz, targetHoriz, accel * dt);

        v.x = nextHoriz.x;
        v.z = nextHoriz.z; // v.y は重力任せ
        _rb.linearVelocity = v;

        // 任意：移動方向へキャラを緩やかに回す
        if (nextHoriz.sqrMagnitude > 0.0001f)
        {
            Quaternion to = Quaternion.LookRotation(nextHoriz.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, to, 12f * dt);
        }
        
    }

    private Vector2 GetMoveDirection(Vector2 moveInput, float cameraYaw)
    {
        float rad = -cameraYaw * Mathf.Deg2Rad;
        return new Vector2(
            moveInput.x * Mathf.Cos(rad) - moveInput.y * Mathf.Sin(rad),
            moveInput.x * Mathf.Sin(rad) + moveInput.y * Mathf.Cos(rad)
        );
    }
}
