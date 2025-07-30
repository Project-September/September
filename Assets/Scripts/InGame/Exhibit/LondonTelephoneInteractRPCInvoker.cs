using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Interact;
using NaughtyAttributes;
using September.Common;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Exhibit
{
    /// <summary>
    /// ロンドンテレフォンインタラクトRPC送信処理
    /// </summary>
    public class LondonTelephoneInteractRPCInvoker : NetworkBehaviour
    {
        [SerializeField,Label("Effect持続時間")] private float _interactTimer;
        
        private EffectSpawner _effectSpawner;
        private CancellationTokenSource _cts;
        private InteractableBase  _interactableBase;

        private void Awake()
        {
            _cts =  new CancellationTokenSource();
            _interactableBase = GetComponent<InteractableBase>();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcRequestInteraction(PlayerRef requestingPlayer)
        {
            // ここで他のプレイヤーに通知
            foreach (var player in Runner.ActivePlayers)
            {
                if (player != requestingPlayer)
                { 
                    ShowEffect(player).Forget();
                }
            }
        }

        private async UniTask ShowEffect(PlayerRef player)
        {
            if (!Runner.TryGetPlayerObject(player, out var playerObject))
                return;

            // 同時インタラクト不可
            _interactableBase.ForceSetInteractable = false;
            Vector3 effectPosition = playerObject.transform.position + Vector3.down * 0.5f;
            
            // Effect生成処理
            InitEffectSpawner();
            string effectId = GenerateEffectId();
            
            _effectSpawner?.RequestPlayLoopEffect(
                effectId,
                EffectType.LondonTelephone,
                effectPosition,
                new());
            
            // Effectを消す
            await UniTask.Delay(TimeSpan.FromSeconds(_interactTimer),cancellationToken: _cts.Token);
            _effectSpawner?.StopEffect(effectId);
            _interactableBase.ForceSetInteractable =　true;　
        }

        private void InitEffectSpawner()
        {
            _effectSpawner ??= StaticServiceLocator.Instance.Get<EffectSpawner>();
        }

        private static string GenerateEffectId()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd-HH:mm:ss");
        }
    }
}