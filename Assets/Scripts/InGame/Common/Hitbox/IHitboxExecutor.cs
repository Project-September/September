using System;
using UnityEngine;

public interface IHitboxExecutor
{
    void Tick(float deltaTime);
#if UNITY_EDITOR
    void DebugCapsuleGizmo(Vector3 start, Vector3 end);
#endif
}