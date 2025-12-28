using UniRx;

namespace September.NewResult
{
    public static class SceneTransitionState
    {
        // Hack: こういうことするとドメインリロードしないとバグる
        public static bool IsTransitioning = false;
    }
}