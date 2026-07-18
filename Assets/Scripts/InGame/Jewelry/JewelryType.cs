using UnityEngine;

namespace InGame.Jewelry.Common
{
    /// <summary>宝石の種類を定義した列挙型</summary>
    public enum JewelryType : byte
    {
        // ====================================================
        // 配列の要素指定に使用するため内部番号を編集しないこと
        // ====================================================
        [InspectorName("デカい宝石")] BigGem,
        [InspectorName("良い感じの宝石")] BetterGem,

        [InspectorName("Inspectorから選択禁止")] Count   // 配列の要素数用の列挙子
    }
}
