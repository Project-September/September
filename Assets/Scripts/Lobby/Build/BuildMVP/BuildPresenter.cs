using September.Common;
using System;
using System.Collections.Generic;

namespace September.Lobby
{
    public class BuildPresenter : IDisposable
    {
        BuildDatasRuntime _runtime;
        BuildViewBase _view;
        /// <summary>登録解除管理クラスをまとめて購読解除するためのコレクション</summary>
        readonly List<ActionDisposable> _disposes = new();

        public BuildPresenter(BuildDatas data, BuildViewBase view)
        {
            _runtime = new(data);
            _view = view;
            if (_view == null) throw new ArgumentNullException(nameof(_view));
            Init();
        }

        void Init()
        {
            ActionDisposable dispose = null;
            //Viewに登録
            dispose = _view.OnMoveIndex(_runtime.MoveIndex);
            _disposes.Add(dispose);
            dispose = _view.OnSelectBuild(_runtime.SelectBuild);
            _disposes.Add(dispose);

            //Runtimeに登録
            dispose = _runtime.OnMoveIndex(_view.VisualizeBuildInfo);
            _disposes.Add(dispose);
            dispose = _runtime.OnSelectBuild(_view.VisualizeSelection);
            _disposes.Add(dispose);
        }

        public void Dispose()
        {
            foreach (var deregistration in _disposes)
                deregistration?.Dispose();
        }
    }
}
