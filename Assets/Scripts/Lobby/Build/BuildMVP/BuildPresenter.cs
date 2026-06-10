using September.Common;
using System;

namespace September.Lobby
{
    public class BuildPresenter : IDisposable
    {
        BuildRuntime _runtime;
        BuildViewBase _view;
        readonly ActionDisposable _dispose;

        public BuildPresenter(BuildDatas data, BuildViewBase view)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (data == null) throw new ArgumentNullException(nameof(data));
            // 諸々の初期化をPresenterに集約
            _runtime = new(data);
            _view = view;
            _dispose = new();
            Init(data);
        }

        void Init(BuildDatas data)
        {
            _view.Init(data.Builds);
            // Viewに登録
            _dispose.AddActionDisposing(_view.OnMoveIndex(_runtime.MoveIndex));
            _dispose.AddActionDisposing(_view.OnMoveIndexForButton(_runtime.MoveIndex));
            _dispose.AddActionDisposing(_view.OnSelectBuild(_runtime.SelectBuild));
            // Runtimeに登録
            _dispose.AddActionDisposing(_runtime.OnMoveIndex(_view.VisualizeBuildInfo));
            _dispose.AddActionDisposing(_runtime.OnSelectBuild(_view.VisualizeSelection));
        }

        public void Dispose()
        {
            // ActionDisposableクラスのDisposeによって一括解除
            _dispose?.Dispose();
        }
    }
}
