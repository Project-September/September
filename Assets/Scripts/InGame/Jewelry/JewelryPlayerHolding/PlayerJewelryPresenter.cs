using InGame.Jewelry.Common;
using September.Common;
using System;

namespace InGame.Jewelry
{
    public class PlayerJewelryPresenter : IDisposable
    {
        PlayerJewelryRuntime _runtime;
        PlayerJewelryView _view;
        PlayerJewelryContainer _container;
        ActionDisposable _actionDisposable;

        public PlayerJewelryPresenter(PlayerJewelryModel model
            , PlayerJewelryRuntime runtime
            , PlayerJewelryView view
            , PlayerJewelryContainer container)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (container == null) throw new ArgumentNullException(nameof(container));

            _runtime = runtime;
            _view = view;
            _container = container;
            _actionDisposable = new();
            Init(model);
        }

        public void Dispose()
        {
            _actionDisposable?.Dispose();
        }

        void Init(PlayerJewelryModel model)
        {
            // viewにアクション登録
            _actionDisposable.AddActionDisposing(_container.OnGetJewelry(_runtime.GetJewelry));
            _actionDisposable.AddActionDisposing(_container.OnDropJewelry(_runtime.DropJewelry));

            // runtimeにアクション登録
            _actionDisposable.AddActionDisposing(_runtime.OnInitialize(_view.Init));
            _actionDisposable.AddActionDisposing(_runtime.OnUpdateJewelryQuantity(_view.UpdateJewelryCount));

            _runtime.Init(model);
        }
    }
}
