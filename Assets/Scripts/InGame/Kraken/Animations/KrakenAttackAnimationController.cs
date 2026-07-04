using System;
using Cysharp.Threading.Tasks;
using RootMotion.FinalIK;
using UnityEngine;

namespace September.InGame.Kraken.Animations
{
    public class KrakenAttackAnimationHandler : MonoBehaviour
    {
        [Serializable]
        private class KrakenTentacleAnimationSettings
        {
            [SerializeField] private Animator _animator;
            [SerializeField] private Transform _armRoot;
            [SerializeField] private FABRIKRoot _fabrikRoot;
            [SerializeField] private string _animationName;

            private Quaternion _startRotation;

            public void LookAt(Vector3 target)
            {
                Transform root = _armRoot;

                _startRotation = root.rotation;

                var forward = -root.transform.right;
                var dir = Vector3.ProjectOnPlane(target - root.position, root.up).normalized;
                Debug.DrawRay(root.position, dir * 100f, Color.red, 3f);

                var rot = Quaternion.FromToRotation(forward, dir);
                Debug.Log($"{target} {forward} {dir} {rot.eulerAngles}");

                root.rotation *= rot;
            }

            public async UniTask PlayAnimation()
            {
                _animator.Play(_animationName, 0, 0f);
                await _animator.WaitUntilEndState(_animationName);
                _armRoot.transform.rotation = _startRotation;
            }
        }

        [SerializeField] private KrakenTentacleAnimationSettings _tentacle;

        public async UniTask Attack(Vector3 target)
        {
            _tentacle.LookAt(target);
            await _tentacle.PlayAnimation();
        }
    }
}
