using UnityEngine;

namespace InGame.Jewelry.Common
{
    /// <summary>宝石の種類を定義した列挙型</summary>
    public enum JewelryType : byte
    {
        // ====================================================
        // 配列の要素指定に使用するため内部番号を編集しないこと
        // ====================================================
        [InspectorName("普通の宝石")] NormalGem,
        [InspectorName("デカい宝石")] BigGem,

        [InspectorName("Inspectorから選択禁止項目")] JewelryTypeCount   // 配列の要素数用の列挙子
    }
}
