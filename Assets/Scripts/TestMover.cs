using Cinemachine;
using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using September.Common;
using UnityEngine;
using UnityEngine.UI;

public class TestMover : NetworkBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] float moveSpeed = 6f;   // 目標水平速度 [m/s]
    [SerializeField] float accel     = 40f;  // 加速・減速 [m/s^2]（大きいほどキビキビ）

    public override void FixedUpdateNetwork()
    {
        if (!GetInput<PlayerInput>(out var input)) return;

        // 権威 or ローカル予測のときだけ本体を動かす（プロキシは触らない）
        if (!(Object.HasStateAuthority || (Object.HasInputAuthority && Runner.IsForward)))
            return;

        // カメラ相対入力 → ワールドXZ
        Vector2 md = GetMoveDirection(input.MoveDirection, input.CameraYaw);
        if (md.sqrMagnitude > 1f) md.Normalize();            // 斜め補正
        if (md.sqrMagnitude < 0.0004f) md = Vector2.zero;    // デッドゾーン（微振れ防止）

        Vector3 targetHoriz = new Vector3(md.x, 0f, md.y) * moveSpeed;

        // ここがポイント：現在の水平速度を target へ“加速度制限”で寄せる
        float dt = Runner.DeltaTime;
        Vector3 v = _rb.linearVelocity;

        Vector3 curHoriz = new Vector3(v.x, 0f, v.z);
        Vector3 nextHoriz = Vector3.MoveTowards(curHoriz, targetHoriz, accel * dt);

        v.x = nextHoriz.x;
        v.z = nextHoriz.z; // v.y はそのまま（重力を活かすなら）
        _rb.linearVelocity = v;
    }

    
    Vector2 GetMoveDirection(Vector2 moveInput, float cameraYaw)
    {
        float radYaw = -cameraYaw * Mathf.Deg2Rad;
        return new Vector2(
            moveInput.x * Mathf.Cos(radYaw) - moveInput.y * Mathf.Sin(radYaw),
            moveInput.x * Mathf.Sin(radYaw) + moveInput.y * Mathf.Cos(radYaw)
        );
    }
}
