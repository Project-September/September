using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerEquipmentManager : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Equipment[] _equipmentData;

        private readonly Dictionary<HumanBodyBones, Equipment> _currentEquipments = new();

        public IReadOnlyDictionary<HumanBodyBones, Equipment> CurrentEquipments => _currentEquipments;

        public event Action<Equipment> Equipped;
        public event Action<Equipment> Unequipped;

        private void Start()
        {
            foreach (var equipment in _equipmentData)
            {
                if (equipment.StartingEquipment)
                {
                    ChangeEquipment(equipment);
                }
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ChangeEquipment(EquipmentType type)
        {
            var equipment = _equipmentData.FirstOrDefault(x => x.Type == type);

            if (equipment == null)
            {
                Debug.LogError($"[EquipmentManager] EquipmentType '{type}' was not found in EquipmentData.");
                return;
            }

            ChangeEquipment(equipment);
        }

        private void ChangeEquipment(Equipment targetEquipment)
        {
            if (_currentEquipments.TryGetValue(targetEquipment.HumanBodyBones, out var currentEquipment))
            {
                DestroyEquipment(currentEquipment);
            }

            InstanceEquipment(targetEquipment);

            _currentEquipments[targetEquipment.HumanBodyBones] = targetEquipment;

            Equipped?.Invoke(targetEquipment);
        }

        private void InstanceEquipment(Equipment targetEquipment)
        {
            if (targetEquipment.Prefab == null) return;

            var parent = _animator.GetBoneTransform(targetEquipment.HumanBodyBones);
            var clonedEquipment = Instantiate(targetEquipment.Prefab, parent);

            clonedEquipment.transform.localPosition = targetEquipment.PositionOffset;
            clonedEquipment.transform.localEulerAngles = targetEquipment.RotationOffset;

            targetEquipment.ClonedObject = clonedEquipment;
        }

        private void DestroyEquipment(Equipment equipment)
        {
            if (equipment.ClonedObject == null) return;

            Destroy(equipment.ClonedObject);
            equipment.ClonedObject = null;

            Unequipped?.Invoke(equipment);
        }
    }
    [System.Serializable]
    public class Equipment
    {
        [SerializeField] private EquipmentType _type;
        public EquipmentType Type => _type;
        [SerializeField] private GameObject _prefab;
        public GameObject Prefab => _prefab;
        [SerializeField] private Vector3 _positionOffset;
        public Vector3 PositionOffset => _positionOffset;
        [SerializeField] private Vector3 _rotationOffset;
        public Vector3 RotationOffset => _rotationOffset;
        [SerializeField] private HumanBodyBones _humanBodyBones;
        public HumanBodyBones HumanBodyBones => _humanBodyBones;
        [SerializeField] private bool _startingEquipment;
        public bool StartingEquipment => _startingEquipment;

        [HideInInspector] public GameObject ClonedObject;
    }
    public enum EquipmentType
    {
        NormalAttack, Armory, Tutankhamen
    }
}
