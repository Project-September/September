using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>ビルドルートの種類を定義した列挙型</summary>
    public enum BuildRouteType
    {
        [InspectorName("攻撃力上昇（回）")] AttackPower,
        [InspectorName("移動速度上昇（m）")] MoveSpeed,
        [InspectorName("気絶耐性（回）")] StunResistance,
        [InspectorName("インタラクト速度上昇（回）")] FastInteract
    }
}
