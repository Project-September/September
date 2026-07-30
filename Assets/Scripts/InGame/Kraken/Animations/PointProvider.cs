using System.Linq;
using RootMotion.FinalIK;
using September.Common.Extensions;

namespace September.InGame.Kraken.Animations
{
    public interface IPointProvider
    {
        public IKFollower.Point[] GetPoints();
    }

    public readonly struct IKSolverPointProvider : IPointProvider
    {
        private readonly IKFollower.Point[] _points;

        public IKSolverPointProvider(IKSolver solver)
        {
            _points = solver.GetPoints().DistinctBy(x => x.transform).Select(Convert).ToArray();
        }

        public IKFollower.Point[] GetPoints()
        {
            return _points;
        }

        private static IKFollower.Point Convert(IKSolver.Point point)
        {
            return new IKFollower.Point(point.solverPosition, point.solverRotation);
        }
    }
}
