using September.Common;
using System;

namespace September.InGame.Common.Stats
{
    public class IngameBuildPresenter : IDisposable
    {
        IngameBuildRuntime _runtime;
        IngameBuildView _view;
        BuildEffector _effector;
        readonly ActionDisposable _dispose;

        public IngameBuildPresenter(BuildDefinition definition, IngameBuildView view, BuildEffector effector)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (effector == null) throw new ArgumentNullException(nameof(effector));
            _runtime = new IngameBuildRuntime(definition);
            _view = view;
            _effector = effector;
            _dispose = new();
            Init();
        }

        void Init()
        {
            // Effectorに登録
            _dispose.AddActionDisposing(_effector.OnUpdateBuild(_runtime.AddProgress));
            _dispose.AddActionDisposing(_effector.OnEnableBuild(_runtime.EnableBuild));
            // Runtimeに登録
            _dispose.AddActionDisposing(_runtime.OnAddProgress(_view.VisualizeBuild));
            _dispose.AddActionDisposing(_runtime.OnEnableBuild(_effector.Init));
            _dispose.AddActionDisposing(_runtime.OnAddProgress(_effector.UpdateBuild));
        }

        public void Dispose()
        {
            // ActionDisposableクラスのDisposeによって一括解除
            _dispose?.Dispose();
        }
    }
}
