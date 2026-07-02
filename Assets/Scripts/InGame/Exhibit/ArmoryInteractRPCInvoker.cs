using Fusion;
using UnityEngine;

namespace InGame.Exhibit
{
    public class ArmoryInteractRPCInvoker : NetworkBehaviour
    {
        [SerializeField] private GameObject _attackWeapon;
        [SerializeField] private Vector3 _handPosition;
        [SerializeField] private Vector3 _handRotation;

        private GameObject _instantiateWeapon;

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

            Transform rightHand = null;
            Transform[] allChildren = player.GetComponentsInChildren<Transform>(true);
            // Player‚Ì“ª‚ÌˆÊ’u‚ð’T‚·
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
                Debug.LogError("AttackWeapon RightHand is null");
                return;
            }

            _instantiateWeapon = Instantiate(_attackWeapon, rightHand.transform);
            _instantiateWeapon.transform.localPosition = _handPosition;
            _instantiateWeapon.transform.localEulerAngles = _handRotation;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_DestroyWeapon()
        {
            DestroyWeapon();
        }
        private void DestroyWeapon()
        {
            if (_instantiateWeapon == null)
                return;

            Destroy(_instantiateWeapon);
            _instantiateWeapon = null;
        }
    }
}
