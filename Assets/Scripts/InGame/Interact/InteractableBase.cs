using System;
using Fusion;
using September.Common;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using September.InGame.Effect;

namespace InGame.Interact
{
    [DisallowMultipleComponent]
    public class InteractableBase : NetworkBehaviour
    {
        [SerializeField] private SerializableDictionary<CharacterType, float> _requiredInteractTimeDictionary = new();

        [SerializeField] private SerializableDictionary<CharacterType, float> _cooldownTimeDictionary = new();

        [SerializeReference, SubclassSelector] private List<CharacterInteractEffectBase> _characterEffects = new();
        
        [SerializeField] private EffectType _cooldownEffectType = EffectType.CooldownSquare;


        [Networked] public float LastInteractTime { get; set; } = -9999f;

        [Networked] public float LastUsedCooldownTime { get; set; } = 0f;
        
        /// <summary>
        /// 外部から強制的にインタラクト可能にするかどうかを設定するために使う
        /// </summary>
        [Networked] public bool ForceSetInteractable { get; set; } = true;

        public SerializableDictionary<CharacterType, float> RequiredInteractTimeDictionary => _requiredInteractTimeDictionary;
        public SerializableDictionary<CharacterType, float> CooldownTimeDictionary => _cooldownTimeDictionary;

        private CharacterInteractEffectBase _activeEffectBase;

        public void Interact(IInteractableContext context)
        {
            if (!HasStateAuthority) return;
            var charaType = context.CharacterType;

            if (!ValidateInteraction(context))
            {
                Debug.Log($"[InteractableBase] OnValidateInteraction により拒否: {context.Interactor}");
                return;
            }

            // クールダウン登録
            LastInteractTime = Runner ? Runner.SimulationTime : Time.time;
            
            //All キャラタイプのクールダウン時間を優先して取得する
            LastUsedCooldownTime = _cooldownTimeDictionary.Dictionary.TryGetValue(CharacterType.All, out var all)
                ? all : _cooldownTimeDictionary.Dictionary.GetValueOrDefault(charaType, 0f);
            
            var effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            effectSpawner.RequestPlayOneShotEffect(EffectType.InteractComplete, transform.position, transform.rotation);

            // 実行
            OnInteract(context);
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
            return true;
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