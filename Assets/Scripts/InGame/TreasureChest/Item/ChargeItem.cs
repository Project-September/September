using Fusion;
using September.Common;
using UnityEngine;

namespace InGame.TreasureChest
{
    public class ChargeItem : ItemBase
    {
        protected override void OnHitPlayer(GameObject player)
        {
            if (player.TryGetComponent(out NetworkObject networkObject))
            {
                //スコア増加が難しそうだったので一旦モアイをインタラクトしたことにする
                PlayerDatabase.Instance.Server_AddExhibit(networkObject.InputAuthority, Result.ExhibitType.Moai);
            }
        }
    }
}
