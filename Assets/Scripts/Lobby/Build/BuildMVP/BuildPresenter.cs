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
        readonly List<ActionDeregistration> _deregistrations = new();

        public BuildPresenter(BuildDatas data, BuildViewBase view)
        {
            _runtime = new(data);
            _view = view;
            if (_view == null) throw new ArgumentNullException(nameof(_view));
            Init();
        }

        void Init()
        {
            ActionDeregistration deregistration = null;
            //Viewに登録
            deregistration = _view.OnNextIndex(_runtime.MoveIndex);
            _deregistrations.Add(deregistration);
            deregistration = _view.OnBackIndex(_runtime.MoveIndex);
            _deregistrations.Add(deregistration);
            deregistration = _view.OnSelectBuild(_runtime.SelectBuild);
            _deregistrations.Add(deregistration);

            //Runtimeに登録
            deregistration = _runtime.OnMoveIndex(_view.VisualizeBuildInfo);
            _deregistrations.Add(deregistration);
            deregistration = _runtime.OnSelectBuild(_view.VisualizeSelection);
            _deregistrations.Add(deregistration);
        }

        public void Dispose()
        {
            foreach (var deregistration in _deregistrations)
                deregistration?.Dispose();
        }
    }
}
