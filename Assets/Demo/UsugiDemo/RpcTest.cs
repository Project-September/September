using Fusion;
using September.Common;
using UnityEngine;

public class RpcTest : NetworkBehaviour
{
    private bool _wasPressedLastFrame = false;

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (!GetInput(out PlayerInput input)) return;

        bool isPressed = input.Buttons.IsSet(PlayerButtons.Interact);

        if (isPressed && !_wasPressedLastFrame)
        {
            Debug.Log("[CLIENT] RPC送信");
            TestRpc();
        }

        _wasPressedLastFrame = isPressed;
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void TestRpc()
    {
        Debug.Log("[SERVER] RPC受信成功！");
    }
}
