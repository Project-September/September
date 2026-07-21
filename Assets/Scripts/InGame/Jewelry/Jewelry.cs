using Fusion;
using InGame.Player;
using UnityEngine;

namespace September.InGame.Jewelry
{
    public class Jewelry : NetworkBehaviour, IJewelry
    {
        [SerializeField] private int _score = 1;

        public int Score => _score;
    }
}
