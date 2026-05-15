using System;
using UnityEngine;

namespace September.InGame.Kraken
{
    public interface IHitboxCaster
    {
        public event Action<Collider> OnHit;
        public void StartCast();
    }
}