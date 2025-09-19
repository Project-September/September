using System;
using Fusion;
using September.Common;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Result;
using September.InGame.Effect;
using September.InGame;
using September.InGame.Common;
using InGame.Player;
using CRISound;
using WebSocketSharp;

namespace InGame.Interact
{
    [RequireComponent(typeof (AudioBroadcaster))] // サウンド再生用のコンポーネント
    [DisallowMultipleComponent]
    public class InteractableBase : NetworkBehaviour
    {
        [SerializeField] private SerializableDictionary<CharacterType, float> _requiredInteractTimeDictionary = new();

        [SerializeField] private SerializableDictionary<CharacterType, float> _cooldownTimeDictionary = new();

        [SerializeReference, SubclassSelector] private List<CharacterInteractEffectBase> _characterEffects = new();

        [SerializeField] private ExhibitType _type;
        [SerializeField] private Vector3 _interactEffectOffset = Vector3.zero;
        [SerializeField] private Transform _cooldownEffectTransform;
        [SerializeField] private Vector3 _cooldownEffectOffset = Vector3.zero;
        [SerializeField] private Vector3 _cooldownEffectRotation = Vector3.zero;
        [SerializeField] private EffectType _interactEffectType = EffectType.NormalInteractComplete;
        [SerializeField] private EffectType _cooldownEffectType = EffectType.CooldownSquare;
        [SerializeField] private bool _spawnCooldownEffectOnStart = true;
        [SerializeField] private AudioBroadcaster _audioBroadcaster;
        [SerializeField] private string _interactSoundCueName;
        [SerializeField] private SoundTrackingType _interactSoundTrackingType = SoundTrackingType.Spot;

        [Networked] public float LastInteractTime { get; set; } = -9999f;

        [Networked] public float LastUsedCooldownTime { get; set; } = 0f;

        /// <summary>
        /// 外部から強制的にインタラクト可能にするかどうかを設定するために使う
        /// </summary>
        [Networked]
        public bool ForceSetInteractable { get; set; } = true;

        public SerializableDictionary<CharacterType, float> RequiredInteractTimeDictionary =>
            _requiredInteractTimeDictionary;

        public SerializableDictionary<CharacterType, float> CooldownTimeDictionary => _cooldownTimeDictionary;

        public ExhibitType ExhibitType => _type;

        private CharacterInteractEffectBase _activeEffectBase;

        public void Interact(IInteractableContext context)
        {
            if (!HasStateAuthority) return;

            if (!ValidateInteraction(context))
            {
                Debug.Log($"[InteractableBase] OnValidateInteraction により拒否: {context.Interactor}");
                return;
            }

            var charaType = context.CharacterType;

            // All 優先でクールダウン時間を取得
            LastUsedCooldownTime = _cooldownTimeDictionary.Dictionary.TryGetValue(CharacterType.All, out var all)
                ? all
                : _cooldownTimeDictionary.Dictionary.GetValueOrDefault(charaType, 0f);

            // クールダウン登録（サーバ時刻 or ローカル時刻）
            LastInteractTime = Runner ? Runner.SimulationTime : Time.time;

            // クールダウンのループエフェクト（必要なら）
            if (_spawnCooldownEffectOnStart && LastUsedCooldownTime > 0f)
            {
                PlayCooldownEffect(LastUsedCooldownTime).Forget();
            }

            // ワンショットの相互作用エフェクト（ホスト側でのみ再生。見た目の同期は別途やる場合はRPC/OnChangedで）
            var effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            effectSpawner.RequestPlayOneShotEffect(_interactEffectType, transform.position + _interactEffectOffset, transform.rotation);

            // このインタラクトに紐づく派生処理
            OnInteract(context);

            // 実行者
            PlayerRef actor = PlayerRef.FromEncoded(context.Interactor);
            
            if (_type != ExhibitType.None)
            {
                PlayerDatabase.Instance.Server_AddExhibit(actor, _type);
            }

            if (_audioBroadcaster != null)
            {
                _audioBroadcaster.RPC_PlaySoundFromCode(_interactSoundCueName, _interactSoundTrackingType, Object, actor); // 2D + 3D再生
            }
        }

        public async UniTask PlayCooldownEffect(float cooldownTime)
        {
            if (cooldownTime <= 0f) return;
            var effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            var uniqueEffectId = NetworkRunner.Instances.First().LocalPlayer.PlayerId +
                                 DateTime.UtcNow.ToString("yyyy-MM-dd-HH:mm:ss");
            var effectTransform = _cooldownEffectTransform != null ? _cooldownEffectTransform : transform;
            effectSpawner.RequestPlayLoopEffect(uniqueEffectId, _cooldownEffectType,
                effectTransform.position + _cooldownEffectOffset, Quaternion.Euler(_cooldownEffectRotation));
            await UniTask.Delay(TimeSpan.FromSeconds(cooldownTime), ignoreTimeScale: false);
            effectSpawner.StopEffect(uniqueEffectId);
        }

