using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Jewelry.Common;
using UnityEngine;

namespace InGame.Jewelry
{
    public class Jewelry : NetworkBehaviour, IJewelry
    {
        [Header("この宝石のパラメータ群"), SerializeField] JewelryInfo _jewelryParams;
        [SerializeField] JewelryControl _jewelryControl;

        public JewelryInfo JewelryParams => _jewelryParams;
        public JewelryControl JewelryControl => _jewelryControl;

        public async UniTask PickupFrom(PlayerRef player)
        {
            Debug.Log("[Jewelry] PickupFrom");

            var obj = Runner.GetPlayerObject(player);
            var container = obj.GetComponentInChildren<IJewelryContainer>();

            if (container == null) return;

            await _jewelryControl.PlayGetMove(obj.transform);

            container.PickUp(this);
        }
    }
}
