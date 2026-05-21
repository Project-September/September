using Fusion;
using September.Common;

namespace InGame.Player
{
    public class PlayerInputManager : NetworkBehaviour
    {
        public virtual bool GetPlayerInput(out PlayerInput input)
        {
            return GetInput(out input);
        }
    }
}