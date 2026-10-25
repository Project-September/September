using RootMotion.FinalIK;
using UnityEngine;

namespace September.InGame.Player
{
    public class AimIKController : MonoBehaviour
    {
        [SerializeField] private AimIK _aim;
        [SerializeField] private AimIK _leftAim;
        [SerializeField] private FullBodyBipedIK _ik;
        [SerializeField] private LookAtIK _look;
        [SerializeField] private Transform _aimOrigin;
        [SerializeField] private Vector3 _aimAxis;

        [Range(0, 1)] public float Weight = 1f;

        public bool TwoHanded;

        public Transform RightAim { set => _aim.solver.transform = value; }
        public Transform LeftAim { set => _leftAim.solver.transform = value; }
        public Vector3 Target { set => _look.solver.IKPosition = value; }

        private Quaternion leftHandRotationRelative;

        private IKEffector LeftHand => _ik.solver.leftHandEffector;
        private IKEffector RightHand => _ik.solver.rightHandEffector;

        private void Start()
        {
            _aim.enabled = false;
            _leftAim.enabled = false;
            _ik.enabled = false;
            _look.enabled = false;
        }

        private void LateUpdate() => InternalUpdate();

        public void ManualUpdate() => InternalUpdate();

        private void InternalUpdate()
        {
            _aim.solver.IKPositionWeight = Weight;
            _leftAim.solver.IKPositionWeight = Weight;
            _ik.solver.IKPositionWeight = Weight;
            _look.solver.IKPositionWeight = Weight;

            Vector3 toLeftHand = LeftHand.bone.position - RightHand.bone.position;
            Vector3 toLeftHandRelative = RightHand.bone.InverseTransformDirection(toLeftHand);

            leftHandRotationRelative = Quaternion.Inverse(RightHand.bone.rotation) * LeftHand.bone.rotation;

            // 射撃時の反動アニメーション等を反映させるための処理
            // 任意のTransform(_aimOrigin)の任意の軸(_aimAxis)を正面とし、反動などで銃の照準(_aim.solver.transform)が正面を向いていない場合でも、
            // 照準からみた正面の向きをIKに知らせることで、元のアニメーションとIKを両立させる
            Vector3 axis = _aimOrigin.rotation * _aimAxis;
            if (_aim.solver.transform) _aim.solver.axis = _aim.solver.transform.InverseTransformDirection(axis);
            if (_leftAim.solver.transform) _leftAim.solver.axis = _leftAim.solver.transform.InverseTransformDirection(axis);

            _aim.solver.IKPosition = _look.solver.IKPosition;
            _aim.solver.Update();

            LeftHand.position = RightHand.bone.position + RightHand.bone.TransformDirection(toLeftHandRelative);
            LeftHand.positionWeight = 1f;

            RightHand.position = RightHand.bone.position;
            RightHand.positionWeight = 1f;
            _ik.solver.GetLimbMapping(FullBodyBipedChain.RightArm).maintainRotationWeight = 1f;

            _ik.solver.Update();

            // 一つの武器を両手持ちする場合、手の位置関係を維持する
            if (TwoHanded)
                LeftHand.bone.rotation = RightHand.bone.rotation * leftHandRotationRelative;

            if (!TwoHanded)
            {
                _leftAim.solver.IKPosition = _look.solver.IKPosition;
                _leftAim.solver.Update();
            }

            _look.solver.Update();
        }
    }
}
