using System;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Interact;
using NaughtyAttributes;
using UnityEngine;

namespace InGame.Exhibit
{
    /// <summary>
    /// モアイにインタラクトした時の設定
    /// </summary>
    public class MoaiInteractInvoker : NetworkBehaviour
    {
        [SerializeField,Label("話し声")] private string _speakCue;
        [SerializeField, Label("インタラクトの時間")] private float _interactTime;
        [SerializeField,Label("AnimationSpeed")] private float _animationSpeed;
        
        private Animator _animator;
        private InteractableBase _interactableBase;

        #region Animation

        private static readonly int Speak = Animator.StringToHash("Speak");

        #endregion

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            _animator = GetComponent<Animator>();
            _interactableBase = GetComponent<InteractableBase>();
        }

        // おしゃべり実装とスコアが少しだけあがる
        public async UniTask StartSpeakAnimation()
        {
            _animator.SetBool(Speak, true);
            _animator.speed = _animationSpeed;
            _interactableBase.ForceSetInteractable = false;
            await UniTask.Delay(TimeSpan.FromSeconds(_interactTime));
            _animator.SetBool(Speak, false);
            _interactableBase.ForceSetInteractable = true;
        }
    }
}