        /// <summary>
        /// 共通のバリデーション（null, クールダウン）
        /// インタラクト可能なときは true を返す
        /// </summary>
        public bool ValidateInteraction(IInteractableContext context)
        {
            var type = context.CharacterType;
            if (IsInCooldown())
            {
                //Debug.LogError("[InteractableBase] クールダウン中のためインタラクトできません");
                return false;
            }

            if (!Object.isActiveAndEnabled)
            {
                //Debug.LogError($"[{name}] インタラクト可能なオブジェクトが無効です");
                return false;
            }

            if (!ForceSetInteractable)
            {
                //Debug.LogError($"[{name}] インタラクト可能なオブジェクトが強制的に無効化されています");
                return false;
            }

            if (!OnValidateInteraction(context, type))
            {
                //Debug.LogError($"[{name}] インタラクト可能なオブジェクトが OnValidateInteraction により拒否されました");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 派生クラスでの個別条件（ロック中、所有者チェックなど）
        /// インタラクト可能ならTrueを返す
        /// </summary>
        protected virtual bool OnValidateInteraction(IInteractableContext context, CharacterType charaType)
        {
            // ゲーム終了状態の時はインタラクトを無効化
            if (IsGameEnded())
            {
                return false;
            }

            // プレイヤーがスタン状態の時はインタラクトを無効化
            if (IsPlayerStunned(context))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// ゲームが終了状態かどうかを判定する
        /// </summary>
        private bool IsGameEnded()
        {
            try
            {
                var inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();
                if (inGameManager == null) return false;

                // EndingStateかどうかをチェック
                return inGameManager.CurrentStateName == "EndingState";
            }
            catch (System.Exception)
            {
                // エラーが発生した場合は安全側にインタラクトを許可
                return false;
            }
        }

        /// <summary>
        /// プレイヤーがスタン状態かどうかを判定する
        /// </summary>
        private bool IsPlayerStunned(IInteractableContext context)
        {
            try
            {
                // インタラクト実行者のPlayerRefを取得
                PlayerRef playerRef = PlayerRef.FromEncoded(context.Interactor);

                var inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();
                if (inGameManager == null) return false;

                // プレイヤーのNetworkObjectを取得
                if (inGameManager.PlayerDataDic.TryGetValue(playerRef, out NetworkObject playerObject))
                {
                    // PlayerManagerを取得してスタン状態をチェック
                    var playerManager = playerObject.GetComponent<PlayerManager>();
                    if (playerManager != null)
                    {
                        return playerManager.IsStun;
                    }
                }
            }
            catch (System.Exception)
            {
                // エラーが発生した場合は安全側でインタラクトを許可
            }

            return false;
        }

        protected virtual void OnInteract(IInteractableContext context)
        {
            var charaType = context.CharacterType;
            // All を優先し、特定キャラタイプの effect があれば上書きする
            var effect = _characterEffects
                             .FirstOrDefault(e => e is { CharacterType: CharacterType.All })
                         ?? _characterEffects.FirstOrDefault(e => e != null && e.CharacterType == charaType);

            if (effect != null)
            {
                _activeEffectBase = effect.Clone();
                _activeEffectBase.OnInteractStart(context, this);
            }
            else
            {
                Debug.LogWarning($"[{name}] {charaType} のインタラクト効果が設定されていません");
            }
        }

        public bool IsInCooldown()
        {
            if (LastUsedCooldownTime <= 0f) return false;
            var currentTime = Runner ? Runner.SimulationTime : Time.time;
            float timeSinceLast = currentTime - LastInteractTime;
            return timeSinceLast < LastUsedCooldownTime;
        }

        private void Update()
        {
            if (!HasStateAuthority) return;
            _activeEffectBase?.OnInteractUpdate(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (!HasStateAuthority) return;
            _activeEffectBase?.OnInteractLateUpdate(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!HasStateAuthority) return;
            _activeEffectBase?.OnInteractFixedUpdate();
        }

        public override void FixedUpdateNetwork()
        {
            GetInput(out PlayerInput input);
            _activeEffectBase?.OnInteractFixedNetworkUpdate(input);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!HasStateAuthority) return;
            _activeEffectBase?.OnInteractCollisionStay(collision);
        }

        // 必要に応じて外部 or クールダウンなどから呼び出す用
        public void EndInteract()
        {
            _activeEffectBase?.OnInteractEnd();
            _activeEffectBase = null;
        }
    }

    public interface IInteractableContext : INetworkStruct
    {
        int Interactor { get; }
        CharacterType CharacterType { get; set; }
    }

    // シンプルな実装例。必要に合わせて情報は追加してください
    public struct InteractableContext : IInteractableContext
    {
        public int Interactor { get; set; }
        public CharacterType CharacterType { get; set; }
    }
}