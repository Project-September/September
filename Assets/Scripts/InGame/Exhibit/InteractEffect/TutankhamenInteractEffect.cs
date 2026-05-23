using System;
using Fusion;
using Ingame.Tanihira;
using InGame.Common;
using InGame.Interact;
using September.Common;
using September.InGame.Common.Stats;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Exhibit
{
    [Serializable]
    public class TutankhamenInteractEffect : CharacterInteractEffectBase
    {
        public TutankhamenInteractRPCInvoker Invoker;
        public float EffectDuration;
        public GameObject EffectPos;
        public float BoostMultiplier = 1.5f;

        [SerializeField] private StatusEffect _buffEffect;
        
        private float OriginalSpeedRate = -1f;
        // 過剰Destroy防止
        private bool _isDestroyScheduled;
        private EffectSpawner _effectSpawner;
        // Interact対象
        private NetworkObject _targetPlayerObject;

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            _isDestroyScheduled = false;
            PlayerRef playerRef = PlayerRef.FromEncoded(context.Interactor);

            if (PlayerDatabase.Instance.PlayerObjectDic.TryGet(playerRef,out var playerNetworkObject))
            {
                if (playerNetworkObject.TryGetComponent(out StatusEffectManager statusEffectManager))
                {
                    StatusEffectManager.StatusEffectSpec spec = new(_buffEffect);
                    spec.Duration = EffectDuration;
                    spec.Modifiers[0].SetByCallerMagnitude(BoostMultiplier);
                    statusEffectManager.AddEffect(spec);
                }
                
                if (playerNetworkObject.TryGetComponent(out FormationManager formationManager))
                {
                    var friendList = formationManager.FriendsList;
                    foreach (var friend in friendList)
                    {
                        friend.StartBuff(BoostMultiplier);
                    }
                }
                
                _targetPlayerObject = playerNetworkObject;
                Invoker.RPC_AttachHeadMask(playerNetworkObject, EffectDuration);
                PlayEffect();
                Invoker.RPC_ShowStatusUpUI(playerRef);
            }
            else
                Debug.LogError("Player not found");
        }

        public override void OnInteractUpdate(float deltaTime)
        {
            if (_isDestroyScheduled || Invoker == null || !Invoker.IsMaskAttached) 
                return;
            
            _isDestroyScheduled = true;
            Invoker.RPC_DestroyMask();
            RestorePlayerSpeed();
        }
        
        // Buffを初期値に戻す
        private void RestorePlayerSpeed()
        {
            _targetPlayerObject = null;
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new TutankhamenInteractEffect
            {
                Invoker = Invoker,
                EffectDuration = EffectDuration,
                EffectPos = EffectPos,
                OriginalSpeedRate = OriginalSpeedRate,
                BoostMultiplier = BoostMultiplier,
                _buffEffect = _buffEffect
            };
        }

        // 非同期でAnimationを再生する
        private void PlayEffect()
        {
            // Effect生成処理
            _effectSpawner ??= StaticServiceLocator.Instance.Get<EffectSpawner>();
            _effectSpawner?.RequestPlayOneShotEffect(EffectType.Tutankhamen, EffectPos.transform.position,
                EffectPos.transform.rotation,EffectPos.transform);
        }
    }
}