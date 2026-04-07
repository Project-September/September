using UnityEngine;

/// <summary>
/// 射撃のステート
/// </summary>
public enum ShootingStateType
{
    [InspectorName("何もしていない")]
    None,
    [InspectorName("構え")]
    Stance,
    [InspectorName("射撃")]
    Shooting
}
