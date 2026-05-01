using Fusion;
using September.Common;

namespace September.InGame.Kraken
{
    public class Kraken : NetworkBehaviour
    {
        [Networked] private float Rotate { get; set; }

        public override void FixedUpdateNetwork()
        {
            if (GetInput<PlayerInput>(out var input))
            {
                Rotate += input.MoveDirection.x;
            }
            
            transform.Rotate(0, Rotate, 0);
        }
    }
}