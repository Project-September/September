using System.Collections.Generic;
using UnityEngine;

namespace September.InGame.Kraken
{
    public interface IPointInterpolator
    {
        public IKFollower.Point Evaluate(float distance);
        public IKFollower.Point[] Evaluate(IReadOnlyList<float> distances);
        public void DebugDraw(Color color);
    }
}