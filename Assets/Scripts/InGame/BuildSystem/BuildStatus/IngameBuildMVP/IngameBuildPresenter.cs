using September.Common;
using September.InGame.Common.Stats;
using System;

namespace September
{
    public class IngameBuildPresenter : IDisposable
    {
        IngameBuildRuntime _runtime;
        IngameBuildView _view;
        readonly ActionDisposable _dispose;

        public IngameBuildPresenter(BuildDefinition definition, IngameBuildView view)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            _runtime = new IngameBuildRuntime(definition);
            _view = view;
            _dispose = new();
            Init();
        }

        void Init()
        {
            // Viewに登録
            _dispose.AddActionDisposing(_view.OnEnableBuild(_runtime.EnableBuild));
            _dispose.AddActionDisposing(_view.OnUpdateBuild(_runtime.AddProgress));
            // Runtimeに登録
            _dispose.AddActionDisposing(_runtime.OnAddProgress(_view.VisualizeBuild));
            _dispose.AddActionDisposing(_runtime.OnEnableBuild(_view.EnableBuild));
        }

        public void Dispose()
        {
            // ActionDisposableクラスのDisposeによって一括解除
            _dispose?.Dispose();
        }
    }
}
