using Fusion;
using UnityEngine;

namespace InGame.Jewelry.Common
{
    /// <summary>プレイヤーの宝石情報を管理する諸々のクラスの初期化を管理するクラス</summary>
    public class PlayerJewelryFactory : NetworkBehaviour
    {
        [Header("プレイヤーの宝石保持情報に必要な参照")]
        [SerializeField] PlayerJewelryDefinition _playerJewelryModel;
        [SerializeField] PlayerJewelryRuntime _playerJewelryRuntime;
        [SerializeField] PlayerJewelryView _playerJewelryView;
        [SerializeField] PlayerJewelryContainer _playerJewelryContainer;
        PlayerJewelryPresenter _playerJewelryPresenter;

        public override void Spawned()
        {
            _playerJewelryPresenter = new(_playerJewelryModel, _playerJewelryRuntime, _playerJewelryView, _playerJewelryContainer);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _playerJewelryPresenter?.Dispose();
        }
    }
}
