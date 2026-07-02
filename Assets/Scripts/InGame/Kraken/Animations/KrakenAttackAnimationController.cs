using System;
using Cysharp.Threading.Tasks;
using RootMotion.FinalIK;
using UnityEngine;

namespace September.InGame.Kraken.Animations
{
    // Todo: 触手の向きを変えられない問題修正
    public class KrakenAttackAnimationHandler : MonoBehaviour
    {
        [Serializable]
        private class KrakenTentacleAnimationSettings
        {
            [SerializeField] private Animator _animator;
            [SerializeField] private FABRIKRoot _fabrikRoot;
            [SerializeField] private string _animationName;

            private Quaternion _startRotation;
            private Quaternion _lookRotation;

            public void LookAt(Vector3 target)
            {
                _startRotation = _fabrikRoot.transform.rotation;

                var forward = -_fabrikRoot.transform.right;
                var dir = Vector3.ProjectOnPlane(target - _fabrikRoot.transform.position, _fabrikRoot.transform.up).normalized;
                Debug.DrawRay(_fabrikRoot.transform.position, dir * 100f, Color.red, 3f);

                var rot = Quaternion.FromToRotation(forward, dir);
                Debug.Log($"{target} {forward} {dir} {rot.eulerAngles}");
            }

            public async UniTask PlayAnimation()
            {
                IKSolver.UpdateDelegate del = () =>
                {
                    _fabrikRoot.transform.rotation *= _lookRotation;
                };

                _fabrikRoot.solver.OnPreUpdate += del;
                _animator.Play(_animationName, 0, 0f);
                await _animator.WaitUntilEndState(_animationName);
                _fabrikRoot.solver.OnPostUpdate -= del;
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
