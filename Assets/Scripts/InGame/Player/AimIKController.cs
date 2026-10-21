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

        [Range(0, 1)] public float Weight = 1f;

        public bool TwoHanded;

        public Transform RightAim { set => _aim.solver.transform = value; }
        public Transform LeftAim { set => _leftAim.solver.transform = value; }

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

        private void LateUpdate()
        {
            _aim.solver.IKPositionWeight = Weight;
            _leftAim.solver.IKPositionWeight = Weight;
            _ik.solver.IKPositionWeight = Weight;
            _look.solver.IKPositionWeight = Weight;

            Vector3 toLeftHand = LeftHand.bone.position - RightHand.bone.position;
            Vector3 toLeftHandRelative = RightHand.bone.InverseTransformDirection(toLeftHand);

            leftHandRotationRelative = Quaternion.Inverse(RightHand.bone.rotation) * LeftHand.bone.rotation;

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
