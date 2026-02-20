using System;
using System.Threading;
using CRISound;
using Cysharp.Threading.Tasks;
using Fusion;
using NaughtyAttributes;
using UnityEngine;
using InGame.Interact;
using InGame.Player;
using Ingame.Tanihira;
using September.Common;
using September.InGame.Effect;
using September.InGame;

namespace InGame.Exhibit
{
    [Serializable]
    public class WarpInteractEffect : CharacterInteractEffectBase
    {
        [Label("ワープ先（Goal）")] public GameObject _warpDestination;
        [Label("Duration")] public float _warpDuration = 0.5f;
        [Label("ワープ先のワープポジション")] public GameObject _warpPosition;
        [Label("音名")] public string _warpInSoundName;
        [SerializeField] private float _coolDownSeconds = 3f;

        public string _warpOutSoundName;

        public AudioBroadcaster _audioBroadcaster;

        private CancellationTokenSource _cts;
        private InteractableBase _interactableBase;
        private EffectSpawner _effectSpawner;
        private bool _isCoolDown;

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
            if (_isCoolDown)
                return;

            _cts = new CancellationTokenSource();

            if (!_effectSpawner)
                _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();

            _interactableBase = target;

            PlayerRef playerRef = PlayerRef.FromEncoded(context.Interactor);

            if (target.Runner.TryGetPlayerObject(playerRef, out NetworkObject playerNetworkObject))
            {
                HandleWarpAsync(playerNetworkObject).Forget();
            }
        }

        // インタラクト時のWarp処理
        private async UniTaskVoid HandleWarpAsync(NetworkObject player)
        {
            if (!_interactableBase)
                return;

            // StateAuthority 以外は何もしない
            if (!_interactableBase.HasStateAuthority)
                return;

            // ワープ先のInteractable取得
            InteractableBase destinationInteractable =
                _warpDestination
                    ? _warpDestination.GetComponent<InteractableBase>()
                    : null;

            try
            {
                float cooldown = 3f;

                _interactableBase.ForceStartCooldown(cooldown);

                if (destinationInteractable)
                {
                    destinationInteractable.ForceStartCooldown(cooldown);
                }

                Vector3 effectPos = player.transform.position + Vector3.up;
                PlayEffect(EffectType.WarpIn, effectPos, Quaternion.identity);

                // プレイヤー非表示
                SetPlayerVisible(player, false);
                
                Vector3 targetPos = _warpPosition.transform.position;

                Vector3 forward = _warpDestination.transform.forward;
                Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
                
                PlayerManager playerManager = player.GetComponent<PlayerManager>();
                playerManager?.SetWarpTarget(targetPos, targetRot);

                // フレンドも移動
                FormationManager formationManager = player.GetComponent<FormationManager>();
                formationManager?.WarpFriendNearPlayer(targetPos, targetRot);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_warpDuration),
                    cancellationToken: _cts.Token);
                
                PlayEffect(EffectType.WarpOut, targetPos, Quaternion.identity);

                // プレイヤー再表示
                SetPlayerVisible(player, true);
                
                if (_audioBroadcaster)
                {
                    _audioBroadcaster.RPC_PlaySoundFromCode(
                        _warpOutSoundName,
                        SoundTrackingType.Spot,
                        player.Id);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Warp cancelled.");
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
                foreach (FriendBase friend in formationManager.CurrentFriendsList)
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

        private async UniTaskVoid StartCoolDown()
        {
            _isCoolDown = true;

            await UniTask.Delay(
                TimeSpan.FromSeconds(_coolDownSeconds),
                cancellationToken: _cts.Token);

            _isCoolDown = false;
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}