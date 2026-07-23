using Fusion;
using September.Common;
using September.InGame.UI;
using UnityEngine;

namespace InGame.Jewelry.Common
{
    /// <summary>プレイヤーの宝石情報を管理する諸々のクラスの初期化を管理するクラス</summary>
    public class PlayerJewelryFactory : NetworkBehaviour
    {
        [Header("プレイヤーの宝石保持情報に必要な参照")]
        [SerializeField] PlayerJewelryDefinition _playerJewelryDefinition;
        [SerializeField] PlayerJewelryRuntime _playerJewelryRuntime;
        [SerializeField] PlayerJewelryView _playerJewelryView;
        [SerializeField] PlayerJewelryContainer _playerJewelryContainer;
        PlayerJewelryPresenter _playerJewelryPresenter;

        readonly ActionDisposable _actionDisposable = new();

        public override void Spawned()
        {
            if (Object.InputAuthority == Runner.LocalPlayer)
            {
                PlayerJewelryView view = UIController.I.UIRootRefs.JewelryView;
                _actionDisposable.AddActionDisposing(_playerJewelryRuntime.OnInitialize(view.Init));
                _actionDisposable.AddActionDisposing(_playerJewelryRuntime.OnUpdateJewelryQuantity(view.UpdateJewelryCount));
            }

            _playerJewelryPresenter = new(_playerJewelryDefinition, _playerJewelryRuntime, _playerJewelryView, _playerJewelryContainer);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _playerJewelryPresenter?.Dispose();
            _actionDisposable.Dispose();
        }
    }
}
