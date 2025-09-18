using UnityEngine.Splines;

namespace InGame.Player.Sarutobi
{
    public class GrapplingSpline : SingletonMonoBehaviour<GrapplingSpline>
    {
        public SplineContainer GrapplingTargetSpline { get; private set; }

        private void Start()
        {
            GrapplingTargetSpline = GetComponent<SplineContainer>();
        }
    }
}
