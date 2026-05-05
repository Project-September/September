using UnityEngine;

namespace September.Lobby
{
    /// <summary>ビルドシステム種類を定義する列挙型</summary>
    public enum BuildType
    {
        [InspectorName("該当なし")] None,
        [InspectorName("攻撃力")] AttackPower,
        [InspectorName("移動速度")] MoveSpeed,
        [InspectorName("気絶耐性")] StunResistance,
        [InspectorName("高速インタラクト")] FastInteract
    }
}
