using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.InGame.Kraken.Animations
{
    public class KrakenAttackAnimationHandler : MonoBehaviour
    {
        [Serializable]
        private class KrakenTentacleAnimationSettings
        {
            [Serializable]
            public class ArmSettings
            {
                [SerializeField] private Animator _animator;
                [SerializeField] private Transform _armRoot;

                public Animator Animator => _animator;
                public Transform ArmRoot => _armRoot;
            }

            [SerializeField] private ArmSettings[] _arms;
            [SerializeField] private string _animationName;
            [SerializeField] private string _endStateName;

            private Quaternion _startRotation;
            private HashSet<ArmSettings> _usingArms = new();

            public bool TryGetUnusedArm(out ArmSettings result)
            {
                foreach (ArmSettings arm in _arms)
                {
                    if (!_usingArms.Add(arm)) continue;
                    result = arm;
                    return true;
                }

                result = null;
                return false;
            }

            public void ReleaseUsingArm(ArmSettings arm)
            {
                _usingArms.Remove(arm);
            }

            public void LookAt(ArmSettings arm, Vector3 target)
            {
                var root = arm.ArmRoot;

                _startRotation = root.rotation;

                var forward = -root.transform.right;
                forward.y = 0;
                var dir = Vector3.ProjectOnPlane(target - root.position, Vector3.up).normalized;
                Debug.DrawRay(root.position, dir * 100f, Color.red, 3f);

                var rot = Quaternion.FromToRotation(forward, dir);
                Debug.Log($"{target} {forward} {dir} {rot.eulerAngles}");

                root.rotation *= rot;
            }

            public async UniTask PlayAnimation(ArmSettings arm)
            {
                await arm.Animator.PlayAsync(_animationName, 0, 0f);
                await arm.Animator.WaitState(_endStateName);
                arm.Animator.transform.rotation = _startRotation;
                Debug.Log($"{_animationName} {_startRotation.eulerAngles}");
            }
        }

        [SerializeField] private KrakenTentacleAnimationSettings _tentacle;

        public async UniTask Attack(Vector3 target)
        {
            if (!_tentacle.TryGetUnusedArm(out var arm)) return;
            LatestArmRootPosition = arm.ArmRoot.position;
            _tentacle.LookAt(arm, target);
            await _tentacle.PlayAnimation(arm);
            _tentacle.ReleaseUsingArm(arm);
        }

        public Vector3 LatestArmRootPosition { get; private set; }
    }
}
