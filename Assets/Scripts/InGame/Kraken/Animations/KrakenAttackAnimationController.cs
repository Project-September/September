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
                forward.y = 0;
                var dir = Vector3.ProjectOnPlane(target - root.position, Vector3.up).normalized;
                Debug.DrawRay(root.position, dir * 100f, Color.red, 3f);

                var rot = Quaternion.FromToRotation(forward, dir);
                Debug.Log($"{target} {forward} {dir} {rot.eulerAngles}");

                root.rotation *= rot;
            }

            public async UniTask PlayAnimation()
            {
                await _animator.PlayAsync(_animationName, 0, 0f);
                _armRoot.transform.rotation = _startRotation;
                Debug.Log($"{_animationName} {_startRotation.eulerAngles}");
            }

            public Vector3 RootPosition => _armRoot.position;
            public Quaternion RootRotation => _armRoot.rotation;
        }

        [SerializeField] private KrakenTentacleAnimationSettings _tentacle;

        public async UniTask Attack(Vector3 target)
        {
            _tentacle.LookAt(target);
            await _tentacle.PlayAnimation();
        }

        public Vector3 RootPosition => _tentacle.RootPosition;
        public Quaternion RootRotation => _tentacle.RootRotation;
    }
}
