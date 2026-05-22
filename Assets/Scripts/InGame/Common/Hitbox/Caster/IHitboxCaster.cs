using System;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Caster
{
    public interface IHitboxCaster
    {
        public event Action<Collider> OnHit;
        public void StartCast();
    }
}
