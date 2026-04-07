using System.Collections.Generic;
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
        RemoteInteraction,
        [InspectorName("ロケットランチャー")]
        RocketLauncher,
        [InspectorName("何も選択していない状態")]
        None
    }
    
    /// <summary>
    /// AbilityStatusの名前を管理
    /// </summary>
    public class HatanoAbilityStatusNameManager
    {
        /// <summary>
        /// 各Abilityの名前を保持
        /// </summary>
        public Dictionary<HatanoAbilityStatus, string> abilityStatusNames{get; private set;}

        public HatanoAbilityStatusNameManager()
        {
            abilityStatusNames = new Dictionary<HatanoAbilityStatus, string>()
            {
                {HatanoAbilityStatus.DoubleBarreledGun, "2丁銃" },
                {HatanoAbilityStatus.RemoteInteraction, "遠距離インタラクション"},
                {HatanoAbilityStatus.RocketLauncher, "ロケットランチャー"},
                {HatanoAbilityStatus.None, "装備なし"}
            };
        }
    }
}
