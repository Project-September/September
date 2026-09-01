using System;
using Fusion;

namespace September.InGame.Effect
{
    public readonly struct EffectID : INetworkStruct, IEquatable<EffectID>
    {
        private readonly int id;

        public bool IsValid => id > 0;

        public EffectID(int id)
        {
            this.id = id;
        }

        public bool Equals(EffectID other)
        {
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            return obj is EffectID other && Equals(other);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public override string ToString()
        {
            return $"id:{id}";
        }
    }
}