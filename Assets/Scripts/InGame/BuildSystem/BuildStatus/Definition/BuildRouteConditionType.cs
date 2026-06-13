using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>ビルド可能かを定義した列挙型</summary>
    public enum BuildRouteConditionType
    {
        [InspectorName("ビルド可能")] CanBuild,
        [InspectorName("最大ビルド状態")] MaxBuild
    }
}
