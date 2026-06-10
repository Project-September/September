using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>ビルドシステムの計算方法を定義した列挙型</summary>
    public enum BuildCalculateType
    {
        [InspectorName("加算")] Add,
        [InspectorName("倍率")] Multiply
    }
}
