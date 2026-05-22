using System;
using September.InGame.Common.Hitbox.Hitboxes;
using September.InGame.Common.Hitbox.Prediction;
using September.InGame.Common.Hitbox.Shapes;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Binder
{
    [Serializable]
    public class BoxHitboxBinder : IHitboxBinder<Box, BoxHitbox, IBoxPrediction>
    {
        [SerializeField] private BoxHitbox[] _shapes;
        [SerializeReference, SubclassSelector] private IBoxPrediction _prediction;
        [SerializeField] private LayerMask _baseLayerMask = ~0;

        BoxHitbox[] IHitboxBinder<Box, BoxHitbox, IBoxPrediction>.Shapes => _shapes;
        IBoxPrediction IHitboxBinder<Box, BoxHitbox, IBoxPrediction>.Prediction => _prediction;
        LayerMask IHitboxBinder<Box, BoxHitbox, IBoxPrediction>.BaseLayerMask => _baseLayerMask;
    }
}
