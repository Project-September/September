
using September.InGame.Common.Hitbox.ShapeStructs;
using September.InGame.Kraken;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Binder
{
    public class BoxHitboxBinder : IHitboxBinder<BoxHitbox, BoxHitboxShape, IBoxPrediction>
    {
        [SerializeField] private BoxHitboxShape[] _shapes;
        [SerializeReference, SubclassSelector] private IBoxPrediction _prediction;
        
        BoxHitboxShape[] IHitboxBinder<BoxHitbox, BoxHitboxShape, IBoxPrediction>.Shapes => _shapes;
        IBoxPrediction IHitboxBinder<BoxHitbox, BoxHitboxShape, IBoxPrediction>.Prediction => _prediction;
    }
}