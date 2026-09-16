using Cysharp.Threading.Tasks;
using Fusion;
using September.InGame.UI;
using System.Threading;
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
        CancellationTokenSource _cts;

        public override void Spawned()
        {
            _cts = new CancellationTokenSource();
            Init(_cts.Token).Forget();
        }

        async UniTask Init(CancellationToken token)
        {
            if (Object.InputAuthority == Runner.LocalPlayer)
            {
                // ローカル用のUIを確実に取得する
                do
                {
                    _playerJewelryView = UIController.I.UIRootRefs.JewelryView;
                    await UniTask.DelayFrame(1, cancellationToken: token);
                } while (_playerJewelryView == null);
            }

            await UniTask.DelayFrame(1, cancellationToken: token);
            _playerJewelryPresenter = new(_playerJewelryDefinition, _playerJewelryRuntime, _playerJewelryView, _playerJewelryContainer);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _cts.Cancel();
            _playerJewelryPresenter?.Dispose();
        }
    }
}
