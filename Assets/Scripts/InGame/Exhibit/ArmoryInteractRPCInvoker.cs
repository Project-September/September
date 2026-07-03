using Fusion;
using UnityEngine;

namespace InGame.Exhibit
{
    public class ArmoryInteractRPCInvoker : NetworkBehaviour
    {
        [SerializeField] private GameObject _attackWeapon;
        [SerializeField] private Vector3 _handPosition;
        [SerializeField] private Vector3 _handRotation;
        [Networked] public bool IsEquipped { get; private set; }
        private GameObject _attachedWeapon;

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_AttachWeapon(NetworkObject player)
        {
            AttachWeapon(player);
        }

        private void AttachWeapon(NetworkObject player)
        {
            if (_attackWeapon == null)
            {
                Debug.LogError($"No attack weapon attached to player {player}");
                return;
            }

            // Player‚Ì‰EŽè‚ð’T‚·
            Transform rightHand = FindRightHand(player);

            if (rightHand == null || _attachedWeapon != null)
                return;

            _attachedWeapon = Instantiate(_attackWeapon, rightHand.transform);
            _attachedWeapon.transform.localPosition = _handPosition;
            _attachedWeapon.transform.localEulerAngles = _handRotation;

            if (HasStateAuthority)
            {
                IsEquipped = true;
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_DestroyWeapon()
        {
            DestroyWeapon();
        }

        private void DestroyWeapon()
        {
            if (_attachedWeapon == null)
                return;

            Destroy(_attachedWeapon);
            _attachedWeapon = null;

            if (HasStateAuthority)
            {
                IsEquipped = false;
            }
        }

        private Transform FindRightHand(NetworkObject player)
        {
            Transform rightHand = null;
            Transform[] allChildren = player.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in allChildren)
            {
                if (child.CompareTag("RightHand"))
                {
                    rightHand = child;
                    break;
                }
            }

            if (rightHand == null)
            {
                Debug.LogError($"RightHand not found on {player.name}");
                return null;
            }

            return rightHand;
        }
    }
}
