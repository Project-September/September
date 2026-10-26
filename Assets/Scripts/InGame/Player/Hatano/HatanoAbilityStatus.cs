using UnityEngine;

namespace InGame.Player.Hatano
{
    /// <summary>
    /// ハタノのAbilityの種類
    /// </summary>
    public enum HatanoAbilityStatus
    {
        [InspectorName("２丁銃")]
        DoubleBarreledGun,
        [InspectorName("遠距離インタラクションAbility")]
        LaserGun,
        [InspectorName("ロケットランチャー")]
        RocketLauncher,
        [InspectorName("何も選択していない状態")]
        None
    }
}
