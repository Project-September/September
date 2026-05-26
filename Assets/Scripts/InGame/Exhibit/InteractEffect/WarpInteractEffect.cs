using System;
using System.Threading;
using CRISound;
using Cysharp.Threading.Tasks;
using Fusion;
using Ingame.Tanihira;
using InGame.Interact;
using InGame.Player;
using NaughtyAttributes;
using September.Common;
using September.InGame;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Exhibit
{
    [Serializable]
    public class WarpInteractEffect : CharacterInteractEffectBase
    {
        [Label("ワープ先（Goal）")]
        public GameObject _warpDestination;
        [Label("Duration")]
        public float _warpDuration = 0.5f;
        [Label("ワープ先のワープポジション")]
        public GameObject _warpPosition;
        [Label("音名")]
        public string _warpInSoundName;

        public string _warpOutSoundName;

        public AudioBroadcaster _audioBroadcaster;

        private CancellationTokenSource _cts;
        private InteractableBase _interactableBase;
        private EffectSpawner _effectSpawner;

        public override CharacterInteractEffectBase Clone()
        {
            return new WarpInteractEffect
            {
                _warpDestination = _warpDestination,
                _warpDuration = _warpDuration,
                _warpPosition = _warpPosition,
                _warpOutSoundName = _warpOutSoundName,
                _audioBroadcaster = _audioBroadcaster,
            };
        }

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            _cts = new CancellationTokenSource();
            if (!_effectSpawner)
                _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            _interactableBase = target;
            PlayerRef playerRef = PlayerRef.FromEncoded(context.Interactor);
            if (PlayerDatabase.Instance.PlayerObjectDic.TryGet(playerRef, out var playerNetworkObject))
            {
                HandleWarpAsync(playerNetworkObject).Forget();
                // 同時インタラクト不可
                target.ForceSetInteractable = false;
            }
        }

        // インタラクト時のWarp処理
        private async UniTaskVoid HandleWarpAsync(NetworkObject player)
        {
            // Effectの再生
            Vector3 effectPos = player.transform.position + Vector3.up * 1.0f;
            PlayEffect(EffectType.WarpIn, effectPos, Quaternion.identity);

            // Playerを初期化
            SetPlayerVisible(player, false);
            Vector3 targetPos = _warpPosition.transform.position;
            Vector3 backward = _warpDestination.transform.forward;
            Quaternion targetRot = Quaternion.LookRotation(backward, Vector3.up);

            // NetWork経由で移動を指示
            PlayerManager playerManager = player.GetComponent<PlayerManager>();
            playerManager?.SetWarpTarget(targetPos, targetRot);

            //隊列を持っていたら、フレンドも移動させる
            FormationManager formationManager = player.GetComponent<FormationManager>();
            formationManager?.WarpFriendNearPlayer(targetPos, targetRot);

            // 少し待ってから移動予約
            await UniTask.Delay(TimeSpan.FromSeconds(_warpDuration), cancellationToken: _cts.Token);

            // ゴール側エフェクトの再生
            PlayEffect(EffectType.WarpOut, targetPos, Quaternion.identity);
            SetPlayerVisible(player, true);
            _interactableBase.ForceSetInteractable = true;

            if (_audioBroadcaster != null)
            {
                _audioBroadcaster.RPC_PlaySoundFromCode(_warpOutSoundName, SoundTrackingType.Spot, player.Id, default); // 発音元を一旦Playerに
            }
        }

        private void SetPlayerVisible(NetworkObject player, bool isVisible)
        {
            // Playerオブジェクトのどっかから拾ってくる
            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = isVisible;
            }

            //隊列がある場合にはフレンドを見えなくする
            if (player.TryGetComponent<FormationManager>(out FormationManager formationManager))
            {
                foreach (var friend in formationManager.CurrentFriendsList)
                {
                    foreach (Renderer renderer in friend.GetComponentsInChildren<Renderer>())
                    {
                        renderer.enabled = isVisible;
                    }
                }
            }
        }

        private void PlayEffect(EffectType effectType, Vector3 effectPos, Quaternion effectRot)
        {
            _effectSpawner?.RequestPlayOneShotEffect(effectType, effectPos, effectRot);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}